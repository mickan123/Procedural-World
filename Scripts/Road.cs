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
    WorldSettings worldSettings;

    List<Vector3> path;

    public float[,] roadStrengthMap;

    private float[,] heightMap;
    private BiomeInfo biomeInfo;

    public Road(WorldSettings worldSettings, float[,] heightMap, BiomeInfo info, List<RoadRoute> roadRoutes, Vector2 chunkCentre) {
        this.roadSettings = worldSettings.roadSettings;
        this.worldSettings = worldSettings;

        this.heightMap = heightMap;
        this.biomeInfo = info;

        roadStrengthMap = new float[heightMap.GetLength(0), heightMap.GetLength(1)];

        for (int i = 0; i < roadRoutes.Count; i++) {
            path = new List<Vector3>();

            Vector3 roadStart3d = new Vector3(roadRoutes[i].roadStart.x, HeightFromFloatCoord(roadRoutes[i].roadStart, heightMap), roadRoutes[i].roadStart.y);
            Vector3 roadEnd3d = new Vector3(roadRoutes[i].roadEnd.x, HeightFromFloatCoord(roadRoutes[i].roadEnd, heightMap), roadRoutes[i].roadEnd.y);

            CreateRoad(roadStart3d, roadEnd3d);
        }
        
    }

    private float HeightFromFloatCoord(Vector2 coord, float[,] heightMap) {
        int maxIndex = heightMap.GetLength(0) - 1;
        int indexX = Mathf.Clamp((int)coord.x, 0, maxIndex);
        int indexY = Mathf.Clamp((int)coord.y, 0, maxIndex);
        
        float x = coord.x - indexX;
        float y = coord.y - indexY;

        float heightNW = heightMap[indexX, indexY];
        float heightNE = heightMap[indexX, Mathf.Min(indexY + 1, maxIndex)];
        float heightSW = heightMap[Mathf.Min(indexX + 1, maxIndex), indexY];
        float heightSE = heightMap[Mathf.Min(indexX + 1, maxIndex), Mathf.Min(indexY + 1, maxIndex)];

        float height = heightNW * (1 - x) * (1 - y) 
                     + heightNE *  x      * (1 - y) 
                     + heightSW * (1 - x) * y
                     + heightSE *  x      * y;       
        
        return height;
    }

    private void CreateRoad(Vector3 roadStart, Vector3 roadEnd) {
        FindPath(roadStart, roadEnd);
        SmoothPath();

        float[,] workingHeightMap = Common.CopyArray(this.heightMap);

        CarvePath(workingHeightMap, workingHeightMap);

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
        while (currentNode.parent != null) {
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
            
            smoothedPoints.Add(points[0]);
        }
        this.path = smoothedPoints;

    }

    private void CarvePath(float[,] workingHeightMap, float[,] referenceHeightMap) {

        int mapSize = referenceHeightMap.GetLength(0);
        for (int i = 0; i < mapSize; i++) {
            for (int j = 0; j < mapSize; j++) {
                Vector3 curPoint = new Vector3(i, referenceHeightMap[i, j], j);
                Vector2 curPoint2d = new Vector2(curPoint.x, curPoint.z);

                int closestPointIndex = FindClosestPointIndex(curPoint);
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
                
                Vector3 closestPointOnLine = ClosestPointOnLine(curPoint, closestPoint, secondClosestPoint - closestPoint);
                Vector2 closestPointOnLine2d = new Vector2(closestPointOnLine.x, closestPointOnLine.z);

                float distance = Vector2.Distance(closestPointOnLine2d, curPoint2d);
                float roadMultiplier = 0f;
                for (int w = 0; w < worldSettings.biomes.Length; w++) {
                    if (worldSettings.biomes[w].allowRoads) {
                        roadMultiplier += biomeInfo.biomeStrengths[i, j, w];
                    }
                }

                if (distance < (roadSettings.width / 2)) {
                    float height = HeightFromFloatCoord(closestPointOnLine2d, referenceHeightMap);
                    float percent = distance / (roadSettings.width / 2f);
                    float newValue = (1f - roadSettings.blendFactor * percent) * closestPointOnLine.y + (roadSettings.blendFactor * percent) * curPoint.y;

                    workingHeightMap[i, j] = roadMultiplier * newValue + (1 - roadMultiplier) * workingHeightMap[i, j];
                    roadStrengthMap[i, j] = 1f * roadMultiplier;
                }
                else if (distance < roadSettings.width) {
                    float height = HeightFromFloatCoord(closestPointOnLine2d, referenceHeightMap);
                    float halfWidth = roadSettings.width / 2f;
                    float multiplier = (distance - halfWidth) / halfWidth;
                    multiplier = multiplier * (1f - roadSettings.blendFactor) + roadSettings.blendFactor;
                    float newValue = multiplier * curPoint.y + (1f - multiplier) * closestPointOnLine.y;

                    workingHeightMap[i, j] = roadMultiplier * newValue + (1 - roadMultiplier) * workingHeightMap[i, j];
                    roadStrengthMap[i, j] = (1f - (distance - halfWidth) / halfWidth) * roadMultiplier;
                }
            }
        }
    }

    private int FindClosestPointIndex(Vector3 curPoint) {
        float minDist = float.MaxValue;
        int closestPointIndex = 0;

        for (int i = 0; i < path.Count; i++) {
            float dist = Vector3.Distance(path[i], curPoint);
            if (dist < minDist) {
                minDist = dist;
                closestPointIndex = i;
            }
        }

        return closestPointIndex;
    }

    private float DistanceFromLine(Vector3 point, Vector3 origin, Vector3 direction) {
        Ray ray = new Ray(origin, direction);
        float distance = Vector3.Cross(ray.direction, point - ray.origin).magnitude;
        return distance;
    }

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

