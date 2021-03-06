using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoadGenerator
{
    private static readonly int[,] offsets = { { 1 , 0}, { 0 , 1}, { -1, 0}, { 0 , -1},
                                               { 1 , 1}, { 1 ,-1}, { -1, 1}, {-1 , -1},
                                               { 2 , 1}, { 2 ,-1}, { -2, 1}, {-2 , -1},
                                               { 1 , 2}, { 1 ,-2}, { -1, 2}, {-1 , -2}};

    private static readonly int numChunkEdgeSmoothPoints = 5; // Number of padded points at start and end of path to help smoothing

    public static RoadData GenerateRoads(TerrainSettings terrainSettings, Vector2 chunkCentre, float[,] heightMap, BiomeInfo info)
    {
        int mapSize = heightMap.GetLength(0);

        List<RoadRoute> routes = GetRoadDestinations(mapSize, chunkCentre);

        int numBiomes = terrainSettings.biomeSettings.Count;
        bool[] roadsEnabled = new bool[numBiomes];

        float[,] roadStrengthMap = new float[mapSize, mapSize];

        List<RoadSettings> roadSettingsList = new List<RoadSettings>();

        for (int i = 0; i < terrainSettings.biomeSettings.Count; i++)
        {
            BiomeGraph graph = terrainSettings.biomeSettings[i].biomeGraph;
            RoadSettings roadSettings = graph.GetRoadSettings();
            roadSettingsList.Add(roadSettings);
        }

        for (int i = 0; i < routes.Count; i++)
        {
            List<RoadData> roadDatas = new List<RoadData>();
            List<Vector3> path = CreatePath(routes[i], heightMap, terrainSettings.maxRoadWidth);

            RoadData data = CreateRoad(path, heightMap, roadSettingsList, info);

            heightMap = data.heightMap;
            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    roadStrengthMap[x, y] = Mathf.Max(data.roadStrengthMap[x, y], roadStrengthMap[x, y]);
                }
            }
        }

        return new RoadData(heightMap, roadStrengthMap);
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

    public static List<Vector3> CreatePath(RoadRoute route, float[,] heightMap, float maxRoadWidth)
    {
        float mapSize = heightMap.GetLength(0);
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
        return SmoothPath(path);
    }

    public static RoadData CreateRoad(
        List<Vector3> path,
        float[,] heightMap,
        List<RoadSettings> roadSettingsList,
        BiomeInfo info
    )
    {
        float[,] workingHeightMap = Common.CopyArray(heightMap);
        float[,] roadStrengthMap = new float[heightMap.GetLength(0), heightMap.GetLength(0)];
        CarvePath(workingHeightMap, heightMap, path, roadSettingsList, roadStrengthMap, info);

        return new RoadData(workingHeightMap, roadStrengthMap);
    }

    private static List<Vector3> FindPath(Vector3 roadStart, Vector3 roadEnd, float[,] heightMap, float maxRoadWidth)
    {
        int mapSize = heightMap.GetLength(0);

        Node[,] nodeGrid = new Node[mapSize, mapSize];
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                nodeGrid[i, j] = new Node(i, j, heightMap[i, j]);
            }
        }

        Node startNode = nodeGrid[(int)roadStart.x, (int)roadStart.z];
        Node endNode = nodeGrid[(int)roadEnd.x, (int)roadEnd.z];

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

    private static List<Node> GetNeighbours(Node[,] nodeGrid, Node node, Node endNode)
    {
        List<Node> neighbours = new List<Node>();
        int mapSize = nodeGrid.GetLength(0);

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

            neighbours.Add(nodeGrid[neighbourX, neighbourY]);
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

    private static List<Vector3> RetracePath(Node node, float[,] heightMap)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = node;
        while (currentNode != null)
        {
            path.Add(new Vector3(currentNode.x, heightMap[currentNode.x, currentNode.y], currentNode.y));
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }

    private static List<Vector3> SmoothPath(List<Vector3> path)
    {
        List<Vector3> smoothedPoints;
        List<Vector3> points;
        int pointsLength = 0;
        int curvedLength = 0;

        pointsLength = path.Count;

        curvedLength = (pointsLength * Mathf.RoundToInt(RoadSettings.smoothness)) - 1;
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
        float[,] workingHeightMap,
        float[,] referenceHeightMap,
        List<Vector3> path,
        List<RoadSettings> roadSettingsList,
        float[,] roadStrengthMap,
        BiomeInfo info
    )
    {
        float maxWidth = 0f;
        for (int i = 0; i < roadSettingsList.Count; i++)
        {
            if (roadSettingsList[i].width > maxWidth)
            {
                maxWidth = roadSettingsList[i].width;
            }
        }
        int[,] closestPathIndexes = FindClosestPathIndexes(referenceHeightMap, path, maxWidth);

        int mapSize = referenceHeightMap.GetLength(0);
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                if (closestPathIndexes[x, y] != -1)
                {
                    AverageRoadSettings averageRoadSettings = CalculateAverageRoadSettings(x, y, roadSettingsList, info);
                    Vector3 closestPointOnLine = ClosestPointOnLine(x, y, referenceHeightMap, closestPathIndexes[x, y], path);
                    Vector3 curPoint = new Vector3(x, referenceHeightMap[x, y], y);
                    CarvePoint(
                        curPoint,
                        closestPointOnLine,
                        workingHeightMap,
                        referenceHeightMap,
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
                if (closestPathIndexes[x, y] != -1)
                {
                    AverageRoadSettings averageRoadSettings = CalculateAverageRoadSettings(x, y, roadSettingsList, info);
                    Vector3 closestPointOnLine = ClosestPointOnLine(x, y, referenceHeightMap, closestPathIndexes[x, y], path);
                    Vector3 curPoint = new Vector3(x, referenceHeightMap[x, y], y);
                    CalculateRoadStrength(
                        curPoint,
                        closestPointOnLine,
                        workingHeightMap,
                        referenceHeightMap,
                        x,
                        y,
                        averageRoadSettings,
                        roadStrengthMap
                    );
                }
            }
        }
    }

    struct AverageRoadSettings
    {
        public float maxAngle;
        public float blendFactor;
        public float width;

        public AverageRoadSettings(float maxAngle, float blendFactor, float width)
        {
            this.maxAngle = maxAngle;
            this.blendFactor = blendFactor;
            this.width = width;
        }
    }

    private static AverageRoadSettings CalculateAverageRoadSettings(int x, int y, List<RoadSettings> roadSettingsList, BiomeInfo info)
    {
        float maxAngle = 0f;
        float blendFactor = 0f;
        float width = 0f;

        for (int biome = 0; biome < info.biomeStrengths.GetLength(2); biome++)
        {
            if (roadSettingsList[biome] != null)
            {
                maxAngle += info.biomeStrengths[x, y, biome] * roadSettingsList[biome].maxAngle;
                blendFactor += info.biomeStrengths[x, y, biome] * roadSettingsList[biome].blendFactor;
                width += info.biomeStrengths[x, y, biome] * roadSettingsList[biome].width;
            }
        }
        return new AverageRoadSettings(maxAngle, blendFactor, width);
    }

    // Finds closest point on path at every point
    private static int[,] FindClosestPathIndexes(float[,] referenceHeightMap, List<Vector3> path, float maxRoadWidth)
    {
        int mapSize = referenceHeightMap.GetLength(0);

        // Check whether a coordinate is approx within roadSettings.width range of 
        // a point on path to determine whether we bother getting the closest path index
        // Points not within this distance dont' matter
        bool[,] getClosestPathIndex = new bool[mapSize, mapSize];
        for (int i = 0; i < path.Count; i++)
        {
            int startX = (int)Mathf.Max(path[i].x - maxRoadWidth, 0);
            int endX = (int)Mathf.Min(mapSize - 1, Mathf.CeilToInt(path[i].x + maxRoadWidth));
            int startZ = (int)Mathf.Max(path[i].z - maxRoadWidth, 0);
            int endZ = (int)Mathf.Min(mapSize - 1, Mathf.CeilToInt(path[i].z + maxRoadWidth));
            for (int x = startX; x <= endX; x++)
            {
                for (int z = startZ; z <= endZ; z++)
                {
                    getClosestPathIndex[x, z] = true;
                }
            }
        }


        int[,] closestPathIndexes = new int[mapSize, mapSize];
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                if (getClosestPathIndex[i, j])
                {
                    Vector3 curPoint = new Vector3(i, referenceHeightMap[i, j], j);

                    float minDist = float.MaxValue;
                    int closestPointIndex = 0;
                    for (int k = 0; k < path.Count; k++)
                    {
                        float dist = (path[k] - curPoint).sqrMagnitude;
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestPointIndex = k;
                        }
                    }
                    closestPathIndexes[i, j] = closestPointIndex;
                }
                else
                {
                    closestPathIndexes[i, j] = -1;
                }
            }
        }
        return closestPathIndexes;
    }

    private static Vector3 ClosestPointOnLine(
        int x,
        int y,
        float[,] referenceHeightMap,
        int closestPointIndex,
        List<Vector3> path
    )
    {
        Vector3 curPoint = new Vector3(x, referenceHeightMap[x, y], y);
        Vector2 curPoint2d = new Vector2(curPoint.x, curPoint.z);

        Vector3 closestPointOnPath = path[closestPointIndex];

        // Get distance of 2nd closest point so we can form a line
        Vector3 secondClosestPoint;
        if (closestPointIndex == path.Count - 1)
        {
            secondClosestPoint = path[closestPointIndex - 1];
        }
        else if (closestPointIndex == 0)
        {
            secondClosestPoint = path[closestPointIndex + 1];
        }
        else
        {
            float distPre = Vector2.Distance(new Vector2(path[closestPointIndex - 1].x, path[closestPointIndex - 1].z), curPoint2d);
            float distPost = Vector2.Distance(new Vector2(path[closestPointIndex + 1].x, path[closestPointIndex + 1].z), curPoint2d);
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

    // Carves out height map at x and y based on curPoint in path and closestPointOnLine of path
    private static void CarvePoint(
        Vector3 curPoint,
        Vector3 closestPointOnLine,
        float[,] workingHeightMap,
        float[,] referenceHeightMap,
        int x,
        int y,
        AverageRoadSettings averageRoadSettings
    )
    {
        int mapSize = workingHeightMap.GetLength(0);

        float distance = Vector2.Distance(new Vector2(closestPointOnLine.x, closestPointOnLine.z), new Vector2(curPoint.x, curPoint.z));
        if (distance > averageRoadSettings.width)
        {
            return;
        }


        // Calculate slope multiplier
        float angle = Common.CalculateAngle(x, y, workingHeightMap);
        float slopeMultiplier = Mathf.Max(0f, 1f - (angle / averageRoadSettings.maxAngle));

        // Calculate edge multipler which we use to not applying terrain carving at edge of map
        float distFromEdgeChunk = Mathf.Min(
            Mathf.Abs(Mathf.Max(x, y) - mapSize),
            Mathf.Abs(Mathf.Min(x, y))
        );
        float edgeMultiplier = Common.SmoothRange(distFromEdgeChunk, 3f, 10f);

        // If within half width of road then fully carve path, otherwise smooth outwards
        float halfRoadWidth = averageRoadSettings.width / 2f;
        if (distance < halfRoadWidth)
        {
            float percentage = distance / halfRoadWidth;
            float roadMultiplier = percentage * averageRoadSettings.blendFactor;
            float newValue = (1f - roadMultiplier) * closestPointOnLine.y + roadMultiplier * curPoint.y;

            workingHeightMap[x, y] = edgeMultiplier * newValue + (1 - edgeMultiplier) * workingHeightMap[x, y];
        }
        else if (distance < averageRoadSettings.width)
        {
            float percentage = (distance - halfRoadWidth) / halfRoadWidth;
            float roadMultiplier = percentage * (1f - averageRoadSettings.blendFactor) + averageRoadSettings.blendFactor;
            float newValue = roadMultiplier * curPoint.y + (1f - roadMultiplier) * closestPointOnLine.y;

            workingHeightMap[x, y] = edgeMultiplier * newValue + (1 - edgeMultiplier) * workingHeightMap[x, y];
        }
    }

    private static void CalculateRoadStrength(
        Vector3 curPoint,
        Vector3 closestPointOnLine,
        float[,] workingHeightMap,
        float[,] referenceHeightMap,
        int x,
        int y,
        AverageRoadSettings averageRoadSettings,
        float[,] roadStrengthMap
    )
    {
        float distance = Vector2.Distance(new Vector2(closestPointOnLine.x, closestPointOnLine.z), new Vector2(curPoint.x, curPoint.z));
        if (distance > averageRoadSettings.width)
        {
            return;
        }

        // Calculate slope multiplier
        float angle = Common.CalculateAngle(x, y, workingHeightMap);
        float slopeMultiplier = Mathf.Max(0f, 1f - (angle / averageRoadSettings.maxAngle));

        // If within half width of road then fully carve path, otherwise smooth outwards
        float halfRoadWidth = averageRoadSettings.width / 2f;
        if (distance < halfRoadWidth)
        {
            roadStrengthMap[x, y] = Mathf.Max(roadStrengthMap[x, y], slopeMultiplier);
        }
        else if (distance < averageRoadSettings.width)
        {
            float percentage = (distance - halfRoadWidth) / halfRoadWidth;

            roadStrengthMap[x, y] = Mathf.Max(roadStrengthMap[x, y], slopeMultiplier * (1f - percentage));
        }
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

    public class RoadRoute
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
    public float[,] heightMap;
    public float[,] roadStrengthMap;

    public RoadData(float[,] heightMap, float[,] roadStrengthMap)
    {
        this.heightMap = heightMap;
        this.roadStrengthMap = roadStrengthMap;
    }
}