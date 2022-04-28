using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Stella3D;

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
    private float[] heightMap;
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

    public bool readyToSpawnObjects = false;

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
        float halfChunkWidth = meshSettings.meshWorldSize / 2;
        sampleCentre = new Vector2((coord.x * meshSettings.meshWorldSize + halfChunkWidth) / meshSettings.meshScale + terrainSettings.offset.x,
                                   (coord.y * meshSettings.meshWorldSize + halfChunkWidth) / meshSettings.meshScale + terrainSettings.offset.x);

        bounds = new Bounds(sampleCentre, Vector2.one * meshSettings.meshWorldSize);

        this.meshObject = new GameObject(name);
        this.meshRenderer = this.meshObject.AddComponent<MeshRenderer>();
        this.meshFilter = this.meshObject.AddComponent<MeshFilter>();
        this.meshCollider = this.meshObject.AddComponent<MeshCollider>();
        this.meshRenderer.material = this.material;
        this.matBlock = new MaterialPropertyBlock();

        this.viewer = (viewer == null) ? meshObject.transform : viewer;

        this.meshObject.transform.position = new Vector3(
            coord.x * meshSettings.meshWorldSize, 
            0, 
            coord.y * meshSettings.meshWorldSize
        );
        this.meshObject.transform.parent = parent;
        SetVisible(false);

        this.lodMeshes = new LODMesh[detailLevels.Length];
        int length = detailLevels.Length;
        for (int i = 0; i < length; i++)
        {
            this.lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            this.lodMeshes[i].updateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex)
            {
                this.lodMeshes[i].updateCallback += UpdateCollisionMesh;
            }
        }
        this.meshWorldSize = terrainSettings.meshSettings.meshWorldSize - 1;
        this.maxViewDst = detailLevels[detailLevels.Length - 1].chunkDistanceThreshold * meshWorldSize;
    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(() => ChunkDataGenerator.GenerateChunkData(terrainSettings, sampleCentre), OnChunkDataReceived);
    }

    public void LoadInEditor()
    {
        this.terrainSettings.ApplyToMaterial(this.material);

        this.chunkData = ChunkDataGenerator.GenerateChunkData(this.terrainSettings, sampleCentre);

        int lodMeshesLength = lodMeshes.Length;
        for (int i = 0; i < lodMeshesLength; i++)
        {
            this.lodMeshes[i].GenerateMeshEditor(this.chunkData.biomeData.heightNoiseMap, this.meshSettings);
        }
        OnChunkDataReceived(this.chunkData);
    }

    private void OnChunkDataReceived(object chunkData)
    {
        this.chunkData = (ChunkData)chunkData;
        this.heightMap = this.chunkData.biomeData.heightNoiseMap;
        this.heightMapReceived = true;
        this.readyToSpawnObjects = true;

        this.UpdateTerrainChunk();
        this.UpdateMaterial();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            List<ObjectSpawner> spawnObjects = this.chunkData.objects;
            for (int i = 0; i < spawnObjects.Count; i++)
            {
                spawnObjects[i].Spawn(meshObject.transform);
            }
        }
