using System.Collections;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using XNode;

[System.Serializable, CreateAssetMenu(menuName = "Procedural Generation Settings/TerrainSettings")]
public class TerrainSettings : ScriptableObject
{

    // Custom editor toolbar tabs settings
    [HideInInspector] public int toolbarTop;
    [HideInInspector] public int toolbarBottom;
    [HideInInspector] public string currentTab;

    // Biome settings
    public BiomeGraph humidityMapGraph;
    public BiomeGraph temperatureMapGraph;

    [Range(0, 1)] public float transitionDistance;
    public List<BiomeSettings> biomeSettings = new List<BiomeSettings>();

    // Erosion settings
    public ErosionSettings erosionSettings;

    // Mesh settings
    public MeshSettings meshSettings;

    // Road settings
    public RoadSettings roadSettings;

    // Always display these settings
    public int seed;

    // Constants
    private const TextureFormat textureFormat = TextureFormat.RGB565;
    private const int textureSize = 512;
    public readonly int maxTexturesPerBiome = 8;
    public readonly int maxBiomeCount = 8;

    // Preview objects
    private Renderer previewTextureObject;
    private GameObject previewMeshObject;
    public Material previewMaterial;

    // Preview settings
    public enum DrawMode { SingleBiomeMesh, BiomesMesh, NoiseMapTexture, FalloffMapTexture, BiomesTexture, HumidityMapTexture, TemperatureMapTexture };
    public DrawMode drawMode;
    public Vector2 centre;
    public LODInfo[] detailLevels;
    [Range(0, MeshSettings.numSupportedLODs - 1)] public int editorPreviewLOD;
    public int singleBiomeIndex = 0;
    public int noiseMapBiomeIndex = 0;

    public Thread mainThread;

    // Keep reference of to these textures so that shader doesn't lose values 
    private TerrainChunk chunk;
    private Texture2DArray biomeBaseTexturesArray;
    private Texture2DArray roadTextureArray;

    public float sqrTransitionDistance
    {
        get
        {
            return (float)transitionDistance * (float)transitionDistance;
        }
    }

    public void Init()
    {
        InitSeeds();
        HydraulicErosion.Init(this);
        this.mainThread = System.Threading.Thread.CurrentThread;
    }

    public void InitSeeds()
    {
        System.Random prng = new System.Random(seed);

        erosionSettings.seed = prng.Next(-100000, 100000);

        humidityMapGraph.Init(prng);
        temperatureMapGraph.Init(prng);

        for (int i = 0; i < biomeSettings.Count; i++)
        {
            var graph = biomeSettings[i].biomeGraph;
            if (graph != null)
            {
                graph.Init(prng);
            }
        }
    }

