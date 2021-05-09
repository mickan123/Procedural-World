using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk
{
    const float colliderGenerationDistanceThreshold = 200;

    public event System.Action<TerrainChunk, bool> onVisibilityChanged;
    public ChunkCoord coord;

    public GameObject meshObject;
    private Vector2 sampleCentre;
    private Bounds bounds;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private MaterialPropertyBlock matBlock;
    private Material material;

    private LODInfo[] detailLevels;
    private LODMesh[] lodMeshes;
    private int colliderLODIndex;

    public ChunkData chunkData;
    private float[,] heightMap;
    private bool heightMapReceived;
    private int previousLODIndex = -1;
    private bool hasSetCollider;

    private float maxViewDst;
    private float meshWorldSize;

    private MeshSettings meshSettings;
    private TerrainSettings terrainSettings;
    private Transform viewer;

    // Keep copy of these so that shader doesn't lose values 
    private Texture2D biomeMapTex;
    private Texture2D[] biomeStrengthTextures;
    private Texture2DArray biomeStrengthTexArray;

    public TerrainChunk(
        ChunkCoord coord,
        TerrainSettings terrainSettings,
        LODInfo[] detailLevels,
        int colliderLODIndex,
        Transform parent,
        Material material,
        Transform viewer,
        String name = "Terrain Chunk"
    )
    {
        this.coord = coord;
        this.detailLevels = detailLevels;
        this.colliderLODIndex = colliderLODIndex;
        this.terrainSettings = terrainSettings;
        this.meshSettings = terrainSettings.meshSettings;
        this.material = material;
        sampleCentre = new Vector2(coord.x * meshSettings.meshWorldSize / meshSettings.meshScale + terrainSettings.offset.x,
                                   coord.y * meshSettings.meshWorldSize / meshSettings.meshScale + terrainSettings.offset.x);

        Vector2 position = new Vector2(coord.x * meshSettings.meshWorldSize, coord.y * meshSettings.meshWorldSize);
        bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

        meshObject = new GameObject(name);
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = this.material;
        matBlock = new MaterialPropertyBlock();

        this.viewer = (viewer == null) ? meshObject.transform : viewer;

        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        meshObject.transform.parent = parent;
        SetVisible(false);

        lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].updateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex)
            {
                lodMeshes[i].updateCallback += UpdateCollisionMesh;
            }
        }
        meshWorldSize = terrainSettings.meshSettings.meshWorldSize - 1;
        maxViewDst = detailLevels[detailLevels.Length - 1].chunkDistanceThreshold * meshWorldSize;
    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(() => ChunkDataGenerator.GenerateChunkData(terrainSettings, sampleCentre), OnChunkDataReceived);
    }

    public void LoadInEditor()
    {

#if (PROFILE && UNITY_EDITOR)
        float startTime = 0f;
        if (terrainSettings.IsMainThread())
        {
            startTime = Time.realtimeSinceStartup;
        }
#endif

        this.terrainSettings.ApplyToMaterial(this.material);

#if (PROFILE && UNITY_EDITOR)
        if (terrainSettings.IsMainThread())
        {
            float endTime = Time.realtimeSinceStartup;
            float timeTaken = endTime - startTime;
            Debug.Log("Apply to material time taken: " + timeTaken + "s");
        }
#endif

        this.chunkData = ChunkDataGenerator.GenerateChunkData(this.terrainSettings, sampleCentre);

        for (int i = 0; i < lodMeshes.GetLength(0); i++)
        {
            this.lodMeshes[i].GenerateMeshEditor(this.chunkData.biomeData.heightNoiseMap, this.meshSettings);
        }
        OnChunkDataReceived(this.chunkData);
    }

    void OnChunkDataReceived(object chunkData)
    {
        this.chunkData = (ChunkData)chunkData;
        this.heightMap = this.chunkData.biomeData.heightNoiseMap;
        heightMapReceived = true;
        this.UpdateTerrainChunk();

#if (PROFILE && UNITY_EDITOR)
        float startTime = 0f;
        if (terrainSettings.IsMainThread())
        {
            startTime = Time.realtimeSinceStartup;
        }
#endif

        this.UpdateMaterial();

#if (PROFILE && UNITY_EDITOR)
        if (terrainSettings.IsMainThread())
        {
            float endTime = Time.realtimeSinceStartup;
            float timeTaken = endTime - startTime;
            Debug.Log("Update material time taken: " + timeTaken + "s");
        }
#endif

#if (PROFILE && UNITY_EDITOR)
        float spawnObjectsStartTime = 0f;
        if (terrainSettings.IsMainThread())
        {
            spawnObjectsStartTime = Time.realtimeSinceStartup;
        }
#endif

        List<ObjectSpawner> spawnObjects = this.chunkData.objects;
        for (int i = 0; i < spawnObjects.Count; i++)
        {
            spawnObjects[i].Spawn(meshObject.transform);
        }

#if (PROFILE && UNITY_EDITOR)
        if (terrainSettings.IsMainThread())
        {
            float spawnObjectsEndTime = Time.realtimeSinceStartup;
            float spawnObjectsTimeTaken = spawnObjectsEndTime - spawnObjectsStartTime;
            Debug.Log("Time to spawn objects: " + spawnObjectsTimeTaken + "s");
        }
#endif
    }

    public void UpdateMaterial()
    {


        BiomeInfo info = this.chunkData.biomeData.biomeInfo;
        int width = info.biomeMap.GetLength(0) - 3;

        // Create texture to pass in biome maps and biome strengths
        int numBiomes = this.terrainSettings.biomeSettings.Count;
        this.biomeMapTex = new Texture2D(width, width, TextureFormat.RGBA32, false, false);

        int finalTexWidth = 256;
        int biomesPerTexture = 4;
        this.biomeStrengthTextures = new Texture2D[terrainSettings.maxBiomeCount / biomesPerTexture + 1];
        for (int i = 0; i < terrainSettings.maxBiomeCount / biomesPerTexture + 1; i++)
        {
            biomeStrengthTextures[i] = new Texture2D(width, width, TextureFormat.RGBA32, false, false);
        }
        this.biomeStrengthTexArray = new Texture2DArray(
            finalTexWidth,
            finalTexWidth,
            terrainSettings.maxBiomeCount,
            TextureFormat.RGBA32,
            false,
            false
        );

        biomeMapTex.filterMode = FilterMode.Trilinear;
        biomeStrengthTexArray.filterMode = FilterMode.Trilinear; // TODO: Should this be bilinear

        float[,] heightMap = this.chunkData.biomeData.heightNoiseMap;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {   
                // Average 4 corners of a point to get the pixel road strength
                // Offset of 1 due to having out of mesh vertices for normal calculations
                int offset = 1;
                float roadStrength = (chunkData.roadStrengthMap[x + offset, y + offset] 
                    + chunkData.roadStrengthMap[x + offset + 1, y + offset] 
                    + chunkData.roadStrengthMap[x + offset + 1, y + offset + 1]
                    + chunkData.roadStrengthMap[x + offset, y + offset + 1]) / 4f;

                float angle = (Common.CalculateAngle(x + offset, y + offset, heightMap)
                            + Common.CalculateAngle(x + offset + 1, y + offset, heightMap)
                            + Common.CalculateAngle(x + offset, y + offset + 1, heightMap)
                            + Common.CalculateAngle(x + offset + 1, y + offset + 1, heightMap)) / 4f;
                angle /= 90f; // Normalize 0 to 1 range

                biomeMapTex.SetPixel(x, y, new Color(chunkData.roadStrengthMap[x + offset, y + offset], angle, 0f, 0f));

                for (int k = 0; k < terrainSettings.maxBiomeCount; k += biomesPerTexture)
                {
                    int texIndex = k / biomesPerTexture;

                    Color biomeStrengths = new Color(
                        (k < numBiomes) ? info.biomeStrengths[x + offset, y + 1, k] : 0f,
                        (k + 1 < numBiomes) ? info.biomeStrengths[x + offset, y + offset, k + 1] : 0f,
                        (k + 2 < numBiomes) ? info.biomeStrengths[x + offset, y + offset, k + 2] : 0f,
                        (k + 3 < numBiomes) ? info.biomeStrengths[x + offset, y + offset, k + 3] : 0f
                    );
                    biomeStrengthTextures[texIndex].SetPixel(x, y, biomeStrengths);
                }
            }
        }

        for (int i = 0; i < biomeStrengthTextures.Length; i++)
        {
            TextureScale.Bilinear(biomeStrengthTextures[i], finalTexWidth, finalTexWidth);
            biomeStrengthTextures[i].Apply();
            biomeStrengthTexArray.SetPixels(biomeStrengthTextures[i].GetPixels(), i);
        }
        biomeStrengthTexArray.Apply();
        matBlock.SetTexture("biomeStrengthMap", biomeStrengthTexArray);

        biomeMapTex.Apply();
        matBlock.SetTexture("biomeMapTex", biomeMapTex);

        this.meshRenderer.SetPropertyBlock(matBlock);
    }

    public static void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
    {
        byte[] _bytes = _texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(_fullPath, _bytes);
        Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath);
    }

    Vector2 viewerPosition
    {
        get
        {
            return new Vector2(viewer.position.x, viewer.position.z);
        }
    }

    public void UpdateTerrainChunk()
    {
        if (heightMapReceived)
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

            bool wasVisible = meshObject.activeSelf;
            bool visible = viewerDstFromNearestEdge <= maxViewDst;

            if (visible)
            {
                int lodIndex = 0;

                for (int i = 0; i < detailLevels.Length - 1; i++)
                {
                    if (viewerDstFromNearestEdge > (detailLevels[i].chunkDistanceThreshold * meshWorldSize))
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (lodIndex != previousLODIndex)
                {
                    LoadLODMesh(lodIndex);
                }
            }

            if (wasVisible != visible)
            {
                SetVisible(visible);
                if (onVisibilityChanged != null)
                {
                    onVisibilityChanged(this, visible);
                }
            }
        }
    }

    public void LoadLODMesh(int lodIndex)
    {
        LODMesh lodMesh = lodMeshes[lodIndex];
        if (lodMesh.hasMesh)
        {
            previousLODIndex = lodIndex;
            meshFilter.mesh = lodMesh.mesh;
        }
        else if (!lodMesh.hasRequestedMesh)
        {
            lodMesh.RequestMesh(heightMap, meshSettings);
        }
    }

    public void UpdateCollisionMesh()
    {
        if (!hasSetCollider)
        {
            float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

            float viewDist = detailLevels[colliderLODIndex].chunkDistanceThreshold * meshWorldSize;

            if (sqrDstFromViewerToEdge < viewDist * viewDist)
            {
                if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
                {
                    lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
                }
            }

            if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
            {
                if (lodMeshes[colliderLODIndex].hasMesh)
                {
                    meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                    hasSetCollider = true;
                }
            }
        }
    }

    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }
}

class LODMesh
{
    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    int lod;
    public event System.Action updateCallback;

    public LODMesh(int lod)
    {
        this.lod = lod;
    }

    void OnMeshDataReceived(object meshDataObject)
    {
        mesh = ((MeshData)meshDataObject).CreateMesh();
        hasMesh = true;

        updateCallback();
    }

    public void RequestMesh(float[,] heightMap, MeshSettings meshSettings)
    {
        hasRequestedMesh = true;
        ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap, meshSettings, lod), OnMeshDataReceived);
    }

    public void GenerateMeshEditor(float[,] heightMap, MeshSettings meshSettings)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, meshSettings, lod);
        this.mesh = meshData.CreateMesh();
        hasMesh = true;
    }
}