using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ObjectPositionData
{    
    public ObjectPositions positions;
    public float[][] heightMap;

    public ObjectPositionData(ObjectPositions positions, float[][] heightMap)
    {
        this.positions = positions;
        this.heightMap = heightMap;
    }
}

public struct ObjectPositions
{
    public float[] xCoords;
    public float[] yCoords;
    public float[] zCoords;

    public Quaternion[] rotations;
    public Vector3[] scales;
    public bool[] filtered;

    public ObjectPositions(float[] xCoords, float[] yCoords, float[] zCoords, Vector3[] scales, Quaternion[] rotations)
    {
        this.xCoords = xCoords;
        this.yCoords = yCoords;
        this.zCoords = zCoords;
        this.rotations = rotations;
        this.scales = scales;
        this.filtered = new bool[this.xCoords.Length];
    }

    public ObjectPositions(float[] xCoords, float[] yCoords, float[] zCoords)
    {
        int numCoords = xCoords.Length;

        this.xCoords = xCoords;
        this.yCoords = yCoords;
        this.zCoords = zCoords;
        this.scales = new Vector3[numCoords];
        this.rotations = new Quaternion[numCoords];

        Vector3 defaultScale = new Vector3(1f, 1f, 1f);
        Quaternion defaultRotation = Quaternion.identity;
        
        for (int i = 0; i < numCoords; i++)
        {
            this.scales[i] = defaultScale;
            this.rotations[i] = defaultRotation;
        }
        this.filtered = new bool[numCoords];
    }
    
    public int Length {
        get {
            return this.xCoords.Length;
        } 
    }
}

public class ObjectSpawner
{
    private static readonly Vector3[] defaultNormals = { 
        new Vector3(0f, 0f, -1f),
        new Vector3(-.866f, 0f, -0.5f),
        new Vector3(.866f, 0f, -0.5f),
        new Vector3(0f, 0f, 1f),
        new Vector3(.866f, 0f, 0.5f),
        new Vector3(-.866f, 0f, 0.5f),
    };

