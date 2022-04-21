using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using BurstGridSearch;

public static class RoadGenerator
{
    // Number of padded points at start and end of path to help smoothing
    private static readonly int numChunkEdgeSmoothPoints = 5; 

    public static RoadData GenerateRoads(
        TerrainSettings terrainSettings, 
        Vector2 chunkCentre, 
        float[][] originalHeightMap, 
        BiomeInfo info
    )
    {
        int mapSize = originalHeightMap.Length;

        // Create list of roadsettings
        List<RoadSettings> roadSettingsList = new List<RoadSettings>();
        for (int i = 0; i < terrainSettings.biomeSettings.Length; i++)
        {
            BiomeGraph graph = terrainSettings.biomeSettings[i].biomeGraph;
            RoadSettings roadSettings = graph.GetRoadSettings();
            roadSettingsList.Add(roadSettings);
        }

        // Allocate road strength map and final heightmap
        float[][] finalRoadStrengthMap = new float[mapSize][];
        float[][] finalHeightMap = new float[mapSize][];
        for (int i = 0; i < mapSize; i++)
        {
            finalRoadStrengthMap[i] = new float[mapSize];
            finalHeightMap[i] = new float[mapSize];
        }

        // Initialise finalHeightMap to be same is originalHeightMap
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                finalHeightMap[i][j] = originalHeightMap[i][j];
            }
        }

        // Create roads
        List<RoadRoute> routes = GetRoadDestinations(mapSize, chunkCentre);
        for (int i = 0; i < routes.Count; i++)
        {
            Vector3[] path = CreatePath(routes[i], originalHeightMap, terrainSettings.maxRoadWidth);
            RoadData data = CreateRoad(path, finalHeightMap, originalHeightMap, roadSettingsList, info, terrainSettings.maxRoadWidth);

            finalHeightMap = data.heightMap;

            // Update roadstrengthmap
            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    finalRoadStrengthMap[x][y] = Mathf.Max(data.roadStrengthMap[x][y], finalRoadStrengthMap[x][y]);
                }
            }
        }

        // Fade away any road carving from edge so that cross chunk roads blend smoothly
        Common.FadeEdgeHeightMap(originalHeightMap, finalHeightMap, 5f);
        
        return new RoadData(finalHeightMap, finalRoadStrengthMap);
    }


    public static List<RoadRoute> GetRoadDestinations(float mapSize, Vector2 chunkCentre)
    {
        List<RoadRoute> destinations = new List<RoadRoute>();

        destinations.Add(new RoadRoute(new Vector2(mapSize / 2, 0),
                                       new Vector2(mapSize / 2, mapSize - 1)));

        destinations.Add(new RoadRoute(new Vector2(0, mapSize / 2),
                                       new Vector2(mapSize - 1, mapSize / 2)));

        return destinations;
    }


    public static Vector3[] CreatePath(RoadRoute route, float[][] heightMap, float maxRoadWidth)
    {
        int mapSize = heightMap.Length;
        Vector3 roadStart = new Vector3(route.roadStart.x, Common.HeightFromFloatCoord(route.roadStart, heightMap), route.roadStart.y);
        Vector3 roadEnd = new Vector3(route.roadEnd.x, Common.HeightFromFloatCoord(route.roadEnd, heightMap), route.roadEnd.y);

        // Create second point perpendicular to edge from start and end points to make sure last part of path is perpendicular to edge of chunk
        Vector3 roadStart2nd = roadStart + new Vector3((roadStart.x == 0) ? 5 : (roadStart.x == mapSize - 1) ? -5 : 0,
                                                        0,
                                                        (roadStart.z == 0) ? 5 : (roadStart.z == mapSize - 1) ? -5 : 0);
        Vector3 roadEnd2nd = roadEnd + new Vector3((roadEnd.x == 0) ? 5 : (roadEnd.x == mapSize - 1) ? -5 : 0,
                                                    0,
                                                    (roadEnd.z == 0) ? 5 : (roadEnd.z == mapSize - 1) ? -5 : 0);
        Vector3[] path;
        path = FindPath(heightMap, maxRoadWidth, roadStart2nd, roadEnd2nd);
        path = SmoothPath(path, roadStart, roadEnd);
        return path;
    }


    private static Vector3[] FindPath(float[][] heightMap, float maxRoadWidth, Vector3 roadStart, Vector3 roadEnd)
    {
        int mapSize = heightMap.Length;

        NativeArray<float> heightMapNat = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        
        for (int i = 0; i < mapSize; i++)
        {
            int start = i * mapSize;
            heightMapNat.GetSubArray(start, mapSize).CopyFrom(heightMap[i]);
        }

        NativeList<int2> pathNat = new NativeList<int2>(Allocator.TempJob);

        FindPathJob burstJob = new FindPathJob
        {
            heightMap = heightMapNat,
            roadStart = roadStart,
            roadEnd = roadEnd,
            mapSize = mapSize,
            maxRoadWidth = maxRoadWidth,
            path = pathNat,
            stepSize = RoadSettings.stepSize
        };

        burstJob.Schedule().Complete();

        Vector3[] path = new Vector3[pathNat.Length];
        for (int i = 0; i < pathNat.Length; i++)
        {
            int x = pathNat[i].x;
            int z = pathNat[i].y;
            path[i] = new Vector3(x, heightMap[x][z], z);
        }

        heightMapNat.Dispose();
        pathNat.Dispose();

        return path;
    } 

    private static Vector3[] SmoothPath(Vector3[] path, Vector3 roadStart, Vector3 roadEnd)
    {
        return BezierSmoothPath(new List<Vector3>(path)).ToArray();
    }
    
    private static Vector3[] SuperSmoothPath(Vector3[] path, Vector3 roadStart, Vector3 roadEnd)
    {
        // The more times we add start and end points smoother end and start of path will be
        PadPath(path, numChunkEdgeSmoothPoints, roadStart, roadEnd);

        int curvedLength = path.Length * Mathf.RoundToInt(RoadSettings.smoothness) - 1;
        NativeList<Vector3> smoothedPointsNat = new NativeList<Vector3>(curvedLength, Allocator.TempJob);
        NativeArray<Vector3> pathNat = new NativeArray<Vector3>(path, Allocator.TempJob);

        SmoothPathJob smoothPathJob = new SmoothPathJob
        {
            smoothedPoints = smoothedPointsNat,
            path = pathNat,
        };

        smoothPathJob.Schedule().Complete();
        
        path = smoothedPointsNat.ToArray();

        smoothedPointsNat.Dispose();
        pathNat.Dispose();

        return path;
    }


    private static List<Vector3> BezierSmoothPath(List<Vector3> path)
    {
        // Pad path so that we have a multiple of 3 points + 1 for bezier curves
        while (path.Count % 3 != 1)
        {
            path.Add(path[path.Count - 1]);
        }

        // Construct bezier curve objects
        int numBezierCurves = path.Count / 3;
        BezierCurve[] curves = new BezierCurve[numBezierCurves];
        for (int i = 0; i < path.Count - 1; i += 3)
        {   
            curves[i / 3] = new BezierCurve(path[i], path[i + 1], path[i + 2], path[i + 3]);
        }

        // Calculate smoothed bezier curve points
        int curvedLength = (path.Count * Mathf.RoundToInt(RoadSettings.smoothness)) - 1;
        List<Vector3> smoothedPoints = new List<Vector3>(curvedLength);
        int numCurves = curves.Length;
        for (int i = 0; i < numCurves; i++)
        {
            Vector3[] segments = curves[i].GetSegments(RoadSettings.smoothness * 4);
            int segmentsLength = segments.Length;
            for (int j = 0; j < segmentsLength; j++)
            {
                smoothedPoints.Add(segments[j]);
            }
        }

        return smoothedPoints;
    }

    private static Vector3[] PadPath(Vector3[] path, int numPads, Vector3 roadStart, Vector3 roadEnd)
    {
        Vector3[] paddedPath = new Vector3[path.Length + (numPads * 2)];
        for (int i = 0; i < numPads; i++)
        {
            paddedPath[i] = roadEnd;
        }

        for (int i = 0; i < path.Length; i++)
        {
            paddedPath[i + numPads] = path[i];
        }


        for (int i = 0; i < numPads; i++)
        {
            int offset = numPads + path.Length;
            paddedPath[i + offset] = roadStart;
        }
        return paddedPath;
    }    


    public static RoadData CreateRoad(
        Vector3[] path,
        float[][] finalHeightMap,
        float[][] originalHeightMap,
        List<RoadSettings> roadSettingsList,
        BiomeInfo info,
        float maxRoadWidth
    )
    {   
        int mapSize = originalHeightMap.Length;
        int numBiomes = info.numBiomes;

        float[][] roadStrengthMap = new float[mapSize][];
        for (int i = 0; i < mapSize; i++)
        {
            roadStrengthMap[i] = new float[mapSize];
        }
        
        NativeArray<float> finalHeightMapNat = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        NativeArray<float> originalHeightMapNat = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        NativeArray<Vector3> pathNat = new NativeArray<Vector3>(path.Length, Allocator.TempJob);
        NativeArray<RoadSettingsStruct> roadSettingsNat = new NativeArray<RoadSettingsStruct>(roadSettingsList.Count, Allocator.TempJob);
        NativeArray<float> roadStrengthMapNat = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        NativeArray<float> biomeStrengthsNat = new NativeArray<float>(mapSize * mapSize * numBiomes, Allocator.TempJob);

        int start = 0;
        for (int i = 0; i < mapSize; i++)
        {
            start = i * mapSize;
            finalHeightMapNat.GetSubArray(start, mapSize).CopyFrom(finalHeightMap[i]);
            originalHeightMapNat.GetSubArray(start, mapSize).CopyFrom(originalHeightMap[i]);
        }
        biomeStrengthsNat.CopyFrom(info.biomeStrengths);
        pathNat.CopyFrom(path);

        NativeArray<int> closestPathIndexesNat = GetClosestPathIndexes(mapSize, maxRoadWidth, path, originalHeightMap);

        for (int i = 0; i < roadSettingsList.Count; i++)
        {
            roadSettingsNat[i] = new RoadSettingsStruct(roadSettingsList[i]);
        }

        CarvePathJob burstJob = new CarvePathJob
        {
            finalHeightMap = finalHeightMapNat,
            originalHeightMap = originalHeightMapNat,
            roadStrengthMap = roadStrengthMapNat,
            path = pathNat,
            roadSettings = roadSettingsNat,
            mapSize = mapSize,
            numBiomes = numBiomes,
            biomeStrengths = biomeStrengthsNat,
            closestPathIndexes = closestPathIndexesNat,
        };

        burstJob.Schedule().Complete();
        for (int i = 0; i < mapSize; i++)
        {
            start = i * mapSize;
            finalHeightMapNat.GetSubArray(start, mapSize).CopyTo(finalHeightMap[i]);
            roadStrengthMapNat.GetSubArray(start, mapSize).CopyTo(roadStrengthMap[i]);
        }

        finalHeightMapNat.Dispose();
        originalHeightMapNat.Dispose();
        pathNat.Dispose();
        roadSettingsNat.Dispose();
        roadStrengthMapNat.Dispose();
        biomeStrengthsNat.Dispose();

        closestPathIndexesNat.Dispose();

        return new RoadData(finalHeightMap, roadStrengthMap);
    }

    public static NativeArray<int> GetClosestPathIndexes(int mapSize, float maxRoadWidth, Vector3[] path, float[][] originalHeightMap)
    {
        bool[] getClosestPathIndex = new bool[mapSize * mapSize];

        for (int i = 0; i < path.Length; i++)
        {
            // Calculate search area and clamp it within map bounds
            int startX = (int)math.max(0f, path[i].x - maxRoadWidth);
            int endX = (int)math.min(mapSize - 1, path[i].x + maxRoadWidth + 1);
            int startZ = (int)math.max(0f, path[i].z - maxRoadWidth);
            int endZ = (int)math.min(mapSize - 1, path[i].z + maxRoadWidth + 1);

            for (int x = startX; x <= endX; x++)
            {
                for (int z = startZ; z <= endZ; z++)
                {   
                    getClosestPathIndex[x * mapSize + z] = true;
                }
            }
        }

        int queryLength = 0;
        for (int i = 0; i < mapSize * mapSize; i++)
        {
            if (getClosestPathIndex[i]) 
            {
                queryLength++;
            }
        }

        Vector3[] queries = new Vector3[queryLength];
        int queryIdx = 0;
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                if (getClosestPathIndex[i * mapSize + j]) 
                {
                    float height = originalHeightMap[i][j];
                    queries[queryIdx] = new Vector3(i, height, j);
                    queryIdx++;
                }
            }
        }
        GridSearchBurst gsb = new GridSearchBurst(maxRoadWidth);
        gsb.initGrid(path);

        int[] closestPoints = gsb.searchClosestPoint(queries);
        NativeArray<int> closestPathIndexesNat = new NativeArray<int>(mapSize * mapSize, Allocator.TempJob);

        int closetsPathIndexCounter = 0;
        for (int i = 0; i < mapSize * mapSize; i++)
        {
            if (getClosestPathIndex[i])
            {
                closestPathIndexesNat[i] = closestPoints[closetsPathIndexCounter];
                closetsPathIndexCounter++;   
            }
            else
            {
                closestPathIndexesNat[i] = -1;
            }
        }

        gsb.clean();
        
        return closestPathIndexesNat;
    }


    public struct RoadRoute
    {
        public Vector2 roadStart;
        public Vector2 roadEnd;

        public RoadRoute(Vector2 roadStart, Vector2 roadEnd)
        {
            this.roadStart = roadStart;
            this.roadEnd = roadEnd;
        }
    }
}


public struct RoadData
{
    public float[][] heightMap;
    public float[][] roadStrengthMap;

    public RoadData(float[][] heightMap, float[][] roadStrengthMap)
    {
        this.heightMap = heightMap;
        this.roadStrengthMap = roadStrengthMap;
    }
}