    public void DrawMapInEditor()
    {
        this.ResetPreview();

        this.Init();

        this.previewTextureObject = new Renderer();

        int width = this.meshSettings.numVerticesPerLine;
        int height = this.meshSettings.numVerticesPerLine;

        
        if (drawMode == DrawMode.NoiseMapTexture)
        {
            float[,] heightMap = this.biomeSettings[noiseMapBiomeIndex].biomeGraph.GetHeightMap(
                this,
                this.centre,
                width,
                height
            );
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        }
        else if (drawMode == DrawMode.FalloffMapTexture)
        {
            DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(width)));
        }
        else if (drawMode == DrawMode.BiomesMesh)
        {
            DrawBiomeMesh(width, height);
        }
        else if (drawMode == DrawMode.SingleBiomeMesh)
        {
            DrawSingleBiome(width, height);
        }
        else
        {
            float[,] humidityMap = this.humidityMapGraph.GetHeightMap(
                this,
                centre,
                width,
                height
            );

            float[,] temperatureMap = this.temperatureMapGraph.GetHeightMap(
                this,
                centre,
                width,
                height
            );
            if (drawMode == DrawMode.BiomesTexture)
            {
                DrawBiomes(width, height, humidityMap, temperatureMap);
            }
            else if (drawMode == DrawMode.HumidityMapTexture)
            {
                DrawTexture(TextureGenerator.TextureFromHeightMap(humidityMap));
            }
            else if (drawMode == DrawMode.TemperatureMapTexture)
            {
                DrawTexture(TextureGenerator.TextureFromHeightMap(temperatureMap));
            }
        }
    }

    private void ResetPreview()
    {
        GameObject prevPreviewObject = GameObject.Find("Preview Chunk");
        if (prevPreviewObject)
        {
            DestroyImmediate(prevPreviewObject.gameObject);
        }
    }

    private void DrawSingleBiome(int width, int height)
    {
        BiomeSettings[] oldBiomes = new BiomeSettings[this.biomeSettings.Count];
        float oldTransitionDistance = this.transitionDistance;

        try
        {
            for (int i = 0; i < this.biomeSettings.Count; i++)
            {
                oldBiomes[i] = (BiomeSettings)(BiomeSettings.CreateInstance("BiomeSettings"));
                oldBiomes[i].startHumidity = this.biomeSettings[i].startHumidity;
                oldBiomes[i].endHumidity = this.biomeSettings[i].endHumidity;
                oldBiomes[i].startTemperature = this.biomeSettings[i].startTemperature;
                oldBiomes[i].endTemperature = this.biomeSettings[i].endTemperature;

                this.biomeSettings[i].startHumidity = 0f;
                this.biomeSettings[i].endHumidity = 0f;
                this.biomeSettings[i].startTemperature = 0f;
                this.biomeSettings[i].endTemperature = 0f;
            }

            this.biomeSettings[singleBiomeIndex].endHumidity = 1f;
            this.biomeSettings[singleBiomeIndex].endTemperature = 1f;
            this.transitionDistance = 0f;
            this.ApplyToMaterial(this.previewMaterial);

            DrawBiomeMesh(width, height);

        }
        finally
        {
            // Reset settings
            for (int i = 0; i < this.biomeSettings.Count; i++)
            {
                this.biomeSettings[i].startHumidity = oldBiomes[i].startHumidity;
                this.biomeSettings[i].endHumidity = oldBiomes[i].endHumidity;
                this.biomeSettings[i].startTemperature = oldBiomes[i].startTemperature;
                this.biomeSettings[i].endTemperature = oldBiomes[i].endTemperature;
            }
            this.transitionDistance = oldTransitionDistance;
        }
    }

    private void DrawBiomeMesh(int width, int height)
    {
#if (UNITY_EDITOR)
		float startTime = 0f;
		if (this.IsMainThread()) {
        	startTime = Time.realtimeSinceStartup;
		}
#endif

        this.chunk = new TerrainChunk(
            new ChunkCoord(0, 0),
            this,
            this.detailLevels,
            0,
            null,
            this.previewMaterial,
            null,
            "Preview Chunk"
        );
        this.chunk.LoadInEditor();
        this.chunk.SetVisible(true);
        this.chunk.meshObject.AddComponent<HideOnPlay>();

#if (UNITY_EDITOR)
		if (this.IsMainThread()) {
			float endTime = Time.realtimeSinceStartup;
			float totalTimeTaken = endTime - startTime;
			Debug.Log("Total time taken: " + totalTimeTaken + "s");
		}
#endif
    }

    private void DrawBiomes(int width, int height, float[,] humidityMap, float[,] temperatureMap)
    {
        BiomeInfo biomeInfo = BiomeHeightMapGenerator.GenerateBiomeInfo(width, height, humidityMap, temperatureMap, this);

        int numBiomes = this.biomeSettings.Count;
        float[,] biomeTextureMap = new float[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                biomeTextureMap[i, j] = (float)biomeInfo.biomeMap[i, j] / (float)(numBiomes - 1);
            }
        }

        DrawTexture(TextureGenerator.TextureFromHeightMap(biomeTextureMap));
    }

    public void DrawTexture(Texture2D texture)
    {
        this.previewTextureObject.sharedMaterial.mainTexture = texture;
        this.previewTextureObject.transform.localScale = new Vector3(-96, 1, 96);
    }

    public void ApplyToMaterial(Material material)
    {
        // Slope height texture settings
        float[] numTexturesPerBiome = new float[biomeSettings.Count];
		float[] startHeights = new float[biomeSettings.Count * maxTexturesPerBiome];
        float[] endHeights = new float[biomeSettings.Count * maxTexturesPerBiome];
        float[] startSlopes = new float[biomeSettings.Count * maxTexturesPerBiome];
        float[] endSlopes = new float[biomeSettings.Count * maxTexturesPerBiome];
        Color[] tints = new Color[biomeSettings.Count * maxTexturesPerBiome];
        float[] tintStrengths = new float[biomeSettings.Count * maxTexturesPerBiome];
        float[] blendStrength = new float[biomeSettings.Count * maxTexturesPerBiome];
        float[] textureScales = new float[biomeSettings.Count * maxTexturesPerBiome];

        this.biomeBaseTexturesArray = new Texture2DArray(textureSize, textureSize, maxTexturesPerBiome * maxBiomeCount, textureFormat, true);
        this.roadTextureArray = new Texture2DArray(textureSize, textureSize, maxTexturesPerBiome, textureFormat, true);

        for (int i = 0; i < biomeSettings.Count; i++)
        {
            numTexturesPerBiome[i] = biomeSettings[i].textureData.Count;
            for (int j = 0; j < biomeSettings[i].textureData.Count; j++)
            {
                TextureData curData = biomeSettings[i].textureData[j];
                startHeights[i * maxTexturesPerBiome + j] = curData.startHeight;
                endHeights[i * maxTexturesPerBiome + j] = curData.endHeight;

                // Normalize slopes into [0, 1] range
                startSlopes[i * maxTexturesPerBiome + j] = curData.startSlope / 90f;
                endSlopes[i * maxTexturesPerBiome + j] = curData.endSlope / 90f;

                tints[i * maxTexturesPerBiome + j] = curData.tint;
                tintStrengths[i * maxTexturesPerBiome + j] = curData.tintStrength;
                blendStrength[i * maxTexturesPerBiome + j] = curData.blendStrength;
                textureScales[i * maxTexturesPerBiome + j] = curData.textureScale;
    
                if (curData.texture != null)
                {
                    this.biomeBaseTexturesArray.SetPixels(curData.texture.GetPixels(0, 0, textureSize, textureSize), i * maxTexturesPerBiome + j);
                }
            }
        }
        this.biomeBaseTexturesArray.Apply();

        material.SetInt("chunkWidth", meshSettings.meshWorldSize);
        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);		

        // Apply base biome texture settings
        material.SetTexture("textures", this.biomeBaseTexturesArray);
        material.SetFloatArray("numTexturesPerBiome", numTexturesPerBiome);
        material.SetFloatArray("startHeights", startHeights);
        material.SetFloatArray("endHeights", endHeights);
        material.SetFloatArray("startSlopes", startSlopes);
        material.SetFloatArray("endSlopes", endSlopes);
        material.SetColorArray("tints", tints);
        material.SetFloatArray("tintStrengths", tintStrengths);
        material.SetFloatArray("blendStrength", blendStrength);
        material.SetFloatArray("textureScales", textureScales);
    }

    public float minHeight
    {
        get
        {
            return 0f;
        }
    }

    public float maxHeight
    {
        get
        {
            float maxHeight = float.MinValue;
            for (int i = 0; i < biomeSettings.Count; i++)
            {
                float height = biomeSettings[i].biomeGraph.GetMaxHeight();
                if (height > maxHeight)
                {
                    maxHeight = height;
                }
            }
            return maxHeight;
        }
    }

    public bool IsMainThread()
    {
        return this.mainThread.Equals(System.Threading.Thread.CurrentThread);
    }

#if UNITY_EDITOR

    public void OnValidate()
    {
        // TODO ensure no overlapping biome values
        erosionSettings.OnValidate();
        meshSettings.OnValidate();

        for (int i = 0; i < biomeSettings.Count; i++)
        {
            if (biomeSettings[i] != null)
            {
                biomeSettings[i].OnValidate();
            }
        }
    }

#endif
}