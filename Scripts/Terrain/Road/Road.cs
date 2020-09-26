using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Road 
{

    private static readonly int[,] offsets = { { 1 , 0}, { 0 , 1}, { -1, 0}, { 0 , -1},
                                               { 1 , 1}, { 1 ,-1}, { -1, 1}, {-1 , -1},
                                               { 2 , 1}, { 2 ,-1}, { -2, 1}, {-2 , -1},
                                               { 1 , 2}, { 1 ,-2}, { -1, 2}, {-1 , -2}};
                                               
    RoadSettings roadSettings;
    TerrainSettings terrainSettings;

    List<Vector3> path;

    public float[,] roadStrengthMap;
    private float[,] heightMap;
    private float[,] originalHeightMap;
    private BiomeInfo biomeInfo;

    public Road(TerrainSettings terrainSettings, float[,] heightMap, BiomeInfo info, List<RoadRoute> roadRoutes, Vector2 chunkCentre) {
        this.roadSettings = terrainSettings.roadSettings;
        this.terrainSettings = terrainSettings;

        this.heightMap = heightMap;
        this.biomeInfo = info;

        originalHeightMap = new float[heightMap.GetLength(0), heightMap.GetLength(1)];
        Common.CopyArrayValues(this.heightMap, this.originalHeightMap);

        float mapSize = this.heightMap.GetLength(0);

        roadStrengthMap = new float[heightMap.GetLength(0), heightMap.GetLength(1)];

        for (int i = 0; i < roadRoutes.Count; i++) {
            path = new List<Vector3>();

            Vector3 roadStart = new Vector3(roadRoutes[i].roadStart.x, Common.HeightFromFloatCoord(roadRoutes[i].roadStart, heightMap), roadRoutes[i].roadStart.y);
            Vector3 roadEnd = new Vector3(roadRoutes[i].roadEnd.x, Common.HeightFromFloatCoord(roadRoutes[i].roadEnd, heightMap), roadRoutes[i].roadEnd.y);

            // Create second point perpendicular to edge from start and end points to make sure last part of path is perpendsicular to edge of chunk
            Vector3 roadStart2nd = roadStart + new Vector3((roadStart.x == 0) ? 5 : (roadStart.x == mapSize - 1) ? -5 : 0,
                                                            0,
                                                           (roadStart.z == 0) ? 5 : (roadStart.z == mapSize - 1) ? -5 : 0);
            Vector3 roadEnd2nd = roadEnd + new Vector3((roadEnd.x == 0) ? 5 : (roadEnd.x == mapSize - 1) ? -5 : 0,
                                                        0,
                                                       (roadEnd.z == 0) ? 5 : (roadEnd.z == mapSize - 1) ? -5 : 0);

            CreateRoad(roadStart, roadEnd, roadStart2nd, roadEnd2nd);
        }
    }

    

    private void CreateRoad(Vector3 roadStart, Vector3 roadEnd, Vector3 roadStart2nd, Vector3 roadEnd2nd) {
        #if (UNITY_EDITOR && PROFILE)
        float pathFindStartTime = 0f;
        if (terrainSettings.IsMainThread()) {
            pathFindStartTime = Time.realtimeSinceStartup;
        }
        #endif

        FindPath(roadStart2nd, roadEnd2nd);

        #if (UNITY_EDITOR && PROFILE)
        if (terrainSettings.IsMainThread()) {
            float pathFindEndTime = Time.realtimeSinceStartup;
            float pathFindTimeTaken = pathFindEndTime - pathFindStartTime;
            Debug.Log("Path finding time taken: " + pathFindTimeTaken + "s");
        }
        #endif

        // The more times we add start and end points smoother end and start of path will be
        for (int i = 0; i < 5; i++) {
            this.path.Insert(0, roadStart);
            this.path.Add(roadEnd);
        }
        SmoothPath();

        float[,] workingHeightMap = Common.CopyArray(this.heightMap);

        #if (UNITY_EDITOR && PROFILE)
        float pathCarveStartTime = 0f;
        if (terrainSettings.IsMainThread()) {
            pathCarveStartTime = Time.realtimeSinceStartup;
        }
        #endif

        CarvePath(workingHeightMap, workingHeightMap);

        #if (UNITY_EDITOR && PROFILE)
        if (terrainSettings.IsMainThread()) {
            float pathCarveEndTime = Time.realtimeSinceStartup;
            float pathCarveTimeTaken = pathCarveEndTime - pathCarveStartTime;
            Debug.Log("Path carving time taken: " + pathCarveTimeTaken + "s");
        }
        #endif

        Common.CopyArrayValues(workingHeightMap, this.heightMap);
    }

    private void FindPath(Vector3 roadStart, Vector3 roadEnd) {
        
        int mapSize = heightMap.GetLength(0);

        Node[,] nodeGrid = new Node[mapSize, mapSize];
        for (int i = 0; i < mapSize; i++) {
            for (int j = 0; j < mapSize; j++) {
                nodeGrid[i, j] = new Node(i, j, this.heightMap[i, j]);
            }
        }

        Node startNode = nodeGrid[(int)roadStart.x, (int)roadStart.z];
        Node endNode = nodeGrid[(int)roadEnd.x, (int)roadEnd.z];

        Heap<Node> openSet = new Heap<Node>(mapSize * mapSize);
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);


        while (openSet.Count > 0) {
            Node currentNode = openSet.RemoveFirst();
            closedSet.Add(currentNode);

            if (currentNode == endNode) {
                RetracePath(currentNode);
                return;
            }
            
            List<Node> neighbours = GetNeighbours(nodeGrid, currentNode, endNode);
            for (int i = 0; i < neighbours.Count; i++) {
                Node neighbour = neighbours[i];

                if (closedSet.Contains(neighbour)) {
                    continue;
                }

                float costToNeighbour = currentNode.gCost + GetCost(currentNode, neighbour, mapSize);
                if (costToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {
                    neighbour.gCost = costToNeighbour;
                    neighbour.hCost = GetCost(neighbour, endNode, mapSize);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour)) {
                        openSet.Add(neighbour);
                    } else {
                        openSet.UpdateItem(neighbour);
                    }
                }
            }
        }
    }

    private List<Node> GetNeighbours(Node[,] nodeGrid, Node node, Node endNode) {
        List<Node> neighbours = new List<Node>();
        int mapSize = nodeGrid.GetLength(0);
        
        float deltaXend = Mathf.Abs(endNode.x - node.x);
        float deltaYend = Mathf.Abs(endNode.y - node.y);
        float distanceToEndnode = deltaXend * deltaXend + deltaYend * deltaYend;

        if (distanceToEndnode < roadSettings.stepSize * 2f) {
            neighbours.Add(endNode);
        }

        for (int i = 0; i < offsets.GetLength(0); i++) {
            int neighbourX = node.x + offsets[i, 0] * roadSettings.stepSize;
            int neighbourY = node.y + offsets[i, 1] * roadSettings.stepSize;

            neighbourX = Mathf.Clamp(neighbourX, 0, mapSize - 1);
            neighbourY = Mathf.Clamp(neighbourY, 0, mapSize - 1);
            
            neighbours.Add(nodeGrid[neighbourX, neighbourY]);
        }

        return neighbours;
    }

    private float GetCost(Node a, Node b, int mapSize) {

        float deltaX = a.x - b.x;
        float deltaY = a.y - b.y;
        float flatDist =  Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);

        float heightDiff = Mathf.Abs(a.height - b.height);
        
        float slope = heightDiff / flatDist;
        float slopeCost = slope; 
        
        // Penalize being close to edge of chunk
        float edgeCost = 0f;
        float halfWidth = roadSettings.width / 2f;
        if (b.x < halfWidth || b.x > mapSize - 1 -  halfWidth  
            || b.y < halfWidth || b.y > mapSize - 1 - halfWidth) {
            edgeCost = 100000f;
        }

        return slopeCost + edgeCost;
    }

    private void RetracePath(Node node) {
        Node currentNode = node;
        while (currentNode != null) {
            path.Add(new Vector3(currentNode.x, this.heightMap[currentNode.x, currentNode.y], currentNode.y));
            currentNode = currentNode.parent;
        }

        path.Reverse();
    }

    private void SmoothPath() {
        List<Vector3> smoothedPoints;
        List<Vector3> points;
        int pointsLength = 0;
        int curvedLength = 0;

        pointsLength = this.path.Count;
         
        curvedLength = (pointsLength * Mathf.RoundToInt(this.roadSettings.smoothness)) - 1;
        smoothedPoints = new List<Vector3>(curvedLength);

        float t = 0.0f;
        for(int pointInTimeOnCurve = 0; pointInTimeOnCurve < curvedLength + 1; pointInTimeOnCurve++){
            t = Mathf.InverseLerp(0, curvedLength, pointInTimeOnCurve);
            
            points = new List<Vector3>(this.path);
            
            for(int j = pointsLength - 1; j > 0; j--){
                for (int i = 0; i < j; i++){
                    points[i] = (1 - t) * points[i] + t * points[i + 1];
                }
            }

            // Check to see if previous paths have edited the height map nearby
            bool averageWithCurrent = false;
            int range = 10;
            for (int i = -range; i <= range; i++) {
                for (int j = -range; j <= range; j++) {
                    float originalY = Common.HeightFromFloatCoord(points[0].x + i, points[0].z + j, this.originalHeightMap);
                    float currentY = Common.HeightFromFloatCoord(points[0].x + i, points[0].z + j, this.heightMap);
                    if (originalY != currentY) {
                        averageWithCurrent = true;
                    }
                }
            }
            
            if (averageWithCurrent) {
                float newY = (Common.HeightFromFloatCoord(points[0].x, points[0].z, this.heightMap) + points[0].y) /2f;
                smoothedPoints.Add(new Vector3(points[0].x, newY, points[0].z));
            }
            else {
                smoothedPoints.Add(points[0]);
            }   
        }
        this.path = smoothedPoints;
    }

    private void CarvePath(float[,] workingHeightMap, float[,] referenceHeightMap) {

        int mapSize = referenceHeightMap.GetLength(0);

        int[,] closestPathIndexes = new int[mapSize, mapSize];
        FindClosestPathIndexes(closestPathIndexes, referenceHeightMap);

        for (int i = 0; i < mapSize; i++) {
            for (int j = 0; j < mapSize; j++) {
                
                Vector3 curPoint = new Vector3(i, referenceHeightMap[i, j], j);
                Vector2 curPoint2d = new Vector2(curPoint.x, curPoint.z);

                int closestPointIndex = closestPathIndexes[i, j];
                Vector3 closestPoint = path[closestPointIndex]; 
                
                // Get distance of 2nd closest point so we can form a line
                Vector3 secondClosestPoint;
                if (closestPointIndex == path.Count - 1) {
                    secondClosestPoint = path[closestPointIndex - 1];
                }
                else if (closestPointIndex == 0) {
                    secondClosestPoint = path[closestPointIndex + 1];
                }
                else {
                    float distPre = Vector2.Distance(new Vector2(path[closestPointIndex - 1].x, path[closestPointIndex - 1].z), curPoint2d);
                    float distPost = Vector2.Distance(new Vector2(path[closestPointIndex + 1].x, path[closestPointIndex + 1].z), curPoint2d);
                    secondClosestPoint = distPre < distPost ? path[closestPointIndex - 1] : path[closestPointIndex + 1];
                }

                // Get distance from point to line
                Vector3 closestPointOnLine = ClosestPointOnLine(curPoint, closestPoint, secondClosestPoint - closestPoint);
                CarvePoint(curPoint, closestPointOnLine, workingHeightMap, i, j);
            }
        }
    }

    // Finds closest point on path at every point
    private void FindClosestPathIndexes(int[,] closestPathIndexes, float[,] referenceHeightMap) {
        int mapSize = referenceHeightMap.GetLength(0);
        for (int i = 0; i < mapSize; i++) {
            for (int j = 0; j < mapSize; j++) {
                Vector3 curPoint = new Vector3(i, referenceHeightMap[i, j], j);
                closestPathIndexes[i, j] = FindClosestPathIndex(curPoint);
            }
        }
    }

     // Finds index of closest point on the path
    private int FindClosestPathIndex(Vector3 curPoint) {
        
        float minDist = float.MaxValue;
        int closestPointIndex = 0;

        for (int i = 0; i < path.Count; i++) {
            float dist = (path[i] - curPoint).sqrMagnitude;
            if (dist < minDist) {
                minDist = dist;
                closestPointIndex = i;
            }
        }

        return closestPointIndex;
    }

    // Finds the closest point on a line of origin and direction from 'point'
    private Vector3 ClosestPointOnLine(Vector3 point, Vector3 origin, Vector3 direction) {
        direction.Normalize();
        Vector3 lhs = point - origin;
        float dotP = Vector3.Dot(lhs, direction);

        Vector3 closestPoint = origin + direction * dotP;

        // Need to clamp closestpoint within roadwidth of the start and endpoints of the line
        Vector3 clampedClosestPoint = new Vector3(Mathf.Clamp(closestPoint.x, origin.x - 2*roadSettings.width, origin.x + direction.x + 2*roadSettings.width),
                                                  Mathf.Clamp(closestPoint.y, origin.y - 2*roadSettings.width, origin.y + direction.y + 2*roadSettings.width),
                                                  Mathf.Clamp(closestPoint.z, origin.z - 2*roadSettings.width, origin.z + direction.z + 2*roadSettings.width));

        return clampedClosestPoint;
    }

    // Carves out height map at x and y based on curPoint in path and closestPointOnLine of path
    private void CarvePoint(Vector3 curPoint, Vector3 closestPointOnLine, float[,] workingHeightMap, int x, int y) {
        int mapSize = workingHeightMap.GetLength(0);

        float distance = Vector2.Distance(new Vector2(closestPointOnLine.x, closestPointOnLine.z), new Vector2(curPoint.x, curPoint.z));

        // Calculate roadMultiplier dependent on which biomes have roads enabled
        float biomeRoadMultiplier = 0f;
        for (int w = 0; w < terrainSettings.biomeSettings.Count; w++) {
            if (terrainSettings.biomeSettings[w].allowRoads) {
                biomeRoadMultiplier += biomeInfo.biomeStrengths[x, y, w];
            }
        }

        // Calculate edge multipler which we use to not applying terrain carving at edge of map
        float distFromEdgeChunk = Mathf.Min(
                                    Mathf.Abs(Mathf.Max(x, y) - mapSize), 
                                    Mathf.Abs(Mathf.Min(x, y))
                                    );
        float edgeMultiplier = Common.SmoothRange(distFromEdgeChunk, 3f, 10f);
        float finalValueMultiplier = edgeMultiplier * biomeRoadMultiplier;

        // If within half width of road then fully carve path, otherwise smooth outwards
        float halfRoadWidth = roadSettings.width / 2f;
        if (distance < halfRoadWidth) {
            
            float percentage = distance / halfRoadWidth;
            float roadMultiplier = percentage * roadSettings.blendFactor;
            float newValue = (1f - roadMultiplier) * closestPointOnLine.y + roadMultiplier * curPoint.y;
            
            workingHeightMap[x, y] = finalValueMultiplier * newValue + (1 - finalValueMultiplier) * workingHeightMap[x, y];
            roadStrengthMap[x, y] = Mathf.Max(roadStrengthMap[x, y], 1f * biomeRoadMultiplier);
        }
        else if (distance < roadSettings.width) {
            float percentage = (distance - halfRoadWidth) / halfRoadWidth;
            float roadMultiplier = percentage * (1f - roadSettings.blendFactor) + roadSettings.blendFactor;
            float newValue = roadMultiplier * curPoint.y + (1f - roadMultiplier) * closestPointOnLine.y;

            workingHeightMap[x, y] = finalValueMultiplier * newValue + (1 - finalValueMultiplier) * workingHeightMap[x, y];
            roadStrengthMap[x, y] = Mathf.Max(roadStrengthMap[x, y], (1f - percentage) * biomeRoadMultiplier);
        }
    }

    // Finds the distance of a point from a line of origin and direction
    private float DistanceFromLine(Vector3 point, Vector3 origin, Vector3 direction) {
        Ray ray = new Ray(origin, direction);
        float distance = Vector3.Cross(ray.direction, point - ray.origin).magnitude;
        return distance;
    }

    private class Node : IHeapItem<Node> {
        public int x;
        public int y;
        public float height;
        public float gCost;
        public float hCost;

        int heapIndex;

        public Node parent;
        
        public Node(int x, int y, float height) {
            this.x = x;
            this.y = y;
            this.height = height;
            gCost = 0;
            hCost = 0;
        }

        public float fCost {
            get {
                return gCost + hCost;
            }
        }

        public int HeapIndex {
            get {
                return heapIndex;
            }
            set {
                heapIndex = value;
            }
        }

        public int CompareTo(Node nodeToCompare) {
            int compare = fCost.CompareTo(nodeToCompare.fCost);
            if (compare == 0) {
                compare = hCost.CompareTo(nodeToCompare.hCost);
            }
            return -compare;
        }
    }
}