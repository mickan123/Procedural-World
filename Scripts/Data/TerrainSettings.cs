using UnityEngine;
using System.Threading;

[System.Serializable, CreateAssetMenu(menuName = "Procedural Generation Settings/TerrainSettings")]
public class TerrainSettings : ScriptableObject
{
    // Custom editor toolbar tabs settings
    [HideInInspector] public int toolbarTop;
    [HideInInspector] public int toolbarBottom;
    [HideInInspector] public string currentTab;

    // Common Settings
    public int seed;

    // Biome settings
    public BiomeGraph humidityMapGraph;
    public BiomeGraph temperatureMapGraph;
    [Range(0, 1)] public float transitionDistance;
    public BiomeSettings[] biomeSettings;

    // Detail settings
    [Range(0, 250)] public int detailViewDistance = 80;
    [Range(0, 1)] public float wavingGrassAmount = 1f;
    [Range(0, 1)] public float wavingGrassSpeed = 1f;
    [Range(0, 1)] public float wavingGrassStrength = 1f;
    public Color wavingGrassTint = new Color(0.5f, 0.5f, 0.5f);
    public int detailResolutionPerPatch = 16;
    [Range(0, 1)] public float detailDensity;
    public readonly int detailResolutionFactor = 1;
    
    // Resolution and width settings
    public int widthIdx = 0;
    public int resolutionIdx = 0;
    public readonly string[] validHeightMapWidths = { "129", "257", "513", "1025", "2049", "4097" };
    
    // Preview objects
    private Renderer previewTextureObject;
    public string previewName = "Preview Chunk";
    public Material previewMaterial;

    // Preview settings
    public enum DrawMode { SingleBiomeMesh, BiomesMesh };
    public DrawMode drawMode;
    public Vector2 chunkCoord;
    public int singleBiomeIndex = 0;
    public int noiseMapBiomeIndex = 0;
    
    // Actual size of chunks in world space
    public int width 
    {
        get 
        {
            return int.Parse(this.validHeightMapWidths[widthIdx]);
        } 
    }

    // Width of heightmaps used to generate chunks, gets scaled up to width
    public int resolution 
    {
        get 
        {
            return int.Parse(this.validHeightMapWidths[resolutionIdx]);
        }
    }

    public float scale
    {
        get
        {
            return (float)this.width / (float)this.resolution;
        }
    }


    // Constants
    private const TextureFormat textureFormat = TextureFormat.RGB565;
    private const int textureSize = 256;
    public readonly int maxTexturesPerBiome = 8;
    public readonly int maxBiomeCount = 8;

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

        humidityMapGraph.Init(prng);
        temperatureMapGraph.Init(prng);

        for (int i = 0; i < biomeSettings.Length; i++)
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


