using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPositionData
{    
    public ObjectPositions positions;
    public float[,] heightMap;

    public ObjectPositionData(ObjectPositions positions, float[,] heightMap)
    {
        this.positions = positions;
        this.heightMap = heightMap;
    }
}

public class ObjectPositions
{
    public List<float> xCoords;
    public List<float> yCoords;
    public List<float> zCoords;

    public List<Quaternion> rotations;
    public List<Vector3> scales;
    public bool[] filtered;

    public ObjectPositions(List<float> xCoords, List<float> yCoords, List<float> zCoords, List<Vector3> scales, List<Quaternion> rotations)
    {
        this.xCoords = xCoords;
        this.yCoords = yCoords;
        this.zCoords = zCoords;
        this.rotations = rotations;
        this.scales = scales;
        this.filtered = new bool[this.xCoords.Count];
    }

    public ObjectPositions(List<float> xCoords, List<float> yCoords, List<float> zCoords)
    {
        this.xCoords = xCoords;
        this.yCoords = yCoords;
        this.zCoords = zCoords;
        this.scales = new List<Vector3>(this.xCoords.Count);
        this.rotations = new List<Quaternion>(this.xCoords.Count);
        for (int i = 0; i < this.xCoords.Count; i++)
        {
            this.scales.Add(new Vector3(1f, 1f, 1f));
            this.rotations.Add(Quaternion.identity);
        }
        this.filtered = new bool[this.xCoords.Count];
    }
    
    public int Count {
        get {
            return this.xCoords.Count;
        } 
    }
}

public class ObjectSpawner
{
    private bool isDetail;

    // Detail only settings
    private Material[] detailMaterials;
    public enum DetailMode { Billboard, Circle, Triangle, GeometryShader };
    private DetailMode detailMode;

    // Mesh object only settings
    public GameObject[] terrainObjects;

    // Common settings
    public ObjectPositions positions;
    private System.Random prng;
    private bool hide;
    private bool staticBatch;

    // Internal vars
    private List<GameObject> spawnedObjects;
    private Transform parent;

    public ObjectSpawner(
        GameObject[] terrainObjects,
        ObjectPositions positions,
        System.Random prng,
        bool staticBatch,
        bool hide
    )
    {
        this.isDetail = false;
        if (terrainObjects == null)
        {
            this.terrainObjects = new GameObject[0];
        }
        else
        {
            this.terrainObjects = terrainObjects;
        }
        this.positions = positions;
        this.prng = prng;
        this.spawnedObjects = new List<GameObject>();
        this.staticBatch = staticBatch;
        this.hide = hide;
    }

    public ObjectSpawner(
        Material[] detailMaterials,
        DetailMode detailMode,
        ObjectPositions positions,
        System.Random prng,
        bool hide
    )
    {
        this.isDetail = true;
        this.detailMaterials = detailMaterials;
        this.detailMode = detailMode;
        this.positions = positions;
        this.prng = prng;
        this.spawnedObjects = new List<GameObject>();
        this.hide = hide;
    }

    public void SetParent(Transform transform)
    {
        for (int i = 0; i < terrainObjects.Length; i++)
        {
            terrainObjects[i].gameObject.transform.parent = transform;
        }
    }

    public void Spawn(Transform parent)
    {
        this.parent = parent;
        if (this.isDetail)
        {
            SpawnDetails();
        }
        else
        {
            SpawnMeshObjects();
        }
    }

    private void SpawnMeshObjects()
    {
        for (int i = 0; i < positions.Count; i++)
        {
            float rand = Common.NextFloat(prng, 0, terrainObjects.Length);
            int objIndex = (int)rand;
            GameObject obj = UnityEngine.Object.Instantiate(terrainObjects[objIndex].gameObject);
            obj.transform.parent = parent;
            obj.transform.localPosition = new Vector3(positions.xCoords[i], positions.yCoords[i], positions.zCoords[i]);
            obj.transform.rotation = positions.rotations[i];
            obj.transform.localScale = positions.scales[i];
            obj.SetActive(!hide);
            spawnedObjects.Add(obj);
        }
        if (spawnedObjects.Count > 0 && staticBatch)
        {
            StaticBatchingUtility.Combine(spawnedObjects.ToArray(), spawnedObjects[0]);
        }
    }