    private static readonly Vector2[] defaultUvs = { 
        new Vector2(0f, 1f),
        new Vector2(1f, 1f),
        new Vector2(1f, 0f),
        new Vector2(0f, 0f),
    };

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
    private GameObject[] spawnedObjects;
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
        this.spawnedObjects = new GameObject[this.positions.Length];
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
        this.spawnedObjects = new GameObject[this.positions.Length];
        this.hide = hide;
    }

    public void SetParent(Transform transform)
    {
        int length = terrainObjects.Length;
        for (int i = 0; i < length; i++)
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
        int length = positions.Length;
        for (int i = 0; i < length; i++)
        {
            float rand = Common.NextFloat(prng, 0, terrainObjects.Length);
            int objIndex = (int)rand;
            GameObject obj = UnityEngine.Object.Instantiate(terrainObjects[objIndex].gameObject);
            obj.transform.parent = parent;
            obj.transform.localPosition = new Vector3(positions.xCoords[i], positions.yCoords[i], positions.zCoords[i]);
            obj.transform.rotation = positions.rotations[i];
            obj.transform.localScale = positions.scales[i];
            obj.SetActive(!hide);
            spawnedObjects[i] = obj;
        }
        if (spawnedObjects.Length > 0 && staticBatch)
        {
            StaticBatchingUtility.Combine(spawnedObjects, spawnedObjects[0]);
        }
    }

    private void SpawnDetails()
    {
        if (this.detailMode == DetailMode.GeometryShader)
        {
            this.GeometryShaderDetails();
            return;
        }

        int numDetailMaterials = this.detailMaterials.Length;

        GameObject[] detailObjects = new GameObject[numDetailMaterials];
        for (int i = 0; i < numDetailMaterials; i++)
        {
            detailObjects[i] = new GameObject();
            detailObjects[i].transform.parent = this.parent;
            detailObjects[i].transform.localPosition = new Vector3(0f, 0f, 0f);
        }

        Mesh[] meshes;
        if (this.detailMode == DetailMode.Billboard)
        {
            meshes = this.GenerateBillboardDetailsMesh();
        }
        else if (this.detailMode == DetailMode.Triangle)
        {
            meshes = this.GenerateTriangleDetailsMesh();
        }
        else if (this.detailMode == DetailMode.Circle)
        {
            meshes = this.GenerateCircleDetailsMesh();
        }
        else
        {
            meshes = this.GenerateCircleDetailsMesh();
        }

        for (int i = 0; i < numDetailMaterials; i++)
        {
            MeshFilter groupMeshFilter = detailObjects[i].AddComponent<MeshFilter>();
            MeshRenderer groupMeshRenderer = detailObjects[i].AddComponent<MeshRenderer>();
            groupMeshFilter.sharedMesh = meshes[i];
            groupMeshRenderer.sharedMaterial = this.detailMaterials[i];

            // TODO options for lighting on details
            groupMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            groupMeshRenderer.receiveShadows = true;
            groupMeshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;

            detailObjects[i].SetActive(!hide);
        }
    }

    private void GeometryShaderDetails()
    {

    }

    private Mesh[] GenerateBillboardDetailsMesh()
    {
        int verticesPerPosition = 4;
        int trianglesPerPosition = 6;

        int numDetailTypes = this.detailMaterials.Length;
        int numObjects = this.positions.Length;
        
        Vector3[][] vertices = new Vector3[numDetailTypes][];
        int[][] triangles = new int[numDetailTypes][];
        Vector2[][] uvs = new Vector2[numDetailTypes][];
        Vector3[][] normals = new Vector3[numDetailTypes][];

        for (int i = 0; i < numDetailTypes; i++) {
            vertices[i] = new Vector3[numObjects * verticesPerPosition];
            triangles[i] = new int[numObjects * trianglesPerPosition];
            uvs[i] = new Vector2[numObjects * verticesPerPosition];
            normals[i] = new Vector3[numObjects * verticesPerPosition];
        }
        
        for (int detail = 0; detail < numDetailTypes; detail++) 
        {
            for (int idx = 0; idx < numObjects; idx++)
            {
                float x = this.positions.xCoords[idx];
                float y = this.positions.yCoords[idx];
                float z = this.positions.zCoords[idx];

                float scaleX = this.positions.scales[idx].x;
                float scaleY = this.positions.scales[idx].y;

                Vector3 a = new Vector3(x - 0.5f * scaleX, y + 0.5f * scaleY, z); // Top left
                Vector3 b = new Vector3(x + 0.5f * scaleX, y + 0.5f * scaleY, z); // Top right
                Vector3 c = new Vector3(x + 0.5f * scaleX, y - 0.5f * scaleY, z); // Bottom right
                Vector3 d = new Vector3(x - 0.5f * scaleX, y - 0.5f * scaleY, z); // Bottom left

                int verticesOffset = idx * verticesPerPosition;
                vertices[detail][verticesOffset + 0] = a;
                vertices[detail][verticesOffset + 1] = b;
                vertices[detail][verticesOffset + 2] = c;
                vertices[detail][verticesOffset + 3] = d;

                int trianglesOffset = idx * trianglesPerPosition;
                triangles[detail][trianglesOffset + 0] = 0 + verticesOffset;
                triangles[detail][trianglesOffset + 1] = 2 + verticesOffset;
                triangles[detail][trianglesOffset + 2] = 3 + verticesOffset;
                triangles[detail][trianglesOffset + 3] = 0 + verticesOffset;
                triangles[detail][trianglesOffset + 4] = 1 + verticesOffset;
                triangles[detail][trianglesOffset + 5] = 2 + verticesOffset;

                int uvsOffset = verticesOffset;
                uvs[detail][uvsOffset + 0] = defaultUvs[0];
                uvs[detail][uvsOffset + 1] = defaultUvs[1];
                uvs[detail][uvsOffset + 2] = defaultUvs[2];
                uvs[detail][uvsOffset + 3] = defaultUvs[3];

                // Same normal for every point for a billboard
                int normalsOffset = verticesOffset;
                normals[detail][normalsOffset + 0] = defaultNormals[0];
                normals[detail][normalsOffset + 1] = defaultNormals[0];
                normals[detail][normalsOffset + 2] = defaultNormals[0];
                normals[detail][normalsOffset + 3] = defaultNormals[0];
            }
        }

        Mesh[] meshes = new Mesh[numDetailTypes];
        for (int i = 0; i < numDetailTypes; i++)
        {
            meshes[i] = new Mesh();
            meshes[i].indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshes[i].vertices = vertices[i];
            meshes[i].triangles = triangles[i];
            meshes[i].normals = normals[i];
            meshes[i].uv = uvs[i];
        }

        return meshes;
    }

    // TODO
    private Mesh[] GenerateTriangleDetailsMesh()
    {
        Mesh[] meshes = new Mesh[1];
        meshes[0] = new Mesh();
        return meshes;
    }

    private Mesh[] GenerateCircleDetailsMesh()
    {
        int verticesPerPosition = 24;
        int trianglesPerPosition = 36;
        
        int numDetailTypes = this.detailMaterials.Length;
        int numObjects = this.positions.Length;
        int numObjectsPerDetail = (numObjects / numDetailTypes) + 1;

        Vector3[][] vertices = new Vector3[numDetailTypes][];
        int[][] triangles = new int[numDetailTypes][];
        Vector2[][] uvs = new Vector2[numDetailTypes][];
        Vector3[][] normals = new Vector3[numDetailTypes][];

        for (int i = 0; i < numDetailTypes; i++) {
            vertices[i] = new Vector3[numObjectsPerDetail * verticesPerPosition];
            triangles[i] = new int[numObjectsPerDetail * trianglesPerPosition];
            uvs[i] = new Vector2[numObjectsPerDetail * verticesPerPosition];
            normals[i] = new Vector3[numObjectsPerDetail * verticesPerPosition];
        }

        // Set positions of vertice for each mesh
        for (int detail = 0; detail < numDetailTypes; detail++) 
        {
            for (int idx = 0; idx < numObjects; idx++)
            {
                float x = this.positions.xCoords[idx];
                float y = this.positions.yCoords[idx];
                float z = this.positions.zCoords[idx];

                float scaleX = this.positions.scales[idx].x;
                float scaleY = this.positions.scales[idx].y;
                float scaleZ = this.positions.scales[idx].z;

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
                
                int verticesOffset = (idx * verticesPerPosition) / numDetailTypes;
                
                // Horizontal quad
                vertices[detail][verticesOffset + 0] = a;
                vertices[detail][verticesOffset + 1] = b;
                vertices[detail][verticesOffset + 2] = c;
                vertices[detail][verticesOffset + 3] = d;

                // Rotated 60 degrees clockwise
                vertices[detail][verticesOffset + 4] = e;
                vertices[detail][verticesOffset + 5] = f;
                vertices[detail][verticesOffset + 6] = g;
                vertices[detail][verticesOffset + 7] = h;

                // Rotated 120 degrees clockwise
                vertices[detail][verticesOffset + 8] = i;
                vertices[detail][verticesOffset + 9] = j;
                vertices[detail][verticesOffset + 10] = k;
                vertices[detail][verticesOffset + 11] = l;

                // Horizontal quad reverse
                vertices[detail][verticesOffset + 12] = a;
                vertices[detail][verticesOffset + 13] = b;
                vertices[detail][verticesOffset + 14] = c;
                vertices[detail][verticesOffset + 15] = d;

                // Rotated 60 degrees clockwise reverse
                vertices[detail][verticesOffset + 16] = e;
                vertices[detail][verticesOffset + 17] = f;
                vertices[detail][verticesOffset + 18] = g;
                vertices[detail][verticesOffset + 19] = h;

                // Rotated 120 degrees clockwise reverse
                vertices[detail][verticesOffset + 20] = i;
                vertices[detail][verticesOffset + 21] = j;
                vertices[detail][verticesOffset + 22] = k;
                vertices[detail][verticesOffset + 23] = l;
            }
        }

        // Set uvs for each mesh
        for (int i = 0; i < numDetailTypes; i++) 
        { 
            for (int j = 0; j < numObjectsPerDetail; j++)
            {
                int uvsOffset = j * verticesPerPosition;
                uvs[i][uvsOffset + 0] = defaultUvs[0];
                uvs[i][uvsOffset + 1] = defaultUvs[1];
                uvs[i][uvsOffset + 2] = defaultUvs[2];
                uvs[i][uvsOffset + 3] = defaultUvs[3];

                uvs[i][uvsOffset + 4] = defaultUvs[0];
                uvs[i][uvsOffset + 5] = defaultUvs[1];
                uvs[i][uvsOffset + 6] = defaultUvs[2];
                uvs[i][uvsOffset + 7] = defaultUvs[3];

                uvs[i][uvsOffset + 8] = defaultUvs[0];
                uvs[i][uvsOffset + 9] = defaultUvs[1];
                uvs[i][uvsOffset + 10] = defaultUvs[2];
                uvs[i][uvsOffset + 11] = defaultUvs[3];

                uvs[i][uvsOffset + 12] = defaultUvs[0];
                uvs[i][uvsOffset + 13] = defaultUvs[1];
                uvs[i][uvsOffset + 14] = defaultUvs[2];
                uvs[i][uvsOffset + 15] = defaultUvs[3];

                uvs[i][uvsOffset + 16] = defaultUvs[0];
                uvs[i][uvsOffset + 17] = defaultUvs[1];
                uvs[i][uvsOffset + 18] = defaultUvs[2];
                uvs[i][uvsOffset + 19] = defaultUvs[3];

                uvs[i][uvsOffset + 20] = defaultUvs[0];
                uvs[i][uvsOffset + 21] = defaultUvs[1];
                uvs[i][uvsOffset + 22] = defaultUvs[2];
                uvs[i][uvsOffset + 23] = defaultUvs[3];
            }
        }

        // Set normals for each mesh
        for (int i = 0; i < numDetailTypes; i++) 
        { 
            for (int j = 0; j < numObjectsPerDetail; j++)
            {
                int normalsOffset = j * verticesPerPosition;
                normals[i][normalsOffset + 0] = defaultNormals[0];
                normals[i][normalsOffset + 1] = defaultNormals[0];
                normals[i][normalsOffset + 2] = defaultNormals[0];
                normals[i][normalsOffset + 3] = defaultNormals[0];

                normals[i][normalsOffset + 4] = defaultNormals[1];
                normals[i][normalsOffset + 5] = defaultNormals[1];
                normals[i][normalsOffset + 6] = defaultNormals[1];
                normals[i][normalsOffset + 7] = defaultNormals[1];

                normals[i][normalsOffset + 8] = defaultNormals[2];
                normals[i][normalsOffset + 9] = defaultNormals[2];
                normals[i][normalsOffset + 10] = defaultNormals[2];
                normals[i][normalsOffset + 11] = defaultNormals[2];

                normals[i][normalsOffset + 12] = defaultNormals[3];
                normals[i][normalsOffset + 13] = defaultNormals[3];
                normals[i][normalsOffset + 14] = defaultNormals[3];
                normals[i][normalsOffset + 15] = defaultNormals[3];

                normals[i][normalsOffset + 16] = defaultNormals[4];
                normals[i][normalsOffset + 17] = defaultNormals[4];
                normals[i][normalsOffset + 18] = defaultNormals[4];
                normals[i][normalsOffset + 19] = defaultNormals[4];

                normals[i][normalsOffset + 20] = defaultNormals[5];
                normals[i][normalsOffset + 21] = defaultNormals[5];
                normals[i][normalsOffset + 22] = defaultNormals[5];
                normals[i][normalsOffset + 23] = defaultNormals[5];
            }
        }

        for (int i = 0; i < numDetailTypes; i++) 
        {
            for (int j = 0; j < numObjectsPerDetail; j++)
            {
                int verticesOffset = j * verticesPerPosition;
                int trianglesOffset = j * trianglesPerPosition;

                // Horizontal quad
                triangles[i][trianglesOffset + 0] = 0 + verticesOffset;
                triangles[i][trianglesOffset + 1] = 2 + verticesOffset;
                triangles[i][trianglesOffset + 2] = 3 + verticesOffset;
                triangles[i][trianglesOffset + 3] = 0 + verticesOffset;
                triangles[i][trianglesOffset + 4] = 1 + verticesOffset;
                triangles[i][trianglesOffset + 5] = 2 + verticesOffset;

                // Rotated 60 degrees clockwise
                triangles[i][trianglesOffset + 6] = 4 + verticesOffset;
                triangles[i][trianglesOffset + 7] = 6 + verticesOffset;
                triangles[i][trianglesOffset + 8] = 7 + verticesOffset;
                triangles[i][trianglesOffset + 9] = 4 + verticesOffset;
                triangles[i][trianglesOffset + 10] = 5 + verticesOffset;
                triangles[i][trianglesOffset + 11] = 6 + verticesOffset;

                // Rotated 120 degrees clockwise
                triangles[i][trianglesOffset + 12] = 8 + verticesOffset;
                triangles[i][trianglesOffset + 13] = 10 + verticesOffset;
                triangles[i][trianglesOffset + 14] = 11 + verticesOffset;
                triangles[i][trianglesOffset + 15] = 8 + verticesOffset;
                triangles[i][trianglesOffset + 16] = 9 + verticesOffset;
                triangles[i][trianglesOffset + 17] = 10 + verticesOffset;

                // Horizontal quad reverse
                triangles[i][trianglesOffset + 18] = 12 + verticesOffset;
                triangles[i][trianglesOffset + 19] = 15 + verticesOffset;
                triangles[i][trianglesOffset + 20] = 14 + verticesOffset;
                triangles[i][trianglesOffset + 21] = 12 + verticesOffset;
                triangles[i][trianglesOffset + 22] = 14 + verticesOffset;
                triangles[i][trianglesOffset + 23] = 13 + verticesOffset;

                // Rotated 60 degrees clockwise reverse
                triangles[i][trianglesOffset + 24] = 16 + verticesOffset;
                triangles[i][trianglesOffset + 25] = 19 + verticesOffset;
                triangles[i][trianglesOffset + 26] = 18 + verticesOffset;
                triangles[i][trianglesOffset + 27] = 16 + verticesOffset;
                triangles[i][trianglesOffset + 28] = 18 + verticesOffset;
                triangles[i][trianglesOffset + 29] = 17 + verticesOffset;

                // Rotated 120 degrees clockwise reverse
                triangles[i][trianglesOffset + 30] = 20 + verticesOffset;
                triangles[i][trianglesOffset + 31] = 23 + verticesOffset;
                triangles[i][trianglesOffset + 32] = 22 + verticesOffset;
                triangles[i][trianglesOffset + 33] = 20 + verticesOffset;
                triangles[i][trianglesOffset + 34] = 22 + verticesOffset;
                triangles[i][trianglesOffset + 35] = 21 + verticesOffset;
            }
        }

        Mesh[] meshes = new Mesh[numDetailTypes];
        for (int i = 0; i < numDetailTypes; i++)
        {
            meshes[i] = new Mesh();
            meshes[i].indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshes[i].vertices = vertices[i];
            meshes[i].triangles = triangles[i];
            meshes[i].normals = normals[i];
            meshes[i].uv = uvs[i];
        }

        return meshes;
    }
}

