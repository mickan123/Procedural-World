using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

[BurstCompile]
struct CarvePathJob : IJob
{
    public NativeArray<float> finalHeightMap;
    public NativeArray<float> originalHeightMap;
    public NativeArray<float> roadStrengthMap;
    public NativeArray<Vector3> path;

    public NativeArray<RoadSettingsStruct> roadSettings;

    public NativeArray<float> biomeStrengths;
    public int numBiomes;
    public int mapSize;

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
        NativeArray<int> closestPathIndexes = FindClosestPathIndexes(originalHeightMap, path, maxWidth, mapSize);

        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                if (closestPathIndexes[x * mapSize + y] != -1)
                {
                    AverageRoadSettings averageRoadSettings = CalculateAverageRoadSettings(mapSize, numBiomes, x, y, roadSettings, biomeStrengths);
                    Vector3 closestPointOnLine = ClosestPointOnLine(mapSize, x, y, originalHeightMap, closestPathIndexes[x * mapSize + y], path);
                    Vector3 curPoint = new Vector3(x, originalHeightMap[x * mapSize + y], y);
                    CarvePoint(
                        curPoint,
                        closestPointOnLine,
                        finalHeightMap,
                        originalHeightMap,
                        mapSize,
                        x,
                        y,
                        averageRoadSettings
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
                    AverageRoadSettings averageRoadSettings = CalculateAverageRoadSettings(mapSize, numBiomes, x, y, roadSettings, biomeStrengths);
                    Vector3 closestPointOnLine = ClosestPointOnLine(mapSize, x, y, originalHeightMap, closestPathIndexes[x * mapSize + y], path);
                    Vector3 curPoint = new Vector3(x, originalHeightMap[x * mapSize + y], y);
                    CalculateRoadStrength(
                        curPoint,
                        closestPointOnLine,
                        finalHeightMap,
                        originalHeightMap,
                        mapSize,
                        x,
                        y,
                        averageRoadSettings,
                        roadStrengthMap
                    );
                }
            }
        }

        closestPathIndexes.Dispose();
    }

    // Finds closest point on path at every point
    private static NativeArray<int> FindClosestPathIndexes(NativeArray<float> originalHeightMap, NativeArray<Vector3> path, float maxRoadWidth, int mapSize)
    {
        NativeArray<bool> getClosestPathIndex = new NativeArray<bool>(mapSize * mapSize, Allocator.Temp);

        // Check whether a coordinate is approx within roadSettings.width range of 
        // a point on path to determine whether we bother getting the closest path index
        // Points not within this distance dont' matter
        for (int i = 0; i < path.Length; i++)
        {
            // Calculate search area and clamp it within map bounds
            int startX = (int)Mathf.Max(0f, path[i].x - maxRoadWidth);
            int endX = (int)Mathf.Min(mapSize - 1, path[i].x + maxRoadWidth + 1);
            int startZ = (int)Mathf.Max(0f, path[i].z - maxRoadWidth);
            int endZ = (int)Mathf.Min(mapSize - 1, path[i].z + maxRoadWidth + 1);

            for (int x = startX; x <= endX; x++)
            {
                for (int z = startZ; z <= endZ; z++)
                {
                    getClosestPathIndex[x * mapSize + z] = true;
                }
            }
        }

        NativeArray<int> closestPathIndexes = new NativeArray<int>(mapSize * mapSize, Allocator.Temp);

        Vector3 delta = new Vector3();
        for (int x = 0; x < mapSize; x++)
        {
            for (int z = 0; z < mapSize; z++)
            {
                float y = originalHeightMap[x * mapSize + z];
                if (getClosestPathIndex[x * mapSize + z])
                {
                    float minDist = float.MaxValue;
                    int closestPointIndex = 0;
                    for (int k = 0; k < path.Length; k++)
                    {
                        delta = path[k] - new Vector3(x, y, z);
                        float dist = delta.sqrMagnitude;
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestPointIndex = k;
                        }
                    }
                    closestPathIndexes[x* mapSize + z] = closestPointIndex;
                }
                else
                {
                    closestPathIndexes[x * mapSize + z] = -1;
                }
            }
        }

        getClosestPathIndex.Dispose();

        return closestPathIndexes;
    }

    private static Vector3 ClosestPointOnLine(
        int mapSize,
        int x,
        int z,
        NativeArray<float> originalHeightMap,
        int closestPointIndex,
        NativeArray<Vector3> path
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

    private static void CarvePoint(
        Vector3 curPoint,
        Vector3 closestPointOnLine,
        NativeArray<float> finalHeightMap,
        NativeArray<float> originalHeightMap,
        int mapSize,
        int x,
        int y,
        AverageRoadSettings averageRoadSettings
    )
    {            
        float deltaX = closestPointOnLine.x - curPoint.x;
        float deltaZ = closestPointOnLine.z - curPoint.z;
        float distance = Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
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

    private static void CalculateRoadStrength(
        Vector3 curPoint,
        Vector3 closestPointOnLine,
        NativeArray<float> finalHeightMap,
        NativeArray<float> originalHeightMap,
        int mapSize,
        int x,
        int y,
        AverageRoadSettings averageRoadSettings,
        NativeArray<float> roadStrengthMap
    )
    {
        float deltaX = closestPointOnLine.x - curPoint.x;
        float deltaZ = closestPointOnLine.z - curPoint.z;
        float distance = Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
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
            roadStrengthMap[x * mapSize + y] = Mathf.Max(slopeMultiplier, roadStrengthMap[x * mapSize + y]);
        }
        else if (distance < averageRoadSettings.width)
        {
            float percentage = (distance - halfRoadWidth) / halfRoadWidth;
            slopeMultiplier = slopeMultiplier * (1f - percentage);
            roadStrengthMap[x * mapSize + y] = Mathf.Max(slopeMultiplier, roadStrengthMap[x * mapSize + y]);
        }
    }

    private static readonly int[] offsets1d = {  1, 0 , 0, 1, -1, 0, 0, -1 }; // 1d offsets for burst compiler

    public static float CalculateAngle(int mapSize, int xIn, int yIn, NativeArray<float> heightMap)
    {
        float maxAngle = 0f;

        for (int i = 0; i < 4; i++)
        {
            int x2 = Mathf.Min(Mathf.Max(xIn + offsets1d[i * 2], 0), mapSize - 1);
            int y2 = Mathf.Min(Mathf.Max(yIn + offsets1d[i * 2 + 1], 0), mapSize - 1);
            float angle = AngleBetweenTwoPoints(
                xIn,
                yIn,
                x2,
                y2,
                mapSize,
                heightMap
            );
            maxAngle = Mathf.Max(angle, maxAngle);
        }
        return maxAngle;
    }

    private static float AngleBetweenTwoPoints(int x1, int y1, int x2, int y2, int mapSize, NativeArray<float> heightMap)
    {
        float angle = Mathf.Rad2Deg * Mathf.Atan2(
            heightMap[x1 * mapSize + y1] - heightMap[x2 * mapSize + y2],
            1f
        );
        angle = Mathf.Abs(angle); // Get abs value
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

    private static AverageRoadSettings CalculateAverageRoadSettings(int mapSize, int numBiomes, int x, int y, NativeArray<RoadSettingsStruct> roadSettingsList, NativeArray<float> biomeStrengths)
    {
        float maxAngle = 0f;
        float distanceBlendFactor = 0f;
        float angleBlendFactor = 0f;
        float width = 0f;

        for (int biome = 0; biome < numBiomes; biome++)
        {
            maxAngle += biomeStrengths[x * mapSize * numBiomes + y * numBiomes + biome] * roadSettingsList[biome].maxAngle;
            distanceBlendFactor += biomeStrengths[x * mapSize * numBiomes + y * numBiomes + biome] * roadSettingsList[biome].distanceBlendFactor;
            angleBlendFactor += biomeStrengths[x * mapSize * numBiomes + y * numBiomes + biome] * roadSettingsList[biome].angleBlendFactor;
            width += biomeStrengths[x * mapSize * numBiomes + y * numBiomes + biome] * roadSettingsList[biome].width;
        }
        return new AverageRoadSettings(maxAngle, distanceBlendFactor, angleBlendFactor, width);
    }
}