        if (drawMode == DrawMode.BiomesMesh)
        {
            DrawBiomeMesh(width);
        }
        else if (drawMode == DrawMode.SingleBiomeMesh)
        {
            DrawSingleBiome(width);
        }
    }

    private void ResetPreview()
    {
        GameObject prevPreviewObject = GameObject.Find(previewName);
        if (prevPreviewObject)
        {
            DestroyImmediate(prevPreviewObject.gameObject);
        }
    }

    private void DrawSingleBiome(int width)
    {
        BiomeSettings[] oldBiomes = new BiomeSettings[this.biomeSettings.Length];
        float oldTransitionDistance = this.transitionDistance;

        try
        {
            for (int i = 0; i < this.biomeSettings.Length; i++)
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
            DrawBiomeMesh(width);
        }
        finally
        {
            // Reset settings
            for (int i = 0; i < this.biomeSettings.Length; i++)
            {
                this.biomeSettings[i].startHumidity = oldBiomes[i].startHumidity;
                this.biomeSettings[i].endHumidity = oldBiomes[i].endHumidity;
                this.biomeSettings[i].startTemperature = oldBiomes[i].startTemperature;
                this.biomeSettings[i].endTemperature = oldBiomes[i].endTemperature;
            }
            this.transitionDistance = oldTransitionDistance;
        }
    }

    private void DrawBiomeMesh(int width)
    {
#if UNITY_EDITOR
        float startTime = 0f;
        if (this.IsMainThread())
        {
            startTime = Time.realtimeSinceStartup;
        }
#endif
        this.chunk = new TerrainChunk(
            new ChunkCoord((int)chunkCoord.x, (int)chunkCoord.y),
            this,
            null,
            this.previewMaterial,
            previewName
        );
        this.chunk.LoadInEditor();
        this.chunk.SetVisible(true);
        this.chunk.chunkObject.AddComponent<HideOnPlay>();

#if UNITY_EDITOR
        if (this.IsMainThread())
        {
            float endTime = Time.realtimeSinceStartup;
            float totalTimeTaken = endTime - startTime;
            Debug.Log("Total time taken: " + totalTimeTaken + "s");
        }
#endif
    }

    public void ApplyToMaterial(Material material)
    {
        material.SetInt("chunkWidth", this.width);
        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);

        this.ApplyHeightSlopeToMaterial(material);
        this.ApplyRoadToMaterial(material);
    }

    private void ApplyHeightSlopeToMaterial(Material material)
    {
        // Slope height texture settings
        float[] numTexturesPerBiome = new float[biomeSettings.Length];
        float[] startHeights = new float[biomeSettings.Length * maxTexturesPerBiome];
        float[] endHeights = new float[biomeSettings.Length * maxTexturesPerBiome];
        float[] startSlopes = new float[biomeSettings.Length * maxTexturesPerBiome];
        float[] endSlopes = new float[biomeSettings.Length * maxTexturesPerBiome];
        Color[] tints = new Color[biomeSettings.Length * maxTexturesPerBiome];
        float[] tintStrengths = new float[biomeSettings.Length * maxTexturesPerBiome];
        float[] blendStrength = new float[biomeSettings.Length * maxTexturesPerBiome];
        float[] textureScales = new float[biomeSettings.Length * maxTexturesPerBiome];

        this.biomeBaseTexturesArray = new Texture2DArray(textureSize, textureSize, maxTexturesPerBiome * maxBiomeCount, textureFormat, true);

        for (int i = 0; i < biomeSettings.Length; i++)
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

    private void ApplyRoadToMaterial(Material material)
    {
        // Slope height texture settings
        float[] numRoadTexturesPerBiome = new float[biomeSettings.Length];
        float[] roadStartHeights = new float[biomeSettings.Length * maxTexturesPerBiome];
        float[] roadEndHeights = new float[biomeSettings.Length * maxTexturesPerBiome];
        float[] roadStartSlopes = new float[biomeSettings.Length * maxTexturesPerBiome];
        float[] roadEndSlopes = new float[biomeSettings.Length * maxTexturesPerBiome];
        Color[] roadTints = new Color[biomeSettings.Length * maxTexturesPerBiome];
        float[] roadTintStrengths = new float[biomeSettings.Length * maxTexturesPerBiome];
        float[] roadBlendStrength = new float[biomeSettings.Length * maxTexturesPerBiome];
        float[] roadTextureScales = new float[biomeSettings.Length * maxTexturesPerBiome];

        this.roadTextureArray = new Texture2DArray(textureSize, textureSize, maxTexturesPerBiome * maxBiomeCount, textureFormat, true);

        for (int i = 0; i < biomeSettings.Length; i++)
        {
            numRoadTexturesPerBiome[i] = 1;
            RoadSettings roadSettings = biomeSettings[i].biomeGraph.GetRoadSettings();
            if (roadSettings == null)
            {
                continue;
            }
            TextureData curData = roadSettings.roadTexture;
            roadStartHeights[i * maxTexturesPerBiome] = curData.startHeight;
            roadEndHeights[i * maxTexturesPerBiome] = curData.endHeight;

            // Normalize slopes into [0, 1] range
            roadStartSlopes[i * maxTexturesPerBiome] = curData.startSlope / 90f;
            roadEndSlopes[i * maxTexturesPerBiome] = curData.endSlope / 90f;

            roadTints[i * maxTexturesPerBiome] = curData.tint;
            roadTintStrengths[i * maxTexturesPerBiome] = curData.tintStrength;
            roadBlendStrength[i * maxTexturesPerBiome] = curData.blendStrength;
            roadTextureScales[i * maxTexturesPerBiome] = curData.textureScale;

            if (curData.texture != null)
            {
                this.roadTextureArray.SetPixels(curData.texture.GetPixels(0, 0, textureSize, textureSize), i * maxTexturesPerBiome);
            }
        }
        this.roadTextureArray.Apply();

        // Apply base biome texture settings
        material.SetTexture("roadTextures", this.roadTextureArray);
        material.SetFloatArray("numTexturesPerBiome", numRoadTexturesPerBiome);
        material.SetFloatArray("roadStartHeights", roadStartHeights);
        material.SetFloatArray("roadEndHeights", roadEndHeights);
        material.SetFloatArray("roadStartSlopes", roadStartSlopes);
        material.SetFloatArray("roadEndSlopes", roadEndSlopes);
        material.SetColorArray("roadTints", roadTints);
        material.SetFloatArray("roadTintStrengths", roadTintStrengths);
        material.SetFloatArray("roadBlendStrength", roadBlendStrength);
        material.SetFloatArray("roadTextureScales", roadTextureScales);
    }

    public float maxRoadWidth
    {
        get
        {
            float maxRoadWidth = 0f;
            for (int i = 0; i < biomeSettings.Length; i++)
            {
                float width = biomeSettings[i].biomeGraph.GetMaxRoadWidth();
                if (width > maxRoadWidth)
                {
                    maxRoadWidth = width;
                }
            }
            return maxRoadWidth;
        }
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
            for (int i = 0; i < biomeSettings.Length; i++)
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
        for (int i = 0; i < biomeSettings.Length; i++)
        {
            if (biomeSettings[i] != null)
            {
                biomeSettings[i].OnValidate();
            }
        }
        this.resolutionIdx = Mathf.Min(this.resolutionIdx, this.widthIdx);
    }

#endif
}