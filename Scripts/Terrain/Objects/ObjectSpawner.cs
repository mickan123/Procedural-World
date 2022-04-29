using System.Collections;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;

public struct ObjectPositionData
{    
    public ObjectPositions positions;
    public float[] heightMap;
    public int width;

    public ObjectPositionData(ObjectPositions positions, float[] heightMap, int width)
    {
        this.positions = positions;
        this.heightMap = heightMap;
        this.width = width;
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

    public bool isDetail;

    // Detail only settings
    private Material[] detailMaterials;
    public enum DetailMode { Billboard, Circle, Triangle, GeometryShader };
    public DetailMode detailMode;

    // Mesh object only settings
    public GameObject[] terrainObjects;

    // Common settings
    public ObjectPositions positions;
    private System.Random prng;
    private bool hide;
    private bool staticBatch;

    // Internal vars
    private GameObject[] spawnedObjects;

    public Transform parent;

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

    public void Spawn(Transform parent)
    {
        this.parent = parent;
        if (this.isDetail)
        {
            SpawnDetails();
        }
        else
        {
            IEnumerator iterator = this.SpawnMeshObjects();
            while (iterator.MoveNext()) {}
        }
    }

    public IEnumerator SpawnMeshObjects()
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
            yield return null;
        }
        if (spawnedObjects.Length > 0 && staticBatch)
        {
            StaticBatchingUtility.Combine(spawnedObjects, spawnedObjects[0]);
        }
    }

    public void SpawnDetails()
    {
        if (this.detailMode == DetailMode.GeometryShader)
        {
            this.GeometryShaderDetails();
            return;
        }
        
        Mesh[] meshes;
        if (this.detailMode == DetailMode.Billboard)
        {
            meshes = this.GenerateBillboardDetailsMesh();
            CreateGameObjects(meshes);
        }
        else if (this.detailMode == DetailMode.Triangle)
        {
            meshes = this.GenerateTriangleDetailsMesh();
            CreateGameObjects(meshes);
        }
        else if (this.detailMode == DetailMode.Circle)
        {
            IEnumerator iterator = this.SpawnCircleDetailsMesh();
            while (iterator.MoveNext()) {}
        }
    }

    // TODO Geometry shader details
    public void GeometryShaderDetails()
    {
    
    }

    // TODO make burst job
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

    [BurstCompile]
    struct GenerateCircleDetailsMeshDataJob : IJob
    {
        public NativeArray<Vector3> vertices;
        public NativeArray<Vector2> uvs;
        public NativeArray<int> triangles;
        public NativeArray<Vector3> normals;

        public NativeArray<float> xCoords;
        public NativeArray<float> yCoords;
        public NativeArray<float> zCoords;

        public NativeArray<Vector3> scales;

        public int numDetailTypes;
        public int numObjects;

        public void Execute()
        {
            int verticesPerPosition = 24;
            int trianglesPerPosition = 36;
            int numObjectsPerDetail = (numObjects / numDetailTypes);

            // Set positions of vertice for each mesh
            for (int objectIdx = 0; objectIdx < numObjectsPerDetail; objectIdx++)
            {
                for (int detailIdx = 0; detailIdx < numDetailTypes; detailIdx++) 
                {
                    int idx = objectIdx * numDetailTypes + detailIdx;
                    
                    float x = xCoords[idx];
                    float y = yCoords[idx];
                    float z = zCoords[idx];

                    float scaleX = scales[idx].x;
                    float scaleY = scales[idx].y;
                    float scaleZ = scales[idx].z;

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
                    
                    int verticesOffset = (detailIdx * numObjectsPerDetail + objectIdx) * verticesPerPosition;
                    
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
                }
            }

            // Set uvs for each mesh
            for (int i = 0; i < numDetailTypes; i++) 
            { 
                for (int j = 0; j < numObjectsPerDetail; j++)
                {
                    int uvsOffset = (i * numObjectsPerDetail + j) * verticesPerPosition;
                    uvs[uvsOffset + 0] = defaultUvs[0];
                    uvs[uvsOffset + 1] = defaultUvs[1];
                    uvs[uvsOffset + 2] = defaultUvs[2];
                    uvs[uvsOffset + 3] = defaultUvs[3];

                    uvs[uvsOffset + 4] = defaultUvs[0];
                    uvs[uvsOffset + 5] = defaultUvs[1];
                    uvs[uvsOffset + 6] = defaultUvs[2];
                    uvs[uvsOffset + 7] = defaultUvs[3];

                    uvs[uvsOffset + 8] = defaultUvs[0];
                    uvs[uvsOffset + 9] = defaultUvs[1];
                    uvs[uvsOffset + 10] = defaultUvs[2];
                    uvs[uvsOffset + 11] = defaultUvs[3];

                    uvs[uvsOffset + 12] = defaultUvs[0];
                    uvs[uvsOffset + 13] = defaultUvs[1];
                    uvs[uvsOffset + 14] = defaultUvs[2];
                    uvs[uvsOffset + 15] = defaultUvs[3];

                    uvs[uvsOffset + 16] = defaultUvs[0];
                    uvs[uvsOffset + 17] = defaultUvs[1];
                    uvs[uvsOffset + 18] = defaultUvs[2];
                    uvs[uvsOffset + 19] = defaultUvs[3];

                    uvs[uvsOffset + 20] = defaultUvs[0];
                    uvs[uvsOffset + 21] = defaultUvs[1];
                    uvs[uvsOffset + 22] = defaultUvs[2];
                    uvs[uvsOffset + 23] = defaultUvs[3];
                }
            }

            // Set normals for each mesh
            for (int i = 0; i < numDetailTypes; i++) 
            { 
                for (int j = 0; j < numObjectsPerDetail; j++)
                {
                    int normalsOffset = (i * numObjectsPerDetail + j) * verticesPerPosition;
                    normals[normalsOffset + 0] = defaultNormals[0];
                    normals[normalsOffset + 1] = defaultNormals[0];
                    normals[normalsOffset + 2] = defaultNormals[0];
                    normals[normalsOffset + 3] = defaultNormals[0];

                    normals[normalsOffset + 4] = defaultNormals[1];
                    normals[normalsOffset + 5] = defaultNormals[1];
                    normals[normalsOffset + 6] = defaultNormals[1];
                    normals[normalsOffset + 7] = defaultNormals[1];

                    normals[normalsOffset + 8] = defaultNormals[2];
                    normals[normalsOffset + 9] = defaultNormals[2];
                    normals[normalsOffset + 10] = defaultNormals[2];
                    normals[normalsOffset + 11] = defaultNormals[2];

                    normals[normalsOffset + 12] = defaultNormals[3];
                    normals[normalsOffset + 13] = defaultNormals[3];
                    normals[normalsOffset + 14] = defaultNormals[3];
                    normals[normalsOffset + 15] = defaultNormals[3];

                    normals[normalsOffset + 16] = defaultNormals[4];
                    normals[normalsOffset + 17] = defaultNormals[4];
                    normals[normalsOffset + 18] = defaultNormals[4];
                    normals[normalsOffset + 19] = defaultNormals[4];

                    normals[normalsOffset + 20] = defaultNormals[5];
                    normals[normalsOffset + 21] = defaultNormals[5];
                    normals[normalsOffset + 22] = defaultNormals[5];
                    normals[normalsOffset + 23] = defaultNormals[5];
                }
            }

            
            
            for (int i = 0; i < numDetailTypes; i++) 
            {
                for (int j = 0; j < numObjectsPerDetail; j++)
                {
                    int verticesOffset = j * verticesPerPosition;
                    int trianglesOffset = (i * numObjectsPerDetail + j) * trianglesPerPosition;

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
                }
            }
        }
    }

    public IEnumerator SpawnCircleDetailsMesh()
    {
        int maxObjectsPerMesh = 1024;

        int verticesPerPosition = 24;
        int trianglesPerPosition = 36;
        
        int numDetailTypes = this.detailMaterials.Length;
        int totalNumObjects = this.positions.Length;

        int numObjectsPerDetail = totalNumObjects / numDetailTypes;

        int numJobs = totalNumObjects / maxObjectsPerMesh;
        int numMeshes = numJobs * numDetailTypes;
        Mesh[] meshes = new Mesh[numMeshes];

        for (int i = 0; i < numJobs; i++)
        {
            int numObjectsInCurMesh = (i == numJobs - 1) ? totalNumObjects % maxObjectsPerMesh : maxObjectsPerMesh;

            int verticesLength = numObjectsInCurMesh * verticesPerPosition;
            int trianglesLength = numObjectsInCurMesh * trianglesPerPosition;
            int uvsLength = numObjectsInCurMesh * verticesPerPosition;
            int normalsLength = numObjectsInCurMesh * verticesPerPosition;

            int verticesPerDetail = verticesLength / numDetailTypes;
            int trianglesPerDetail = trianglesLength / numDetailTypes;
            int uvsPerDetail = uvsLength / numDetailTypes;
            int normalsPerDetail = normalsLength / numDetailTypes;

            NativeArray<Vector3> verticesNat = new NativeArray<Vector3>(verticesLength, Allocator.TempJob);
            NativeArray<int> trianglesNat = new NativeArray<int>(trianglesLength, Allocator.TempJob);
            NativeArray<Vector2> uvsNat = new NativeArray<Vector2>(uvsLength, Allocator.TempJob);
            NativeArray<Vector3> normalsNat = new NativeArray<Vector3>(normalsLength, Allocator.TempJob);

            NativeArray<float> xCoordsNat = new NativeArray<float>(numObjectsInCurMesh, Allocator.TempJob);
            NativeArray<float> yCoordsNat = new NativeArray<float>(numObjectsInCurMesh, Allocator.TempJob);
            NativeArray<float> zCoordsNat = new NativeArray<float>(numObjectsInCurMesh, Allocator.TempJob);
            NativeArray<Vector3> scalesNat = new NativeArray<Vector3>(numObjectsInCurMesh, Allocator.TempJob);

            NativeArray<float>.Copy(this.positions.xCoords, i * maxObjectsPerMesh, xCoordsNat, 0, numObjectsInCurMesh);
            NativeArray<float>.Copy(this.positions.yCoords, i * maxObjectsPerMesh, yCoordsNat, 0, numObjectsInCurMesh);
            NativeArray<float>.Copy(this.positions.zCoords, i * maxObjectsPerMesh, zCoordsNat, 0, numObjectsInCurMesh);
            NativeArray<Vector3>.Copy(this.positions.scales, i * maxObjectsPerMesh, scalesNat, 0, numObjectsInCurMesh);
            

            GenerateCircleDetailsMeshDataJob burstJob = new GenerateCircleDetailsMeshDataJob
            {
                vertices = verticesNat,
                uvs = uvsNat,
                triangles = trianglesNat,
                normals = normalsNat,
                xCoords = xCoordsNat,
                yCoords = yCoordsNat,
                zCoords = zCoordsNat,
                scales = scalesNat,
                numDetailTypes = numDetailTypes,
                numObjects = numObjectsInCurMesh,
            };
                
            burstJob.Schedule().Complete();

            for (int j = 0; j < numDetailTypes; j++)
            {
                int meshIndex = i * numDetailTypes + j;
                meshes[meshIndex] = new Mesh();
                meshes[meshIndex].indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                meshes[meshIndex].SetVertices(verticesNat, j * verticesPerDetail, verticesPerDetail);
                meshes[meshIndex].SetTriangles(trianglesNat.GetSubArray(j * trianglesPerDetail, trianglesPerDetail).ToArray(), 0);
                meshes[meshIndex].SetNormals(normalsNat, j * normalsPerDetail, normalsPerDetail);
                meshes[meshIndex].SetUVs(0, uvsNat.GetSubArray(j * uvsPerDetail, uvsPerDetail));
            }

            verticesNat.Dispose();
            trianglesNat.Dispose();
            uvsNat.Dispose();
            normalsNat.Dispose();
            xCoordsNat.Dispose();
            yCoordsNat.Dispose();
            zCoordsNat.Dispose();
            scalesNat.Dispose();

            yield return null;
        }

        CreateGameObjects(meshes);
    }

    private void CreateGameObjects(Mesh[] meshes)
    {
        int numMeshes = meshes.Length;

        GameObject[] detailObjects = new GameObject[numMeshes];
        for (int i = 0; i < numMeshes; i++)
        {
            detailObjects[i] = new GameObject();
            detailObjects[i].transform.parent = this.parent;
            detailObjects[i].transform.localPosition = new Vector3(0f, 0f, 0f);
        }

        for (int i = 0; i < meshes.Length; i++)
        {
            MeshFilter groupMeshFilter = detailObjects[i].AddComponent<MeshFilter>();
            MeshRenderer groupMeshRenderer = detailObjects[i].AddComponent<MeshRenderer>();
            groupMeshFilter.sharedMesh = meshes[i];
            groupMeshRenderer.sharedMaterial = this.detailMaterials[i % this.detailMaterials.Length];

            // TODO options for lighting on details
            groupMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            groupMeshRenderer.receiveShadows = true;
            groupMeshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;

            detailObjects[i].SetActive(!hide);
        }
    }
}

