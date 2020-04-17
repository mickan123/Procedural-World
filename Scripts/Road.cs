using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Road 
{
    GameObject meshObject;

    MeshRenderer meshRenderer;
	MeshFilter meshFilter;

    RoadSettings roadSettings;
    WorldSettings worldSettings;

    Vector3 roadStart;
    Vector3 roadEnd;
    Vector3 prevPoint;

    List<Vector3> vertices;
    List<int> triangles;
	List<Vector2> uvs;
	List<Vector3> normals;

    public Road(WorldSettings worldSettings, float[,] heightMap, Vector2 roadStart, Vector2 roadEnd, Vector2 chunkCentre) {
        meshObject = new GameObject("Road");
		meshRenderer = meshObject.AddComponent<MeshRenderer>();
		meshFilter = meshObject.AddComponent<MeshFilter>();

        float mapSize = (float)heightMap.GetLength(0);
        meshObject.transform.position = new Vector3(chunkCentre.x, 0.01f, chunkCentre.y);

        this.roadStart = new Vector3(roadStart.x, HeightFromFloatCoord(roadStart, heightMap), roadStart.y);
        this.roadEnd = new Vector3(roadEnd.x, HeightFromFloatCoord(roadEnd, heightMap), roadEnd.y);
        this.prevPoint = this.roadStart;

        this.roadSettings = worldSettings.roadSettings;
        this.worldSettings = worldSettings;

        vertices = new List<Vector3>();
        triangles = new List<int>();
        uvs = new List<Vector2>();
        normals = new List<Vector3>();

        CreateRoad(heightMap, roadStart, roadEnd);
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

    private void CreateRoad(float[,] heightMap, Vector3 roadStart, Vector3 roadEnd) {
        List<Vector3> path = GeneratePath(heightMap, roadStart, roadEnd);

        float[,] workingHeightMap = Common.CopyArray(heightMap);

        for (int i = 0; i < path.Count; i++) {
            AddPoint(workingHeightMap, heightMap, path[i]);
        }
        
        Common.CopyArrayValues(workingHeightMap, heightMap);

        CreateMesh();
    }

    private List<Vector3> GeneratePath(float[,] heightMap, Vector3 roadStart, Vector3 roadEnd) {
        List<Vector3> path = new List<Vector3>();
        
        int mapSize = worldSettings.meshSettings.meshWorldSize;

        for (int i = 1; i < mapSize; i+=3) {
            float x = roadStart.x + i;
            float z = roadStart.z + i;
            float y = HeightFromFloatCoord(new Vector2(x, z), heightMap);
            Vector3 newPoint = new Vector3(x, y, z);
            path.Add(newPoint);
        }

        return path;
    }

    private void AddPoint(float[,] workingHeightMap, float[,] referenceHeightMap, Vector3 nextPoint) {
        
        int mapSize = referenceHeightMap.GetLength(0);

        Vector2 prevPoint2d = new Vector2(prevPoint.x, prevPoint.z);
        Vector2 nextPoint2d = new Vector2(nextPoint.x, nextPoint.z);

        float heightStart = HeightFromFloatCoord(prevPoint2d, referenceHeightMap);
        float heightEnd =  HeightFromFloatCoord(nextPoint2d, referenceHeightMap);

        float length = Vector2.Distance(prevPoint2d, nextPoint2d);

        Vector3 direction = (nextPoint - prevPoint);
        direction.Normalize();

        Vector2 direction2d = (nextPoint2d - prevPoint2d);
        direction2d.Normalize();
        
        float maxWidthOffset = (roadSettings.width / 2f);
        int minX = (int)Mathf.Min(nextPoint.x - maxWidthOffset, prevPoint.x - maxWidthOffset);
        int maxX = (int)Mathf.Max(nextPoint.x + maxWidthOffset, prevPoint.x + maxWidthOffset);
        int minZ = (int)Mathf.Min(nextPoint.z - maxWidthOffset, prevPoint.z - maxWidthOffset);
        int maxZ = (int)Mathf.Max(nextPoint.z + maxWidthOffset, prevPoint.z + maxWidthOffset);

        Vector3 normal = Vector3.Cross(direction, Vector3.up).normalized;
        Vector2 normal2d = new Vector2(normal.x, normal.z);
        Rectangle boundingRect = new Rectangle(prevPoint2d + normal2d * roadSettings.width, 
                                                prevPoint2d - normal2d * roadSettings.width, 
                                                nextPoint2d - normal2d * roadSettings.width,
                                                nextPoint2d + normal2d * roadSettings.width);

                    

        for (int i = minX; i <= maxX; i++) {
            for (int j = minZ; j <= maxZ; j++) {
                
                if (i >= 0 && i < mapSize && j > 0 && j < mapSize && boundingRect.Contains(new Vector2(i, j))) {

                    // Calculate distance of point from line
                    Ray ray = new Ray(prevPoint, direction);
                    Vector3 point = new Vector3(i, referenceHeightMap[i, j], j);
                    float distance = Vector3.Cross(ray.direction, point - ray.origin).magnitude;

                    if (distance < (roadSettings.width / 2f)) {

                        // Calculate closest point on centre of line
                        Vector2 lhs = new Vector2(point.x, point.z) - prevPoint2d;
                        float distAlongLine = Vector2.Dot(lhs, direction2d);
                        float percent = (distAlongLine / length);
                        float newHeight =  percent * heightEnd + (1f - percent) * heightStart;
                        workingHeightMap[i, j] = newHeight;
                    }
                }                
            }
        }

        AddMeshValues(nextPoint);

    }

    private void AddMeshValues(Vector3 nextPoint) {
        float length = Vector3.Distance(prevPoint, nextPoint);
		
        this.vertices.Add(new Vector3(prevPoint.x, prevPoint.y, prevPoint.z - roadSettings.width / 2));
        this.vertices.Add(new Vector3(nextPoint.x, nextPoint.y, nextPoint.z - roadSettings.width / 2));
        this.vertices.Add(new Vector3(nextPoint.x, nextPoint.y, nextPoint.z + roadSettings.width / 2));
        this.vertices.Add(new Vector3(prevPoint.x, prevPoint.y, prevPoint.z + roadSettings.width / 2));

        int triangleOffset = this.vertices.Count - 4;
		int[] triangles = {
            1 + triangleOffset, triangleOffset, 2 + triangleOffset,	// triangle 1
            2 + triangleOffset, triangleOffset, 3 + triangleOffset	// triangle 2
        };
        this.triangles.AddRange(triangles);
		
        this.uvs.Add(new Vector2(0, 0));
        this.uvs.Add(new Vector2(length, 0));
        this.uvs.Add(new Vector2(length, 1));
        this.uvs.Add(new Vector2(0, 1));

        this.normals.Add(Vector3.up);
        this.normals.Add(Vector3.up);
        this.normals.Add(Vector3.up);
        this.normals.Add(Vector3.up);

        this.prevPoint = nextPoint;
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
}

public class Rectangle {

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
