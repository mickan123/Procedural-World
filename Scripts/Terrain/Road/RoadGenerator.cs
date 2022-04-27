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
        float[] originalHeightMap, 
        BiomeInfo info
    )
    {
        int width = info.width;

        // Create list of roadsettings
        List<RoadSettings> roadSettingsList = new List<RoadSettings>();
        for (int i = 0; i < terrainSettings.biomeSettings.Length; i++)
        {
            BiomeGraph graph = terrainSettings.biomeSettings[i].biomeGraph;
            RoadSettings roadSettings = graph.GetRoadSettings();
            roadSettingsList.Add(roadSettings);
        }

        // Allocate road strength map and final heightmap
        float[] finalRoadStrengthMap = new float[width * width];
        float[] finalHeightMap = new float[width * width];

        // Initialise finalHeightMap to be same is originalHeightMap
        for (int i = 0; i < width * width; i++)
        {
            finalHeightMap[i] = originalHeightMap[i];
        }

        // Create roads
        List<RoadRoute> routes = GetRoadDestinations(width, chunkCentre);
        for (int i = 0; i < routes.Count; i++)
        {
            Vector3[] path = CreatePath(routes[i], originalHeightMap, width, terrainSettings.maxRoadWidth);
            RoadData data = CreateRoad(path, finalHeightMap, originalHeightMap, roadSettingsList, info, terrainSettings.maxRoadWidth);

            finalHeightMap = data.heightMap;

            // Update roadstrengthmap
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    finalRoadStrengthMap[x * width + y] = Mathf.Max(data.roadStrengthMap[x * width + y], finalRoadStrengthMap[x * width + y]);
                }
            }
        }

        // Fade away any road carving from edge so that cross chunk roads blend smoothly
        Common.FadeEdgeHeightMap(originalHeightMap, finalHeightMap, width, 5f);
        
        return new RoadData(finalHeightMap, finalRoadStrengthMap);
    }


    public static List<RoadRoute> GetRoadDestinations(float width, Vector2 chunkCentre)
    {
        List<RoadRoute> destinations = new List<RoadRoute>();

        destinations.Add(new RoadRoute(new Vector2(width / 2, 0),
                                       new Vector2(width / 2, width - 1)));

        destinations.Add(new RoadRoute(new Vector2(0, width / 2),
                                       new Vector2(width - 1, width / 2)));

        return destinations;
    }


    public static Vector3[] CreatePath(RoadRoute route, float[] heightMap, int width, float maxRoadWidth)
    {
        Vector3 roadStart = new Vector3(route.roadStart.x, Common.HeightFromFloatCoord(route.roadStart, heightMap, width), route.roadStart.y);
        Vector3 roadEnd = new Vector3(route.roadEnd.x, Common.HeightFromFloatCoord(route.roadEnd, heightMap, width), route.roadEnd.y);

        // Create second point perpendicular to edge from start and end points to make sure last part of path is perpendicular to edge of chunk
        Vector3 roadStart2nd = roadStart + new Vector3((roadStart.x == 0) ? 5 : (roadStart.x == width - 1) ? -5 : 0,
                                                        0,
                                                        (roadStart.z == 0) ? 5 : (roadStart.z == width - 1) ? -5 : 0);
        Vector3 roadEnd2nd = roadEnd + new Vector3((roadEnd.x == 0) ? 5 : (roadEnd.x == width - 1) ? -5 : 0,
                                                    0,
                                                    (roadEnd.z == 0) ? 5 : (roadEnd.z == width - 1) ? -5 : 0);
        Vector3[] path;
        path = FindPath(heightMap, width, maxRoadWidth, roadStart2nd, roadEnd2nd);
        path = SmoothPath(path, roadStart, roadEnd);
        return path;
    }


    private static Vector3[] FindPath(float[] heightMap, int width, float maxRoadWidth, Vector3 roadStart, Vector3 roadEnd)
    {

        NativeArray<float> heightMapNat = new NativeArray<float>(heightMap, Allocator.TempJob);
        
        NativeList<int2> pathNat = new NativeList<int2>(Allocator.TempJob);

        FindPathJob burstJob = new FindPathJob
        {
            heightMap = heightMapNat,
            roadStart = roadStart,
            roadEnd = roadEnd,
            width = width,
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
            path[i] = new Vector3(x, heightMap[x * width + z], z);
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
        float[] finalHeightMap,
        float[] originalHeightMap,
        List<RoadSettings> roadSettingsList,
        BiomeInfo info,
        float maxRoadWidth
    )
    {   
        int width = info.width;
        int numBiomes = info.numBiomes;

        float[] roadStrengthMap = new float[width * width];
        
        NativeArray<float> finalHeightMapNat = new NativeArray<float>(finalHeightMap, Allocator.TempJob);
        NativeArray<float> originalHeightMapNat = new NativeArray<float>(originalHeightMap, Allocator.TempJob);
        NativeArray<Vector3> pathNat = new NativeArray<Vector3>(path, Allocator.TempJob);
        NativeArray<RoadSettingsStruct> roadSettingsNat = new NativeArray<RoadSettingsStruct>(roadSettingsList.Count, Allocator.TempJob);
        NativeArray<float> roadStrengthMapNat = new NativeArray<float>(width * width, Allocator.TempJob);
        NativeArray<float> biomeStrengthsNat = new NativeArray<float>(info.biomeStrengths, Allocator.TempJob);

        pathNat.CopyFrom(path);

        NativeArray<int> closestPathIndexesNat = GetClosestPathIndexes(width, maxRoadWidth, path, originalHeightMap);

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
            width = width,
            numBiomes = numBiomes,
            biomeStrengths = biomeStrengthsNat,
            closestPathIndexes = closestPathIndexesNat,
        };

        burstJob.Schedule().Complete();

        finalHeightMapNat.CopyTo(finalHeightMap);
        roadStrengthMapNat.CopyTo(roadStrengthMap);

        finalHeightMapNat.Dispose();
        originalHeightMapNat.Dispose();
        pathNat.Dispose();
        roadSettingsNat.Dispose();
        roadStrengthMapNat.Dispose();
        biomeStrengthsNat.Dispose();

        closestPathIndexesNat.Dispose();

        return new RoadData(finalHeightMap, roadStrengthMap);
    }

    public static NativeArray<int> GetClosestPathIndexes(int width, float maxRoadWidth, Vector3[] path, float[] originalHeightMap)
    {
        bool[] getClosestPathIndex = new bool[width * width];

        for (int i = 0; i < path.Length; i++)
        {
            // Calculate search area and clamp it within map bounds
            int startX = (int)math.max(0f, path[i].x - maxRoadWidth);
            int endX = (int)math.min(width - 1, path[i].x + maxRoadWidth + 1);
            int startZ = (int)math.max(0f, path[i].z - maxRoadWidth);
            int endZ = (int)math.min(width - 1, path[i].z + maxRoadWidth + 1);

            for (int x = startX; x <= endX; x++)
            {
                for (int z = startZ; z <= endZ; z++)
                {   
                    getClosestPathIndex[x * width + z] = true;
                }
            }
        }

        int queryLength = 0;
        for (int i = 0; i < width * width; i++)
        {
            if (getClosestPathIndex[i]) 
            {
                queryLength++;
            }
        }

        Vector3[] queries = new Vector3[queryLength];
        int queryIdx = 0;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (getClosestPathIndex[i * width + j]) 
                {
                    float height = originalHeightMap[i * width + j];
                    queries[queryIdx] = new Vector3(i, height, j);
                    queryIdx++;
                }
            }
        }
        GridSearchBurst gsb = new GridSearchBurst(maxRoadWidth);
        gsb.initGrid(path);

        int[] closestPoints = gsb.searchClosestPoint(queries);
        NativeArray<int> closestPathIndexesNat = new NativeArray<int>(width * width, Allocator.TempJob);

        int closetsPathIndexCounter = 0;
        for (int i = 0; i < width * width; i++)
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
    public float[] heightMap;
    public float[] roadStrengthMap;

    public RoadData(float[] heightMap, float[] roadStrengthMap)
    {
        this.heightMap = heightMap;
        this.roadStrengthMap = roadStrengthMap;
    }
}