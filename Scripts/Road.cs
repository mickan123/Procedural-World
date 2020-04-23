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

    GameObject meshObject;

    MeshRenderer meshRenderer;
	MeshFilter meshFilter;

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

    LineRenderer lineRenderer;

    public Road(WorldSettings worldSettings, float[,] heightMap, Vector2 roadStart, Vector2 roadEnd, Vector2 chunkCentre) {
        this.roadSettings = worldSettings.roadSettings;
        this.worldSettings = worldSettings;

        meshObject = new GameObject("Road");
        this.lineRenderer = meshObject.AddComponent<LineRenderer>();
        this.lineRenderer.material = roadSettings.roadMaterial;
        this.lineRenderer.startWidth = roadSettings.width;
        this.lineRenderer.endWidth = roadSettings.width;
        this.lineRenderer.alignment = LineAlignment.TransformZ;

		meshRenderer = meshObject.AddComponent<MeshRenderer>();
		meshFilter = meshObject.AddComponent<MeshFilter>();

        float mapSize = (float)heightMap.GetLength(0);
        meshObject.transform.position = new Vector3(chunkCentre.x, 0.01f, chunkCentre.y);

        this.roadStart = new Vector3(roadStart.x, HeightFromFloatCoord(roadStart, heightMap), roadStart.y);
        this.roadEnd = new Vector3(roadEnd.x, HeightFromFloatCoord(roadEnd, heightMap), roadEnd.y);

        path = new List<Vector3>();

        vertices = new List<Vector3>();
        triangles = new List<int>();
        quads = new List<int>();
        uvs = new List<Vector2>();
        normals = new List<Vector3>();

        float startTime = Time.realtimeSinceStartup;

        CreateRoad(heightMap);

        float endTime = Time.realtimeSinceStartup;

        Debug.Log("Time taken: " + (endTime - startTime));
    }

    private float HeightFromFloatCoord(Vector2 coord, float[,] heightMap) {
        int maxIndex = heightMap.GetLength(0) - 1;
        int indexX = (int)coord.x;
        int indexY = (int)coord.y;

        float x = indexX - coord.x;
        float y = indexY - coord.y;

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

        float[,] workingHeightMap = Common.CopyArray(heightMap);

        CarvePath(workingHeightMap, workingHeightMap);

        Common.CopyArrayValues(workingHeightMap, heightMap);

        // lineRenderer.positionCount = path.Count;
        // lineRenderer.SetPositions(path.ToArray());

        AddMeshValues(heightMap);
        CreateMesh();
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
            
            List<Node> neighbours = GetNeighbours(nodeGrid, currentNode);
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

    private List<Node> GetNeighbours(Node[,] nodeGrid, Node node) {
        List<Node> neighbours = new List<Node>();
        int mapSize = nodeGrid.GetLength(0);

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
        float slopeCost = 100 * slope * slope; 
        
        // Penalize being close to edge of chunk
        float edgeCost = 0f;
        if (b.x < roadSettings.width || b.x > mapSize - 1 -  roadSettings.width 
            || b.y < roadSettings.width || b.y > mapSize - 1 - roadSettings.width
            || angle >= 90f) {
            edgeCost = 1000000000f;
        }

        return flatDist * (1 + slopeCost) + edgeCost;
    }

    private void RetracePath(Node node, float[,] heightMap) {
        Node currentNode = node;
        while (currentNode.parent != null) {
            path.Add(new Vector3(currentNode.x, heightMap[currentNode.x, currentNode.y], currentNode.y));
            currentNode = currentNode.parent;
        }

        path.Reverse();
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
        Vector2 lhs = point - origin;
        float dotP = Vector3.Dot(lhs, direction);
        return origin + direction * dotP;
    }

    private void CarvePath(float[,] workingHeightMap, float[,] referenceHeightMap) {
        
        int mapSize = referenceHeightMap.GetLength(0);
        for (int i = 0; i < mapSize; i++) {
            for (int j = 0; j < mapSize; j++) {
                Vector3 curPoint = new Vector3(i, referenceHeightMap[i, j], j);

                int closestPointIndex = FindClosestPointIndex(curPoint);
                Vector3 closestPoint = path[closestPointIndex]; 

                // Get distance of 2nd closest point so we can form a line
                Vector3 secondClosestPoint;
                if (i == path.Count) {
                    secondClosestPoint = path[i - 1];
                }
                else if (i == 0) {
                    secondClosestPoint = path[i + 1];
                }
                else {
                    float distPre = Vector3.Distance(path[i - 1], curPoint);
                    float distPost = Vector3.Distance(path[i + 1], curPoint);
                    secondClosestPoint = distPre < distPost ? path[i - 1] : path[i + 1];
                }
                
                Vector3 closestPointOnLine = ClosestPointOnLine(curPoint, closestPoint, secondClosestPoint - closestPoint);
                float distance = Vector3.Distance(closestPoint, curPoint);

                if (distance < (roadSettings.width / 2)) {
                    float percent = distance / roadSettings.width;
                    float newValue = 0.25f * curPoint.y + 0.75f * closestPoint.y;

                    workingHeightMap[(int)curPoint.x, (int)curPoint.z] = newValue;
                }
            }
        }
    }

    private void AddMeshValues(float[,] heightMap) {

        int mapSize = heightMap.GetLength(0);

        for (int i = 0; i < mapSize - 1; i++) {
            for (int j = 0; j < mapSize - 1; j++) {
                Vector3 curPoint = new Vector3(i, heightMap[i, j] + 0.01f, j);

                int closestPointIndex = FindClosestPointIndex(curPoint);
                Vector3 closestPoint = path[closestPointIndex]; 

                float distance = Vector3.Distance(closestPoint, curPoint);

                if (distance < (roadSettings.width / 2)) {
                    AddMeshSquare(heightMap, i, j);
                }
            }
        }
    }

    private void AddMeshSquare(float[,] heightMap, int x, int y) {

        this.vertices.Add(new Vector3(x, heightMap[x, y + 1] + 0.05f, y + 1));
        this.vertices.Add(new Vector3(x + 1, heightMap[x + 1, y + 1] + 0.01f, y + 1));
        this.vertices.Add(new Vector3(x, heightMap[x, y] + 0.01f, y));
        this.vertices.Add(new Vector3(x + 1, heightMap[x + 1, y] + 0.01f, y));
        
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

        this.meshFilter.mesh = mesh;
    }
    
    public void SetVisible(bool visible) {
		meshObject.SetActive(visible);
	}

    public void SetParent(Transform parent) {
        this.meshFilter.transform.parent = parent;
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

    private class Rectangle {

        public Vector2[] points;

        public Rectangle(Vector2 a, Vector2 b, Vector2 c, Vector2 d) {
            points = new Vector2[4];
            points[0] = a;
            points[1] = b;
            points[2] = c;
            points[3] = d;
        }

        public bool Contains(Vector2 p) {

            for (int i = 0; i < points.Length; i++) {
                if (p == points[i]) {
                    return true;
                }
            }

            bool inside = false;
            int j = points.Length - 1;
            for (int i = 0; i < points.Length; j = i++) {
                Vector2 pi = points[i];
                Vector2 pj = points[j];
                if (((pi.y <= p.y && p.y < pj.y) || (pj.y <= p.y && p.y < pi.y)) &&
                    (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x)) {
                    inside = !inside;
                }    
            }

            return inside;
        }
    }
}

