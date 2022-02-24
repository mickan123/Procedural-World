using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

public static class RoadGenerator
{
    private static readonly int[,] offsets = { { 1 , 0}, { 0 , 1}, { -1, 0}, { 0 , -1},
                                               { 1 , 1}, { 1 ,-1}, { -1, 1}, {-1 , -1},
                                               { 2 , 1}, { 2 ,-1}, { -2, 1}, {-2 , -1},
                                               { 1 , 2}, { 1 ,-2}, { -1, 2}, {-1 , -2}};

    private static readonly int numChunkEdgeSmoothPoints = 5; // Number of padded points at start and end of path to help smoothing

    public static RoadData GenerateRoads(TerrainSettings terrainSettings, Vector2 chunkCentre, float[][] referenceHeightMap, BiomeInfo info)
    {
        int mapSize = referenceHeightMap.Length;

        List<RoadRoute> routes = GetRoadDestinations(mapSize, chunkCentre);

        int numBiomes = terrainSettings.biomeSettings.Length;
        bool[] roadsEnabled = new bool[numBiomes];

        List<RoadSettings> roadSettingsList = new List<RoadSettings>();

        for (int i = 0; i < terrainSettings.biomeSettings.Length; i++)
        {
            BiomeGraph graph = terrainSettings.biomeSettings[i].biomeGraph;
            RoadSettings roadSettings = graph.GetRoadSettings();
            roadSettingsList.Add(roadSettings);
        }

        // Initialise road strength map and final heightmap
        float[][] roadStrengthMap = new float[mapSize][];
        float[][] finalHeightMap = new float[mapSize][];
        for (int i = 0; i < mapSize; i++)
        {
            roadStrengthMap[i] = new float[mapSize];
            finalHeightMap[i] = new float[mapSize];
            
        }

        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                finalHeightMap[i][j] = referenceHeightMap[i][j];
            }
        }

        for (int i = 0; i < routes.Count; i++)
        {
            Vector3[] path = CreatePath(routes[i], referenceHeightMap, terrainSettings.maxRoadWidth);

            RoadData data = CreateRoad(path, finalHeightMap, referenceHeightMap, roadSettingsList, info);

            finalHeightMap = data.heightMap;
            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    roadStrengthMap[x][y] = Mathf.Max(data.roadStrengthMap[x][y], roadStrengthMap[x][y]);
                }
            }
        }

        // Fade away any road carving from edge so that cross chunk roads blend smoothly

        float blendDistance = 5f;
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                float nearDist = i < j ? i : j;
                float farDist = mapSize - 1 - (i > j ? i : j);
                float distFromEdge = nearDist < farDist ? nearDist : farDist;
                distFromEdge = distFromEdge - 3f < 0f ? 0f : distFromEdge - 3f ;
                float edgeMultiplier = distFromEdge / blendDistance < 1f ? distFromEdge / blendDistance :1f;
                finalHeightMap[i][j] = edgeMultiplier * finalHeightMap[i][j] + (1f - edgeMultiplier) * referenceHeightMap[i][j];
            }
        }

        return new RoadData(finalHeightMap, roadStrengthMap);
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
        float mapSize = heightMap.Length;
        Vector3 roadStart = new Vector3(route.roadStart.x, Common.HeightFromFloatCoord(route.roadStart, heightMap), route.roadStart.y);
        Vector3 roadEnd = new Vector3(route.roadEnd.x, Common.HeightFromFloatCoord(route.roadEnd, heightMap), route.roadEnd.y);

        // Create second point perpendicular to edge from start and end points to make sure last part of path is perpendsicular to edge of chunk
        Vector3 roadStart2nd = roadStart + new Vector3((roadStart.x == 0) ? 5 : (roadStart.x == mapSize - 1) ? -5 : 0,
                                                        0,
                                                        (roadStart.z == 0) ? 5 : (roadStart.z == mapSize - 1) ? -5 : 0);
        Vector3 roadEnd2nd = roadEnd + new Vector3((roadEnd.x == 0) ? 5 : (roadEnd.x == mapSize - 1) ? -5 : 0,
                                                    0,
                                                    (roadEnd.z == 0) ? 5 : (roadEnd.z == mapSize - 1) ? -5 : 0);

        List<Vector3> path = FindPath(roadStart2nd, roadEnd2nd, heightMap, maxRoadWidth);

        // The more times we add start and end points smoother end and start of path will be
        for (int i = 0; i < numChunkEdgeSmoothPoints; i++)
        {
            path.Insert(0, roadStart);
            path.Add(roadEnd);
        }
        return SmoothPath(path).ToArray();
    }

    public static RoadData CreateRoad(
        Vector3[] path,
        float[][] finalHeightMap,
        float[][] referenceHeightMap,
        List<RoadSettings> roadSettingsList,
        BiomeInfo info
    )
    {   
        int mapSize = referenceHeightMap.Length;
        float[][] roadStrengthMap = new float[mapSize][];
        for (int i = 0; i < mapSize; i++)
        {
            roadStrengthMap[i] = new float[mapSize];
        }
        CarvePath(finalHeightMap, referenceHeightMap, path, roadSettingsList, roadStrengthMap, info);

        return new RoadData(finalHeightMap, roadStrengthMap);
    }

    private static List<Vector3> FindPath(Vector3 roadStart, Vector3 roadEnd, float[][] heightMap, float maxRoadWidth)
    {
        int mapSize = heightMap.Length;

        Node[][] nodeGrid = new Node[mapSize][];
        for (int i = 0; i < mapSize; i++)
        {
            nodeGrid[i] = new Node[mapSize];
            for (int j = 0; j < mapSize; j++)
            {
                nodeGrid[i][j] = new Node(i, j, heightMap[i][j]);
            }
        }

        Node startNode = nodeGrid[(int)roadStart.x][(int)roadStart.z];
        Node endNode = nodeGrid[(int)roadEnd.x][(int)roadEnd.z];

        Heap<Node> openSet = new Heap<Node>(mapSize * mapSize);
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet.RemoveFirst();
            closedSet.Add(currentNode);

            if (currentNode == endNode)
            {
                return RetracePath(currentNode, heightMap);
            }

            List<Node> neighbours = GetNeighbours(nodeGrid, currentNode, endNode);
            for (int i = 0; i < neighbours.Count; i++)
            {
                Node neighbour = neighbours[i];

                if (closedSet.Contains(neighbour))
                {
                    continue;
                }

                float costToNeighbour = currentNode.gCost + GetCost(currentNode, neighbour, mapSize, maxRoadWidth / 2f);
                if (costToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = costToNeighbour;
                    neighbour.hCost = GetCost(neighbour, endNode, mapSize, maxRoadWidth / 2f);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                    else
                    {
                        openSet.UpdateItem(neighbour);
                    }
                }
            }
        }
        return new List<Vector3>();
    }

    private static List<Node> GetNeighbours(Node[][] nodeGrid, Node node, Node endNode)
    {
        List<Node> neighbours = new List<Node>();
        int mapSize = nodeGrid.Length;

        float deltaXend = Mathf.Abs(endNode.x - node.x);
        float deltaYend = Mathf.Abs(endNode.y - node.y);
        float distanceToEndnode = deltaXend * deltaXend + deltaYend * deltaYend;

        if (distanceToEndnode < RoadSettings.stepSize * 2f)
        {
            neighbours.Add(endNode);
        }

        for (int i = 0; i < offsets.GetLength(0); i++)
        {
            int neighbourX = node.x + offsets[i, 0] * RoadSettings.stepSize;
            int neighbourY = node.y + offsets[i, 1] * RoadSettings.stepSize;

            neighbourX = Mathf.Clamp(neighbourX, 0, mapSize - 1);
            neighbourY = Mathf.Clamp(neighbourY, 0, mapSize - 1);

            neighbours.Add(nodeGrid[neighbourX][neighbourY]);
        }

        return neighbours;
    }

    private static float GetCost(Node a, Node b, int mapSize, float halfRoadWidth)
    {

        float deltaX = a.x - b.x;
        float deltaY = a.y - b.y;
        float flatDist = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);

        float heightDiff = Mathf.Abs(a.height - b.height);

        float slope = heightDiff / flatDist;
        float slopeCost = slope;

        // Penalize being close to edge of chunk
        float edgeCost = 0f;
        if (b.x < halfRoadWidth || b.x > mapSize - 1 - halfRoadWidth
            || b.y < halfRoadWidth || b.y > mapSize - 1 - halfRoadWidth)
        {
            edgeCost = 100000f;
        }

        return slopeCost + edgeCost;
    }

    private static List<Vector3> RetracePath(Node node, float[][] heightMap)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = node;
        while (currentNode != null)
        {
            path.Add(new Vector3(currentNode.x, heightMap[currentNode.x][currentNode.y], currentNode.y));
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }

    private static List<Vector3> SmoothPath(List<Vector3> path)
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

    // Results in a smoother path than the bezier curve approach however 
    // is very costly for performance
    private static List<Vector3> VerySmoothPath(List<Vector3> path)
    {
        List<Vector3> smoothedPoints;
        List<Vector3> points;
        int pointsLength = path.Count;
        int curvedLength = (pointsLength * Mathf.RoundToInt(RoadSettings.smoothness)) - 1;
        smoothedPoints = new List<Vector3>(curvedLength);

        float t = 0.0f;
        for (int pointInTimeOnCurve = 0; pointInTimeOnCurve < curvedLength + 1; pointInTimeOnCurve++)
        {
            t = Mathf.InverseLerp(0, curvedLength, pointInTimeOnCurve);

            points = new List<Vector3>(path);

            for (int j = pointsLength - 1; j > 0; j--)
            {
                for (int i = 0; i < j; i++)
                {
                    points[i] = (1 - t) * points[i] + t * points[i + 1];
                }
            }

            smoothedPoints.Add(points[0]);
        }
        return smoothedPoints;
    }

    private static void CarvePath(
        float[][] finalHeightMap,
        float[][] referenceHeightMap,
        Vector3[] path,
        List<RoadSettings> roadSettingsList,
        float[][] roadStrengthMap,
        BiomeInfo info
    )
    {
        int mapSize = referenceHeightMap.Length;
        int numBiomes = info.numBiomes;

        NativeArray<float> finalHeightMapNat = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        NativeArray<float> referenceHeightMapNat = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        NativeArray<Vector3> pathNat = new NativeArray<Vector3>(path.Length, Allocator.TempJob);
        NativeArray<RoadSettingsStruct> roadSettingsNat = new NativeArray<RoadSettingsStruct>(roadSettingsList.Count, Allocator.TempJob);
        NativeArray<float> roadStrengthMapNat = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);
        NativeArray<float> biomeStrengthsNat = new NativeArray<float>(mapSize * mapSize * numBiomes, Allocator.TempJob);

        int start = 0;
        for (int i = 0; i < mapSize; i++)
        {
            start = i * mapSize;
            finalHeightMapNat.GetSubArray(start, mapSize).CopyFrom(finalHeightMap[i]);
            referenceHeightMapNat.GetSubArray(start, mapSize).CopyFrom(referenceHeightMap[i]);
            roadStrengthMapNat.GetSubArray(start, mapSize).CopyFrom(roadStrengthMap[i]);
        }

        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                for (int k = 0; k < numBiomes; k++)
                {
                    biomeStrengthsNat[i * mapSize * numBiomes + j * numBiomes + k] = info.biomeStrengths[i][j][k];
                }
            }
        }

        pathNat.CopyFrom(path);

        for (int i = 0; i < roadSettingsList.Count; i++)
        {
            roadSettingsNat[i] = new RoadSettingsStruct(roadSettingsList[i]);
        }

        CarvePathJob burstJob = new CarvePathJob
        {
            finalHeightMap = finalHeightMapNat,
            referenceHeightMap = referenceHeightMapNat,
            roadStrengthMap = roadStrengthMapNat,
            path = pathNat,
            roadSettings = roadSettingsNat,
            mapSize = mapSize,
            numBiomes = numBiomes,
            biomeStrengths = biomeStrengthsNat,
        };

        burstJob.Schedule().Complete();
        for (int i = 0; i < mapSize; i++)
        {
            start = i * mapSize;
            finalHeightMapNat.GetSubArray(start, mapSize).CopyTo(finalHeightMap[i]);
            roadStrengthMapNat.GetSubArray(start, mapSize).CopyTo(roadStrengthMap[i]);
        }

        finalHeightMapNat.Dispose();
        referenceHeightMapNat.Dispose();
        pathNat.Dispose();
        roadSettingsNat.Dispose();
        roadStrengthMapNat.Dispose();
        biomeStrengthsNat.Dispose();
    }

    private class Node : IHeapItem<Node>
    {
        public int x;
        public int y;
        public float height;
        public float gCost;
        public float hCost;

        int heapIndex;

        public Node parent;

        public Node(int x, int y, float height)
        {
            this.x = x;
            this.y = y;
            this.height = height;
            gCost = 0;
            hCost = 0;
        }

        public float fCost
        {
            get
            {
                return gCost + hCost;
            }
        }

        public int HeapIndex
        {
            get
            {
                return heapIndex;
            }
            set
            {
                heapIndex = value;
            }
        }

        public int CompareTo(Node nodeToCompare)
        {
            int compare = fCost.CompareTo(nodeToCompare.fCost);
            if (compare == 0)
            {
                compare = hCost.CompareTo(nodeToCompare.hCost);
            }
            return -compare;
        }
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