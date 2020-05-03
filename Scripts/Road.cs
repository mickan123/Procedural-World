using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Road 
{

    private static readonly int[,] offsets = { { 1 , 0}, { 0 , 1}, { -1, 0}, { 0 , -1},
                                               { 1 , 1}, { 1 ,-1}, { -1, 1}, {-1 , -1},
                                               { 1 , 1}, { 1 ,-1}, { -1, 1}, {-1 , -1},
                                               { 2 , 1}, { 2 ,-1}, { -2, 1}, {-2 , -1},
                                               { 1 , 2}, { 1 ,-2}, { -1, 2}, {-1 , -2}};
                                               
    RoadSettings roadSettings;
    WorldSettings worldSettings;

    Vector3 roadStart;
    Vector3 roadEnd;

    List<Vector3> path;

    List<Vector3> vertices;
    List<int> triangles;
    List<int> quads;
	List<Vector2> uvs;
	List<Vector3> normals;

    public float[,] roadStrengthMap;

    public Road(WorldSettings worldSettings, float[,] heightMap, Vector2 roadStart, Vector2 roadEnd, Vector2 chunkCentre) {
        this.roadSettings = worldSettings.roadSettings;
        this.worldSettings = worldSettings;

        this.roadStart = new Vector3(roadStart.x, HeightFromFloatCoord(roadStart, heightMap), roadStart.y);
        this.roadEnd = new Vector3(roadEnd.x, HeightFromFloatCoord(roadEnd, heightMap), roadEnd.y);

        path = new List<Vector3>();

        vertices = new List<Vector3>();
        triangles = new List<int>();
        quads = new List<int>();
        uvs = new List<Vector2>();
        normals = new List<Vector3>();

        roadStrengthMap = new float[heightMap.GetLength(0), heightMap.GetLength(1)];

        float startTime = Time.realtimeSinceStartup;

        CreateRoad(heightMap);

        float endTime = Time.realtimeSinceStartup;

        Debug.Log("Road Generation time taken: " + (endTime - startTime));
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

    private void CreateRoad(float[,] heightMap) {
        FindPath(heightMap);
        SmoothPath();

        float[,] workingHeightMap = Common.CopyArray(heightMap);

        CarvePath(workingHeightMap, workingHeightMap);

        Common.CopyArrayValues(workingHeightMap, heightMap);
    }

    private void FindPath(float[,] heightMap) {
        
        int mapSize = heightMap.GetLength(0);

        Node[,] nodeGrid = new Node[mapSize, mapSize];
        for (int i = 0; i < mapSize; i++) {
            for (int j = 0; j < mapSize; j++) {
                nodeGrid[i, j] = new Node(i, j, heightMap[i, j]);
            }
        }

        Node startNode = nodeGrid[(int)this.roadStart.x, (int)this.roadStart.z];
        Node endNode = nodeGrid[(int)this.roadEnd.x, (int)this.roadEnd.z];

        Heap<Node> openSet = new Heap<Node>(mapSize * mapSize);
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);


        while (openSet.Count > 0) {
            Node currentNode = openSet.RemoveFirst();
            closedSet.Add(currentNode);

            if (currentNode == endNode) {
                RetracePath(currentNode, heightMap);
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

        float angle;
        if (a.parent == null) {
            angle = 0f;
        }
        else {
            Vector2 prevDir = new Vector2(a.x - a.parent.x, a.y - a.parent.y);
            Vector2 curDir = new Vector2(b.x - a.x, b.y - a.y);
            angle = Vector2.Angle(prevDir, curDir);
        }
        
        float slope = heightDiff / flatDist;
        float slopeCost = 1000 * slope * slope; 
        
        // Penalize being close to edge of chunk
        float edgeCost = 0f;
        float halfWidth = roadSettings.width / 2f;
        if (b.x < halfWidth || b.x > mapSize - 1 -  halfWidth  
            || b.y < halfWidth || b.y > mapSize - 1 - halfWidth
            || angle >= 90f) {
            edgeCost = 1000000000f;
        }

        return flatDist + slopeCost + edgeCost;
    }

    private void RetracePath(Node node, float[,] heightMap) {
        Node currentNode = node;
        while (currentNode.parent != null) {
            path.Add(new Vector3(currentNode.x, heightMap[currentNode.x, currentNode.y], currentNode.y));
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

                if (distance < (roadSettings.width / 2)) {
                    float height = HeightFromFloatCoord(closestPointOnLine2d, referenceHeightMap);
                    float percent = distance / (roadSettings.width / 2f);
                    float newValue = (1f - roadSettings.blendFactor * percent) * height + (roadSettings.blendFactor * percent) * curPoint.y;

                    workingHeightMap[i, j] = newValue;
                    roadStrengthMap[i, j] = 1f;
                }
                else if (distance < roadSettings.width) {
                    float height = HeightFromFloatCoord(closestPointOnLine2d, referenceHeightMap);
                    float halfWidth = roadSettings.width / 2f;
                    float multiplier = (distance - halfWidth) / halfWidth;
                    multiplier = multiplier * (1f - roadSettings.blendFactor) + roadSettings.blendFactor;
                    float newValue = multiplier * curPoint.y + (1f - multiplier) * height;

                    workingHeightMap[i, j] = newValue;
                    roadStrengthMap[i, j] = 1f - (distance - halfWidth) / halfWidth;
                }
                else {
                    roadStrengthMap[i, j] = 0;
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
        return origin + direction * dotP;
    }

    private void AddMeshSquare(float[,] heightMap, int x, int y) {

        this.vertices.Add(new Vector3(x, heightMap[x, y + 1] + 0.05f, y + 1));
        this.vertices.Add(new Vector3(x + 1, heightMap[x + 1, y + 1] + 0.05f, y + 1));
        this.vertices.Add(new Vector3(x, heightMap[x, y] + 0.05f, y));
        this.vertices.Add(new Vector3(x + 1, heightMap[x + 1, y] + 0.05f, y));
        
        this.uvs.Add(new Vector2(0, 1));
        this.uvs.Add(new Vector2(1, 1));
        this.uvs.Add(new Vector2(0, 0));
        this.uvs.Add(new Vector2(1, 0));
        
        this.normals.Add(Vector3.up);
        this.normals.Add(Vector3.up);
        this.normals.Add(Vector3.up);
        this.normals.Add(Vector3.up);

        int triangleOffset = this.vertices.Count - 4;
        int[] triangles = {
            triangleOffset, triangleOffset + 1, triangleOffset + 2,	// triangle 1
            triangleOffset + 2, triangleOffset + 1, triangleOffset + 3,	// triangle 2
        };

        this.triangles.AddRange(triangles);
    }

    private void CreateMesh() {
        Mesh mesh = new Mesh(); 
        
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.normals = normals.ToArray();

        // this.meshFilter.mesh = mesh;
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

