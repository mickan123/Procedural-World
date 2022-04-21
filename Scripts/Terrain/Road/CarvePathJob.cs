using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
struct CarvePathJob : IJob
{
    public NativeArray<float> finalHeightMap;
    [ReadOnly] public NativeArray<float> originalHeightMap;
    public NativeArray<float> roadStrengthMap;
    public NativeArray<Vector3> path;

    [ReadOnly] public NativeArray<RoadSettingsStruct> roadSettings;

    [ReadOnly] public NativeArray<float> biomeStrengths;
    [ReadOnly] public int numBiomes;
    [ReadOnly] public int mapSize;

    [ReadOnly] public NativeArray<int> closestPathIndexes;

    public void Execute() 
    {
        float maxWidth = 0f;
        for (int i = 0; i < roadSettings.Length; i++)
        {
            if (roadSettings[i].width > maxWidth)
            {
                maxWidth = roadSettings[i].width;
            }
        }

        NativeArray<AverageRoadSettings> averageRoadSettings = new NativeArray<AverageRoadSettings>(mapSize * mapSize, Allocator.Temp);
        NativeArray<Vector3> closestPointsOnLine = new NativeArray<Vector3>(mapSize * mapSize, Allocator.Temp);
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                if (closestPathIndexes[x * mapSize + y] != -1)
                {
                    averageRoadSettings[x * mapSize + y] = CalculateAverageRoadSettings(x, y);
                    closestPointsOnLine[x * mapSize + y] = ClosestPointOnLine(x, y, closestPathIndexes[x * mapSize + y]);
                }
            }
        }
        

        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                if (closestPathIndexes[x * mapSize + y] != -1)
                {
                    Vector3 curPoint = new Vector3(x, originalHeightMap[x * mapSize + y], y);
                    CarvePoint(
                        curPoint,
                        closestPointsOnLine[x * mapSize + y],
                        x,
                        y,
                        averageRoadSettings[x * mapSize + y]
                    );
                }
            }
        }

        // Calculate road strength, must be done after road has been carved as it changes the angles
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                if (closestPathIndexes[x * mapSize + y] != -1)
                {
                    Vector3 curPoint = new Vector3(x, originalHeightMap[x * mapSize + y], y);
                    CalculateRoadStrength(
                        curPoint,
                        closestPointsOnLine[x * mapSize + y],
                        x,
                        y,
                        averageRoadSettings[x * mapSize + y]
                    );
                }
            }
        }

        averageRoadSettings.Dispose();
        closestPointsOnLine.Dispose();
    }

    private Vector3 ClosestPointOnLine(
        int x,
        int z,
        int closestPointIndex
    )
    {
        Vector3 curPoint = new Vector3(x, originalHeightMap[x * mapSize + z], z);

        Vector3 closestPointOnPath = path[closestPointIndex];

        // Get distance of 2nd closest point so we can form a line
        Vector3 secondClosestPoint;
        if (closestPointIndex == path.Length - 1)
        {
            secondClosestPoint = path[closestPointIndex - 1];
        }
        else if (closestPointIndex == 0)
        {
            secondClosestPoint = path[closestPointIndex + 1];
        }
        else
        {
            Vector3 previousPoint = path[closestPointIndex - 1];
            Vector3 nextPoint = path[closestPointIndex + 1];

            float deltaPreX = previousPoint.x - x;
            float deltaPreZ = previousPoint.z - z;
            float distPre = deltaPreX * deltaPreX + deltaPreZ * deltaPreZ;
            
            float deltaPostX = nextPoint.x - x;
            float deltaPostZ = nextPoint.z - z;
            float distPost = deltaPostX * deltaPostX + deltaPostZ * deltaPostZ;

            secondClosestPoint = distPre < distPost ? path[closestPointIndex - 1] : path[closestPointIndex + 1];
        }

        Vector3 direction = secondClosestPoint - closestPointOnPath;
        Vector3 origin = closestPointOnPath;
        Vector3 point = curPoint;

        direction.Normalize();
        Vector3 lhs = point - origin;
        float dotP = Vector3.Dot(lhs, direction);

        Vector3 closestPoint = origin + direction * dotP;

        return closestPoint;    
    }

    private void CarvePoint(
        Vector3 curPoint,
        Vector3 closestPointOnLine,
        int x,
        int y,
        AverageRoadSettings averageRoadSettings
    )
    {            
        float deltaX = closestPointOnLine.x - curPoint.x;
        float deltaZ = closestPointOnLine.z - curPoint.z;
        float distance = math.sqrt(deltaX * deltaX + deltaZ * deltaZ);
        if (distance > averageRoadSettings.width)
        {
            return;
        }


        // Calculate slope multiplier
        float angle = CalculateAngle(mapSize, x, y, finalHeightMap);
        float slopeMultiplier = 1f - averageRoadSettings.angleBlendFactor * angle / averageRoadSettings.maxAngle;
        slopeMultiplier = math.clamp(slopeMultiplier, 0f, 1f);
        
        // If within half width of road then fully carve path, otherwise smooth outwards
        float halfRoadWidth = averageRoadSettings.width / 2f;
        if (distance < halfRoadWidth)
        {
            float percentage = distance / halfRoadWidth;
            float roadMultiplier = percentage * averageRoadSettings.distanceBlendFactor;
            float newValue = (1f - roadMultiplier) * closestPointOnLine.y + roadMultiplier * curPoint.y;

            finalHeightMap[x * mapSize + y] = slopeMultiplier * newValue + (1 - slopeMultiplier) * finalHeightMap[x * mapSize + y];
        }
        else if (distance < averageRoadSettings.width)
        {
            float percentage = (distance - halfRoadWidth) / halfRoadWidth;
            float roadMultiplier = percentage * (1f - averageRoadSettings.distanceBlendFactor) + averageRoadSettings.distanceBlendFactor;
            float newValue = roadMultiplier * curPoint.y + (1f - roadMultiplier) * closestPointOnLine.y;

            finalHeightMap[x * mapSize + y] = slopeMultiplier * newValue + (1 - slopeMultiplier) * finalHeightMap[x * mapSize + y];
        }
    }

    private void CalculateRoadStrength(
        Vector3 curPoint,
        Vector3 closestPointOnLine,
        int x,
        int y,
        AverageRoadSettings averageRoadSettings
    )
    {
        float deltaX = closestPointOnLine.x - curPoint.x;
        float deltaZ = closestPointOnLine.z - curPoint.z;
        float distance = math.sqrt(deltaX * deltaX + deltaZ * deltaZ);
        if (distance > averageRoadSettings.width)
        {
            return;
        }

        // Calculate slope multiplier
        float angle = CalculateAngle(mapSize, x, y, finalHeightMap);
        float slopeMultiplier = 1f - averageRoadSettings.angleBlendFactor * angle / averageRoadSettings.maxAngle;
        slopeMultiplier = slopeMultiplier < 0f ? 0f : slopeMultiplier;
        slopeMultiplier = slopeMultiplier > 1f ? 1f : slopeMultiplier;

        // If within half width of road then fully carve path, otherwise smooth outwards
        float halfRoadWidth = averageRoadSettings.width / 2f;
        if (distance < halfRoadWidth)
        {
            roadStrengthMap[x * mapSize + y] = math.max(slopeMultiplier, roadStrengthMap[x * mapSize + y]);
        }
        else if (distance < averageRoadSettings.width)
        {
            float percentage = (distance - halfRoadWidth) / halfRoadWidth;
            slopeMultiplier = slopeMultiplier * (1f - percentage);
            roadStrengthMap[x * mapSize + y] = math.max(slopeMultiplier, roadStrengthMap[x * mapSize + y]);
        }
    }

    private static readonly int[] offsets1d = {  1, 0 , 0, 1, -1, 0, 0, -1 }; // 1d offsets for burst compiler

    public float CalculateAngle(int mapSize, int xIn, int yIn, NativeArray<float> heightMap)
    {
        float maxAngle = 0f;

        for (int i = 0; i < 4; i++)
        {
            int x2 = math.clamp(xIn + offsets1d[i * 2], 0, mapSize - 1);
            int y2 = math.clamp(yIn + offsets1d[i * 2 + 1], 0, mapSize - 1);
            float angle = AngleBetweenTwoPoints(
                xIn,
                yIn,
                x2,
                y2,
                mapSize,
                heightMap
            );
            maxAngle = math.max(angle, maxAngle);
        }
        return maxAngle;
    }

    private float AngleBetweenTwoPoints(int x1, int y1, int x2, int y2, int mapSize, NativeArray<float> heightMap)
    {
        float angle = math.degrees(math.atan2(
            heightMap[x1 * mapSize + y1] - heightMap[x2 * mapSize + y2],
            1f
        ));
        angle = math.abs(angle); // Get abs value
        return angle;
    }

    private struct AverageRoadSettings
    {
        public float maxAngle;
        public float distanceBlendFactor;
        public float angleBlendFactor;
        public float width;

        public AverageRoadSettings(float maxAngle, float distanceBlendFactor, float angleBlendFactor, float width)
        {
            this.maxAngle = maxAngle;
            this.distanceBlendFactor = distanceBlendFactor;
            this.angleBlendFactor = angleBlendFactor;
            this.width = width;
        }
    }

    private AverageRoadSettings CalculateAverageRoadSettings(int x, int y)
    {
        float maxAngle = 0f;
        float distanceBlendFactor = 0f;
        float angleBlendFactor = 0f;
        float width = 0f;

        for (int biome = 0; biome < numBiomes; biome++)
        {
            maxAngle += biomeStrengths[x * mapSize * numBiomes + y * numBiomes + biome] * roadSettings[biome].maxAngle;
            distanceBlendFactor += biomeStrengths[x * mapSize * numBiomes + y * numBiomes + biome] * roadSettings[biome].distanceBlendFactor;
            angleBlendFactor += biomeStrengths[x * mapSize * numBiomes + y * numBiomes + biome] * roadSettings[biome].angleBlendFactor;
            width += biomeStrengths[x * mapSize * numBiomes + y * numBiomes + biome] * roadSettings[biome].width;
        }
        return new AverageRoadSettings(maxAngle, distanceBlendFactor, angleBlendFactor, width);
    }
}