    private void SpawnDetails()
    {
        if (this.detailMode == DetailMode.GeometryShader)
        {
            this.GeometryShaderDetails();
            return;
        }

        int detailsInterval = this.positions.Count / this.detailMaterials.Length;

        // Shuffle so we get even distribution of different detail materials as the order of
        // positions are grouped (e.g. first x points in 0,0 square next x in 0,1 ...)
        this.positions.xCoords.Shuffle(this.positions.yCoords, this.positions.zCoords);

        for (int i = 0; i < this.detailMaterials.Length; i++)
        {
            Camera camera = Camera.main;

            GameObject groupObject = new GameObject();
            groupObject.transform.parent = this.parent;
            groupObject.transform.localPosition = new Vector3(0f, 0f, 0f);
            MeshFilter groupMeshFilter = groupObject.AddComponent<MeshFilter>();
            MeshRenderer groupMeshRenderer = groupObject.AddComponent<MeshRenderer>();

            Mesh mesh;
            if (this.detailMode == DetailMode.Billboard)
            {
                mesh = this.GenerateBillboardDetailsMesh(i * detailsInterval, (i + 1) * detailsInterval);
            }
            else if (this.detailMode == DetailMode.Triangle)
            {
                mesh = this.GenerateTriangleDetailsMesh();
            }
            else if (this.detailMode == DetailMode.Circle)
            {
                mesh = this.GenerateCircleDetailsMesh(i * detailsInterval, (i + 1) * detailsInterval);
            }
            else
            {
                mesh = this.GenerateCircleDetailsMesh(i * detailsInterval, (i + 1) * detailsInterval);
            }

            groupMeshFilter.sharedMesh = mesh;
            groupMeshRenderer.sharedMaterial = this.detailMaterials[i];

            // TODO options for lighting on details
            groupMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            groupMeshRenderer.receiveShadows = true;
            groupMeshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;

            groupObject.SetActive(!hide);
        }
    }

    private void GeometryShaderDetails()
    {

    }

    private Mesh GenerateBillboardDetailsMesh(int start, int end)
    {
        int verticesPerPosition = 4;
        int trianglesPerPosition = 6;

        int numObjects = end - start;
        Vector3[] vertices = new Vector3[numObjects * verticesPerPosition];
        int[] triangles = new int[numObjects * trianglesPerPosition];
        Vector2[] uvs = new Vector2[numObjects * verticesPerPosition];
        Vector3[] normals = new Vector3[numObjects * verticesPerPosition];

        for (int i = 0; i < numObjects; i++)
        {
            float x = this.positions.xCoords[i];
            float y = this.positions.yCoords[i];
            float z = this.positions.zCoords[i];

            float scaleX = this.positions.scales[i].x;
            float scaleY = this.positions.scales[i].y;

            Vector3 a = new Vector3(x - 0.5f * scaleX, y + 0.5f * scaleY, z); // Top left
            Vector3 b = new Vector3(x + 0.5f * scaleX, y + 0.5f * scaleY, z); // Top right
            Vector3 c = new Vector3(x + 0.5f * scaleX, y - 0.5f * scaleY, z); // Bottom right
            Vector3 d = new Vector3(x - 0.5f * scaleX, y - 0.5f * scaleY, z); // Bottom left

            int verticesOffset = i * verticesPerPosition;
            vertices[verticesOffset + 0] = a;
            vertices[verticesOffset + 1] = b;
            vertices[verticesOffset + 2] = c;
            vertices[verticesOffset + 3] = d;

            int trianglesOffset = i * trianglesPerPosition;
            triangles[trianglesOffset + 0] = 0 + verticesOffset;
            triangles[trianglesOffset + 1] = 2 + verticesOffset;
            triangles[trianglesOffset + 2] = 3 + verticesOffset;
            triangles[trianglesOffset + 3] = 0 + verticesOffset;
            triangles[trianglesOffset + 4] = 1 + verticesOffset;
            triangles[trianglesOffset + 5] = 2 + verticesOffset;

            int uvsOffset = verticesOffset;
            uvs[uvsOffset + 0] = new Vector2(0f, 1f);
            uvs[uvsOffset + 1] = new Vector2(1f, 1f);
            uvs[uvsOffset + 2] = new Vector2(1f, 0f);
            uvs[uvsOffset + 3] = new Vector2(0f, 0f);

            int normalsOffset = verticesOffset;
            normals[normalsOffset + 0] = new Vector3(0f, 0f, -1f);
            normals[normalsOffset + 1] = new Vector3(0f, 0f, -1f);
            normals[normalsOffset + 2] = new Vector3(0f, 0f, -1f);
            normals[normalsOffset + 3] = new Vector3(0f, 0f, -1f);
        }

        Mesh finalMesh = new Mesh();
        finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        finalMesh.vertices = vertices;
        finalMesh.triangles = triangles;
        finalMesh.normals = normals;
        finalMesh.uv = uvs;

        return finalMesh;
    }