#endif
    }

    public void UpdateMaterial()
    {
        BiomeInfo info = this.chunkData.biomeData.biomeInfo;
        float[] biomeStrengths = info.biomeStrengths;
        float[] heightMap = this.chunkData.biomeData.heightNoiseMap;

        int width = info.width;
        int biomeMapWidth = width - 3;
        
        NativeArray<float> heightMapNat = new NativeArray<float>(heightMap, Allocator.TempJob);

        // Use shared array so that we convert between native and non native with no cost
        SharedArray<float> anglesNat = new SharedArray<float>(width * width);

        Common.CalculateAnglesJob burstJob = new Common.CalculateAnglesJob{
            heightMap = heightMapNat,
            angles = anglesNat,
            width = width
        };
        JobHandle handle = burstJob.Schedule();

        // Create texture to pass in biome maps and biome strengths
        int numBiomes = this.terrainSettings.biomeSettings.Length;
        this.biomeMapTex = new Texture2D(biomeMapWidth, biomeMapWidth, TextureFormat.RGBA32, false, false);

        // Create biomeStrength textures representing the strength of each biome
        int biomesPerTexture = 4;
        this.biomeStrengthTextures = new Texture2D[terrainSettings.maxBiomeCount / biomesPerTexture];
        for (int i = 0; i < terrainSettings.maxBiomeCount / biomesPerTexture; i++)
        {
            biomeStrengthTextures[i] = new Texture2D(biomeMapWidth, biomeMapWidth, TextureFormat.RGBA32, false, false);
        }
        this.biomeStrengthTexArray = new Texture2DArray(
            biomeMapWidth,
            biomeMapWidth,
            terrainSettings.maxBiomeCount,
            TextureFormat.RGBA32,
            false,
            false
        );

        biomeMapTex.filterMode = FilterMode.Trilinear;
        biomeStrengthTexArray.filterMode = FilterMode.Trilinear; // TODO: Should this be bilinear

        // Create arrays to hold pixel colors so we can use SetPixels vs SetPixel individually
        byte[] biomeMapTexPixels = new byte[biomeMapWidth * biomeMapWidth * 4];
        byte[][] biomeStrengthTexPixels = new byte[biomeStrengthTextures.Length][];
        int biomeStrengthTexPixelsLength = biomeStrengthTexPixels.Length;
        for (int i = 0; i < biomeStrengthTexPixelsLength; i++) {
            biomeStrengthTexPixels[i] = new byte[biomeMapWidth * biomeMapWidth * 4];
        }

        handle.Complete();

        // Convert to managed array as NativeArray has safety checks in editor
        float[] angles = anglesNat;
        
        // Offset of 1 for all xy coords due to having out of mesh vertices for normal calculations
        int offset = 1;
        for (int x = 0; x < biomeMapWidth; x++)
        {
            for (int y = 0; y < biomeMapWidth; y++)
            {   
                // Average 4 corners of a point to get the pixel road strength
                float roadStrength = (chunkData.roadStrengthMap[(x + offset) * width + y + offset] 
                    + chunkData.roadStrengthMap[(x + offset + 1) * width + y + offset] 
                    + chunkData.roadStrengthMap[(x + offset + 1) * width + y + offset + 1]
                    + chunkData.roadStrengthMap[(x + offset) * width + y + offset + 1]) / 4f;

                float angle = angles[x * width + y] / 90f;

                // Get biomeMap pixel data (2 unused values b,a out of rgba)
                int biomeMapTexIdx = y * biomeMapWidth * 4 + x * 4;
                biomeMapTexPixels[biomeMapTexIdx] =  (byte)(roadStrength * 255);
                biomeMapTexPixels[biomeMapTexIdx + 1] =  (byte)(angle * 255);

                // Create biomestrength pixel data
                for (int k = 0; k < terrainSettings.maxBiomeCount; k += biomesPerTexture)
                {
                    int texIndex = k / biomesPerTexture;
                    int biomeStrengthIdx = (x + offset) * width * numBiomes + (y + offset) * numBiomes + k;
                    int biomeStrengthTexIdx = y * biomeMapWidth * 4 + x * 4;

                    biomeStrengthTexPixels[texIndex][biomeStrengthTexIdx] = (k < numBiomes) ? (byte)(biomeStrengths[biomeStrengthIdx] * 255) : (byte)0;
                    biomeStrengthTexPixels[texIndex][biomeStrengthTexIdx + 1] = (k < numBiomes) ? (byte)(biomeStrengths[biomeStrengthIdx + 1] * 255) : (byte)0;
                    biomeStrengthTexPixels[texIndex][biomeStrengthTexIdx + 2] = (k < numBiomes) ? (byte)(biomeStrengths[biomeStrengthIdx + 2] * 255) : (byte)0;
                    biomeStrengthTexPixels[texIndex][biomeStrengthTexIdx + 3] = (k < numBiomes) ? (byte)(biomeStrengths[biomeStrengthIdx + 3] * 255) : (byte)0;
                }
            }
        }
        
        int biomeStrengthTexturesLength = biomeStrengthTextures.Length;
        for (int i = 0; i < biomeStrengthTexturesLength; i++)
        {
            biomeStrengthTexArray.SetPixelData<byte>(biomeStrengthTexPixels[i], 0, i);
        }
        biomeStrengthTexArray.Apply();
        matBlock.SetTexture("biomeStrengthMap", biomeStrengthTexArray);

        biomeMapTex.SetPixelData<byte>(biomeMapTexPixels, 0);
        biomeMapTex.Apply();
        matBlock.SetTexture("biomeMapTex", biomeMapTex);

        this.meshRenderer.SetPropertyBlock(matBlock);

        heightMapNat.Dispose();
        anglesNat.Dispose();
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

            bool wasVisible = this.meshObject.activeSelf;
            bool visible = viewerDstFromNearestEdge <= maxViewDst;

            if (visible)
            {
                int lodIndex = 0;

                int detailLevelsLength = detailLevels.Length;
                for (int i = 0; i < detailLevelsLength - 1; i++)
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
            this.meshFilter.mesh = lodMesh.mesh;
        }
        else if (!lodMesh.hasRequestedMesh)
        {
            lodMesh.RequestMesh(this.heightMap, this.meshSettings);
        }
    }

    public void UpdateCollisionMesh()
    {
        if (!this.hasSetCollider)
        {
            float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

            float viewDist = detailLevels[colliderLODIndex].chunkDistanceThreshold * meshWorldSize;

            if (sqrDstFromViewerToEdge < viewDist * viewDist)
            {
                if (!this.lodMeshes[colliderLODIndex].hasRequestedMesh)
                {
                    this.lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
                }
            }

            if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
            {
                if (this.lodMeshes[colliderLODIndex].hasMesh)
                {
                    this.meshCollider.sharedMesh = this.lodMeshes[colliderLODIndex].mesh;
                    this.hasSetCollider = true;
                }
            }
        }
    }

    public void SetVisible(bool visible)
    {
        this.meshObject.SetActive(visible);
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

    public void RequestMesh(float[] heightMap, MeshSettings meshSettings)
    {
        hasRequestedMesh = true;
        ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap, meshSettings, lod), OnMeshDataReceived);
    }

    public void GenerateMeshEditor(float[] heightMap, MeshSettings meshSettings)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, meshSettings, lod);
        this.mesh = meshData.CreateMesh();
        hasMesh = true;
    }
}