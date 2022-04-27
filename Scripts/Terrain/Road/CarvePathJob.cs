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
    [ReadOnly] public int width;

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

        NativeArray<AverageRoadSettings> averageRoadSettings = new NativeArray<AverageRoadSettings>(width * width, Allocator.Temp);
        NativeArray<Vector3> closestPointsOnLine = new NativeArray<Vector3>(width * width, Allocator.Temp);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {
                if (closestPathIndexes[x * width + y] != -1)
                {
                    averageRoadSettings[x * width + y] = CalculateAverageRoadSettings(x, y);
                    closestPointsOnLine[x * width + y] = ClosestPointOnLine(x, y, closestPathIndexes[x * width + y]);
                }
            }
        }
        

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {
                if (closestPathIndexes[x * width + y] != -1)
                {
                    Vector3 curPoint = new Vector3(x, originalHeightMap[x * width + y], y);
                    CarvePoint(
                        curPoint,
                        closestPointsOnLine[x * width + y],
                        x,
                        y,
                        averageRoadSettings[x * width + y]
                    );
                }
            }
        }

        // Calculate road strength, must be done after road has been carved as it changes the angles
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {
                if (closestPathIndexes[x * width + y] != -1)
                {
                    Vector3 curPoint = new Vector3(x, originalHeightMap[x * width + y], y);
                    CalculateRoadStrength(
                        curPoint,
                        closestPointsOnLine[x * width + y],
                        x,
                        y,
                        averageRoadSettings[x * width + y]
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
        Vector3 curPoint = new Vector3(x, originalHeightMap[x * width + z], z);

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
        float angle = CalculateAngle(width, x, y, finalHeightMap);
        float slopeMultiplier = 1f - averageRoadSettings.angleBlendFactor * angle / averageRoadSettings.maxAngle;
        slopeMultiplier = math.clamp(slopeMultiplier, 0f, 1f);
        
        // If within half width of road then fully carve path, otherwise smooth outwards
        float halfRoadWidth = averageRoadSettings.width / 2f;
        if (distance < halfRoadWidth)
        {
            float percentage = distance / halfRoadWidth;
            float roadMultiplier = percentage * averageRoadSettings.distanceBlendFactor;
            float newValue = (1f - roadMultiplier) * closestPointOnLine.y + roadMultiplier * curPoint.y;

            finalHeightMap[x * width + y] = slopeMultiplier * newValue + (1 - slopeMultiplier) * finalHeightMap[x * width + y];
        }
        else if (distance < averageRoadSettings.width)
        {
            float percentage = (distance - halfRoadWidth) / halfRoadWidth;
            float roadMultiplier = percentage * (1f - averageRoadSettings.distanceBlendFactor) + averageRoadSettings.distanceBlendFactor;
            float newValue = roadMultiplier * curPoint.y + (1f - roadMultiplier) * closestPointOnLine.y;

            finalHeightMap[x * width + y] = slopeMultiplier * newValue + (1 - slopeMultiplier) * finalHeightMap[x * width + y];
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
        float angle = CalculateAngle(width, x, y, finalHeightMap);
        float slopeMultiplier = 1f - averageRoadSettings.angleBlendFactor * angle / averageRoadSettings.maxAngle;
        slopeMultiplier = slopeMultiplier < 0f ? 0f : slopeMultiplier;
        slopeMultiplier = slopeMultiplier > 1f ? 1f : slopeMultiplier;

        // If within half width of road then fully carve path, otherwise smooth outwards
        float halfRoadWidth = averageRoadSettings.width / 2f;
        if (distance < halfRoadWidth)
        {
            roadStrengthMap[x * width + y] = math.max(slopeMultiplier, roadStrengthMap[x * width + y]);
        }
        else if (distance < averageRoadSettings.width)
        {
            float percentage = (distance - halfRoadWidth) / halfRoadWidth;
            slopeMultiplier = slopeMultiplier * (1f - percentage);
            roadStrengthMap[x * width + y] = math.max(slopeMultiplier, roadStrengthMap[x * width + y]);
        }
    }

    private static readonly int[] offsets1d = {  1, 0 , 0, 1, -1, 0, 0, -1 }; // 1d offsets for burst compiler

    public float CalculateAngle(int width, int xIn, int yIn, NativeArray<float> heightMap)
    {
        float maxAngle = 0f;

        for (int i = 0; i < 4; i++)
        {
            int x2 = math.clamp(xIn + offsets1d[i * 2], 0, width - 1);
            int y2 = math.clamp(yIn + offsets1d[i * 2 + 1], 0, width - 1);
            float angle = AngleBetweenTwoPoints(
                xIn,
                yIn,
                x2,
                y2,
                width,
                heightMap
            );
            maxAngle = math.max(angle, maxAngle);
        }
        return maxAngle;
    }

    private float AngleBetweenTwoPoints(int x1, int y1, int x2, int y2, int width, NativeArray<float> heightMap)
    {
        float angle = math.degrees(math.atan2(
            heightMap[x1 * width + y1] - heightMap[x2 * width + y2],
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
            maxAngle += biomeStrengths[x * this.width * numBiomes + y * numBiomes + biome] * roadSettings[biome].maxAngle;
            distanceBlendFactor += biomeStrengths[x * this.width * numBiomes + y * numBiomes + biome] * roadSettings[biome].distanceBlendFactor;
            angleBlendFactor += biomeStrengths[x * this.width * numBiomes + y * numBiomes + biome] * roadSettings[biome].angleBlendFactor;
            width += biomeStrengths[x * this.width * numBiomes + y * numBiomes + biome] * roadSettings[biome].width;
        }
        return new AverageRoadSettings(maxAngle, distanceBlendFactor, angleBlendFactor, width);
    }
}