    private Mesh GenerateTriangleDetailsMesh()
    {
        return new Mesh();
    }

    private Mesh GenerateCircleDetailsMesh(int start, int end)
    {
        int verticesPerPosition = 24;
        int trianglesPerPosition = 36;

        int numObjects = end - start;
        Vector3[] vertices = new Vector3[numObjects * verticesPerPosition];
        int[] triangles = new int[numObjects * trianglesPerPosition];
        Vector2[] uvs = new Vector2[numObjects * verticesPerPosition];
        Vector3[] normals = new Vector3[numObjects * verticesPerPosition];
        
        for (int pos = start; pos < end; pos++)
        {
            float x = this.positions.xCoords[pos];
            float y = this.positions.yCoords[pos];
            float z = this.positions.zCoords[pos];

            float scaleX = this.positions.scales[pos].x;
            float scaleY = this.positions.scales[pos].y;
            float scaleZ = this.positions.scales[pos].z;

            // Horizontal quad
            Vector3 a = new Vector3(x - 0.5f * scaleX, y + 0.5f * scaleY, z); // Top left
            Vector3 b = new Vector3(x + 0.5f * scaleX, y + 0.5f * scaleY, z); // Top right
            Vector3 c = new Vector3(x + 0.5f * scaleX, y - 0.5f * scaleY, z); // Bottom right
            Vector3 d = new Vector3(x - 0.5f * scaleX, y - 0.5f * scaleY, z); // Bottom left

            // Rotated 60 degrees clockwise
            Vector3 e = new Vector3(x - 0.25f * scaleX, y + 0.5f * scaleY, z + 0.433f * scaleZ); // Top left
            Vector3 f = new Vector3(x + 0.25f * scaleX, y + 0.5f * scaleY, z - 0.433f * scaleZ); // Top right
            Vector3 g = new Vector3(x + 0.25f * scaleX, y - 0.5f * scaleY, z - 0.433f * scaleZ); // Bottom right
            Vector3 h = new Vector3(x - 0.25f * scaleX, y - 0.5f * scaleY, z + 0.433f * scaleZ); // Bottom left

            // Rotated 120 degrees clockwise
            Vector3 i = new Vector3(x - 0.25f * scaleX, y + 0.5f * scaleY, z - 0.433f * scaleZ); // Top left
            Vector3 j = new Vector3(x + 0.25f * scaleX, y + 0.5f * scaleY, z + 0.433f * scaleZ); // Top right
            Vector3 k = new Vector3(x + 0.25f * scaleX, y - 0.5f * scaleY, z + 0.433f * scaleZ); // Bottom right
            Vector3 l = new Vector3(x - 0.25f * scaleX, y - 0.5f * scaleY, z - 0.433f * scaleZ); // Bottom left

            int verticesOffset = (pos - start) * verticesPerPosition;

            // Horizontal quad
            vertices[verticesOffset + 0] = a;
            vertices[verticesOffset + 1] = b;
            vertices[verticesOffset + 2] = c;
            vertices[verticesOffset + 3] = d;

            // Rotated 60 degrees clockwise
            vertices[verticesOffset + 4] = e;
            vertices[verticesOffset + 5] = f;
            vertices[verticesOffset + 6] = g;
            vertices[verticesOffset + 7] = h;

            // Rotated 120 degrees clockwise
            vertices[verticesOffset + 8] = i;
            vertices[verticesOffset + 9] = j;
            vertices[verticesOffset + 10] = k;
            vertices[verticesOffset + 11] = l;

            // Horizontal quad reverse
            vertices[verticesOffset + 12] = a;
            vertices[verticesOffset + 13] = b;
            vertices[verticesOffset + 14] = c;
            vertices[verticesOffset + 15] = d;

            // Rotated 60 degrees clockwise reverse
            vertices[verticesOffset + 16] = e;
            vertices[verticesOffset + 17] = f;
            vertices[verticesOffset + 18] = g;
            vertices[verticesOffset + 19] = h;

            // Rotated 120 degrees clockwise reverse
            vertices[verticesOffset + 20] = i;
            vertices[verticesOffset + 21] = j;
            vertices[verticesOffset + 22] = k;
            vertices[verticesOffset + 23] = l;

            int trianglesOffset = (pos - start) * trianglesPerPosition;

            // Horizontal quad
            triangles[trianglesOffset + 0] = 0 + verticesOffset;
            triangles[trianglesOffset + 1] = 2 + verticesOffset;
            triangles[trianglesOffset + 2] = 3 + verticesOffset;
            triangles[trianglesOffset + 3] = 0 + verticesOffset;
            triangles[trianglesOffset + 4] = 1 + verticesOffset;
            triangles[trianglesOffset + 5] = 2 + verticesOffset;

            // Rotated 60 degrees clockwise
            triangles[trianglesOffset + 6] = 4 + verticesOffset;
            triangles[trianglesOffset + 7] = 6 + verticesOffset;
            triangles[trianglesOffset + 8] = 7 + verticesOffset;
            triangles[trianglesOffset + 9] = 4 + verticesOffset;
            triangles[trianglesOffset + 10] = 5 + verticesOffset;
            triangles[trianglesOffset + 11] = 6 + verticesOffset;

            // Rotated 120 degrees clockwise
            triangles[trianglesOffset + 12] = 8 + verticesOffset;
            triangles[trianglesOffset + 13] = 10 + verticesOffset;
            triangles[trianglesOffset + 14] = 11 + verticesOffset;
            triangles[trianglesOffset + 15] = 8 + verticesOffset;
            triangles[trianglesOffset + 16] = 9 + verticesOffset;
            triangles[trianglesOffset + 17] = 10 + verticesOffset;

            // Horizontal quad reverse
            triangles[trianglesOffset + 18] = 12 + verticesOffset;
            triangles[trianglesOffset + 19] = 15 + verticesOffset;
            triangles[trianglesOffset + 20] = 14 + verticesOffset;
            triangles[trianglesOffset + 21] = 12 + verticesOffset;
            triangles[trianglesOffset + 22] = 14 + verticesOffset;
            triangles[trianglesOffset + 23] = 13 + verticesOffset;

            // Rotated 60 degrees clockwise reverse
            triangles[trianglesOffset + 24] = 16 + verticesOffset;
            triangles[trianglesOffset + 25] = 19 + verticesOffset;
            triangles[trianglesOffset + 26] = 18 + verticesOffset;
            triangles[trianglesOffset + 27] = 16 + verticesOffset;
            triangles[trianglesOffset + 28] = 18 + verticesOffset;
            triangles[trianglesOffset + 29] = 17 + verticesOffset;

            // Rotated 120 degrees clockwise reverse
            triangles[trianglesOffset + 30] = 20 + verticesOffset;
            triangles[trianglesOffset + 31] = 23 + verticesOffset;
            triangles[trianglesOffset + 32] = 22 + verticesOffset;
            triangles[trianglesOffset + 33] = 20 + verticesOffset;
            triangles[trianglesOffset + 34] = 22 + verticesOffset;
            triangles[trianglesOffset + 35] = 21 + verticesOffset;

            int uvsOffset = verticesOffset;
            uvs[uvsOffset + 0] = new Vector2(0f, 1f);
            uvs[uvsOffset + 1] = new Vector2(1f, 1f);
            uvs[uvsOffset + 2] = new Vector2(1f, 0f);
            uvs[uvsOffset + 3] = new Vector2(0f, 0f);

            uvs[uvsOffset + 4] = new Vector2(0f, 1f);
            uvs[uvsOffset + 5] = new Vector2(1f, 1f);
            uvs[uvsOffset + 6] = new Vector2(1f, 0f);
            uvs[uvsOffset + 7] = new Vector2(0f, 0f);

            uvs[uvsOffset + 8] = new Vector2(0f, 1f);
            uvs[uvsOffset + 9] = new Vector2(1f, 1f);
            uvs[uvsOffset + 10] = new Vector2(1f, 0f);
            uvs[uvsOffset + 11] = new Vector2(0f, 0f);

            uvs[uvsOffset + 12] = new Vector2(0f, 1f);
            uvs[uvsOffset + 13] = new Vector2(1f, 1f);
            uvs[uvsOffset + 14] = new Vector2(1f, 0f);
            uvs[uvsOffset + 15] = new Vector2(0f, 0f);

            uvs[uvsOffset + 16] = new Vector2(0f, 1f);
            uvs[uvsOffset + 17] = new Vector2(1f, 1f);
            uvs[uvsOffset + 18] = new Vector2(1f, 0f);
            uvs[uvsOffset + 19] = new Vector2(0f, 0f);

            uvs[uvsOffset + 20] = new Vector2(0f, 1f);
            uvs[uvsOffset + 21] = new Vector2(1f, 1f);
            uvs[uvsOffset + 22] = new Vector2(1f, 0f);
            uvs[uvsOffset + 23] = new Vector2(0f, 0f);

            int normalsOffset = verticesOffset;
            normals[normalsOffset + 0] = new Vector3(0f, 0f, -1f);
            normals[normalsOffset + 1] = new Vector3(0f, 0f, -1f);
            normals[normalsOffset + 2] = new Vector3(0f, 0f, -1f);
            normals[normalsOffset + 3] = new Vector3(0f, 0f, -1f);

            normals[normalsOffset + 4] = new Vector3(-.866f, 0f, -0.5f);
            normals[normalsOffset + 5] = new Vector3(-.866f, 0f, -0.5f);
            normals[normalsOffset + 6] = new Vector3(-.866f, 0f, -0.5f);
            normals[normalsOffset + 7] = new Vector3(-.866f, 0f, -0.5f);

            normals[normalsOffset + 8] = new Vector3(.866f, 0f, -0.5f);
            normals[normalsOffset + 9] = new Vector3(.866f, 0f, -0.5f);
            normals[normalsOffset + 10] = new Vector3(.866f, 0f, -0.5f);
            normals[normalsOffset + 11] = new Vector3(.866f, 0f, -0.5f);

            normals[normalsOffset + 12] = new Vector3(0f, 0f, 1f);
            normals[normalsOffset + 13] = new Vector3(0f, 0f, 1f);
            normals[normalsOffset + 14] = new Vector3(0f, 0f, 1f);
            normals[normalsOffset + 15] = new Vector3(0f, 0f, 1f);

            normals[normalsOffset + 16] = new Vector3(.866f, 0f, 0.5f);
            normals[normalsOffset + 17] = new Vector3(.866f, 0f, 0.5f);
            normals[normalsOffset + 18] = new Vector3(.866f, 0f, 0.5f);
            normals[normalsOffset + 19] = new Vector3(.866f, 0f, 0.5f);

            normals[normalsOffset + 20] = new Vector3(-.866f, 0f, 0.5f);
            normals[normalsOffset + 21] = new Vector3(-.866f, 0f, 0.5f);
            normals[normalsOffset + 22] = new Vector3(-.866f, 0f, 0.5f);
            normals[normalsOffset + 23] = new Vector3(-.866f, 0f, 0.5f);
        }

        Mesh finalMesh = new Mesh();
        finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        finalMesh.vertices = vertices;
        finalMesh.triangles = triangles;
        finalMesh.normals = normals;
        finalMesh.uv = uvs;

        return finalMesh;
    }
}

