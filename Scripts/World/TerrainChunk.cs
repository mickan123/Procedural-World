using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk
{
    const float colliderGenerationDistanceThreshold = 200;

    public event System.Action<TerrainChunk> onChunkLoaded;
    public ChunkCoord coord;

    public GameObject chunkObject;
    private Vector2 sampleCentre;

    private MaterialPropertyBlock matBlock;
    private Material material;

    public ChunkData chunkData;
    private float[] heightMap;
    private bool heightMapReceived;
    
    public TerrainSettings terrainSettings;

    // Keep copy of these so that shader doesn't lose values 
    private Texture2D biomeMapTex;
    private Texture2D[] biomeStrengthTextures;
    private Texture2DArray biomeStrengthTexArray;

    public Terrain terrain;
    private TerrainCollider terrainCollider;
    private TerrainData terrainData;

    public bool readyToSpawnObjects = false;

    public TerrainChunk(
        ChunkCoord coord,
        TerrainSettings terrainSettings,
        Transform parent,
        Material material,
        String name = "Terrain Chunk"
    )
    {
        this.coord = coord;
        this.terrainSettings = terrainSettings;
        this.material = material;
        float halfChunkWidth = (float)(terrainSettings.resolution) / 2f;
        float resolution = (terrainSettings.resolution - 1f);
        sampleCentre = new Vector2((coord.x * resolution + halfChunkWidth) + terrainSettings.offset.x,
                                   (coord.y * resolution + halfChunkWidth) + terrainSettings.offset.y);

        Debug.Log(sampleCentre);
        this.chunkObject = new GameObject(name);
        this.terrain = this.chunkObject.AddComponent<Terrain>();
        this.terrain.materialTemplate = material;
        this.terrain.detailObjectDistance = terrainSettings.detailViewDistance;
        this.terrain.detailObjectDensity = terrainSettings.detailDensity;
        
        this.terrainData = new TerrainData();
        
        this.terrainData.SetDetailResolution(terrainSettings.resolution * terrainSettings.detailResolutionFactor, terrainSettings.detailResolutionPerPatch);
        this.terrainData.wavingGrassAmount = terrainSettings.wavingGrassAmount;
        this.terrainData.wavingGrassSpeed = terrainSettings.wavingGrassSpeed;
        this.terrainData.wavingGrassStrength = terrainSettings.wavingGrassStrength;
        this.terrainData.wavingGrassTint = terrainSettings.wavingGrassTint;
        this.terrain.terrainData = this.terrainData;

        this.terrainCollider = this.chunkObject.AddComponent<TerrainCollider>();
        terrainCollider.terrainData = terrainData;
        
        this.matBlock = new MaterialPropertyBlock();

        this.chunkObject.transform.position = new Vector3(
            coord.y * terrainSettings.width, 
            0, 
            coord.x * terrainSettings.width
        );
        this.chunkObject.transform.parent = parent;
        SetVisible(false);
    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(() => ChunkDataGenerator.GenerateChunkData(terrainSettings, sampleCentre), OnChunkDataReceived);
    }

    public void LoadInEditor()
    {
        this.terrainSettings.ApplyToMaterial(this.material);

        this.chunkData = ChunkDataGenerator.GenerateChunkData(this.terrainSettings, sampleCentre);

        OnChunkDataReceived(this.chunkData);
    }

    private void OnChunkDataReceived(object chunkData)
    {
        this.chunkData = (ChunkData)chunkData;
        this.heightMap = this.chunkData.biomeData.heightNoiseMap;
        this.heightMapReceived = true;
        this.readyToSpawnObjects = true;

        this.ConfigureTerrainHeightMap();
        this.ConfigureTerrainDetails();
        this.UpdateMaterial();

#if UNITY_EDITOR
        List<ObjectSpawner> spawnObjects = this.chunkData.objects;
        if (!Application.isPlaying)
        {
            for (int i = 0; i < spawnObjects.Count; i++)
            {
                spawnObjects[i].Spawn(chunkObject.transform, this);
            }
        }
#endif  
        SetVisible(true);
        if (this.onChunkLoaded != null)
        {
            onChunkLoaded(this);
        }
        
    }

    private void ConfigureTerrainHeightMap()
    {
        int width = this.chunkData.biomeData.width;
        float maxHeight = terrainSettings.maxHeight;
        
        this.terrainData.heightmapResolution = this.terrainSettings.resolution;
        float[,] heights2D = new float[width, width];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < width; j++)
            {
                heights2D[i, j] = this.heightMap[i * width + j] / maxHeight;
            }
        }
        this.terrainData.SetHeights(0, 0, heights2D);
        this.terrainData.size = new Vector3(this.terrainSettings.width, maxHeight, this.terrainSettings.width);
    }

    private void ConfigureTerrainDetails()
    {
        List<ObjectSpawner> spawnObjects = this.chunkData.objects;
        List<DetailPrototype> detailPrototypes = new List<DetailPrototype>();
        int detailLayer = 0;
        for (int i = 0; i < spawnObjects.Count; i++)
        {
            if (spawnObjects[i].isDetail)
            {
                detailPrototypes.Add(spawnObjects[i].detailPrototype);
                
            }
        }
        this.terrainData.detailPrototypes = detailPrototypes.ToArray();

        for (int i = 0; i < spawnObjects.Count; i++)
        {
            if (spawnObjects[i].isDetail)
            {
                this.terrainData.SetDetailLayer(0, 0, detailLayer, spawnObjects[i].detailDensity);
                detailLayer++;
            }
        }
    }

    private void UpdateMaterial()
    {
        BiomeInfo info = this.chunkData.biomeData.biomeInfo;
        float[] biomeStrengths = info.biomeStrengths;

        int width = info.width;
        int biomeMapWidth = width - 3;
        
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
        
        // Offset of 1 for all xy coords due to having out of mesh vertices for normal calculations
        int offset = 1;
        for (int x = 0; x < biomeMapWidth; x++)
        {
            for (int y = 0; y < biomeMapWidth; y++)
            {   
                // Average 4 corners of a point to get the pixel road strength
                // x and y are swapped due to how Unity terrain is handling coordinates
                float roadStrength = (chunkData.roadStrengthMap[(y + offset) * width + x + offset] 
                    + chunkData.roadStrengthMap[(y + offset + 1) * width + x + offset] 
                    + chunkData.roadStrengthMap[(y + offset + 1) * width + x + offset + 1]
                    + chunkData.roadStrengthMap[(y + offset) * width + x + offset + 1]) / 4f;

                float angle = chunkData.angles[y * width + x] / 90f;

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

        this.terrain.SetSplatMaterialPropertyBlock(matBlock);
    }

    public void SetVisible(bool visible)
    {
        this.chunkObject.SetActive(visible);
    }
}