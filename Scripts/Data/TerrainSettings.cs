using System.Collections;
using UnityEngine;
using System.Threading;
using MyBox;
using System.Collections.Generic;

[CreateAssetMenu(), System.Serializable]
public class TerrainSettings : ScriptableObject {

	// Custom editor toolbar tabs settings
	[HideInInspector] public int toolbarTop;
	[HideInInspector] public int toolbarBottom;
	[HideInInspector] public string currentTab;
	
	// Biome settings
	public NoiseMapSettings humidityMapSettings;
	public NoiseMapSettings temperatureMapSettings;
	[Range(0,1)] public float transitionDistance;
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
	private const int biomeStrengthTextureWidth = 256;
	public readonly int maxLayerCount = 8;
	public readonly int maxBiomeCount = 8;

	// Preview objects
	private Renderer previewTextureObject;
	private MeshFilter previewMeshFilter;
	private GameObject previewMeshObject;
	private MeshCollider previewMeshCollider;
	private MeshRenderer previewMeshRenderer;
	public Material previewMaterial;

	// Preview settings
	public enum DrawMode { SingleBiomeMesh, BiomesMesh, NoiseMapTexture, FalloffMapTexture, BiomesTexture, HumidityMapTexture, TemperatureMapTexture };
	public DrawMode drawMode;
	public Vector2 centre;
	[Range(0, MeshSettings.numSupportedLODs - 1)] public int editorPreviewLOD;
	public int singleBiomeIndex = 0;
	public int noiseMapBiomeIndex = 0;

	public Thread mainThread;

	public float sqrTransitionDistance {
		get {
			return (float)transitionDistance * (float)transitionDistance;
		}
	}

	public void Init() {
		InitSeeds();
		HydraulicErosion.Init(this);
		this.mainThread = System.Threading.Thread.CurrentThread;
	}

	public void InitSeeds() {
		System.Random prng = new System.Random(seed);
		
		temperatureMapSettings.seed = prng.Next(-100000, 100000);
		humidityMapSettings.seed = prng.Next(-100000, 100000);
		erosionSettings.seed = prng.Next(-100000, 100000);

		for (int i = 0; i < biomeSettings.Count; i++) {
			biomeSettings[i].heightMapSettings.seed = prng.Next(-100000, 100000);

			for (int j = 0; j < biomeSettings[i].terrainObjectSettings.Count; j++) {
				if (biomeSettings[i].terrainObjectSettings[j] != null) {
					biomeSettings[i].terrainObjectSettings[j].noiseMapSettings.seed = prng.Next(-100000, 100000);
				}
			}
		}
	}

	public void ApplyToMaterial(Material material) {

		float[] layerCounts = new float[biomeSettings.Count];

		Color[] baseColours = new Color[maxLayerCount * maxBiomeCount];
		float[] baseStartHeights = new float[maxLayerCount * maxBiomeCount];
		float[] baseBlends = new float[maxLayerCount * maxBiomeCount];
		float[] baseColourStrength = new float[maxLayerCount * maxBiomeCount];
		float[] baseTextureScales = new float[maxLayerCount * maxBiomeCount];
		Texture2DArray texturesArray = new Texture2DArray(textureSize, textureSize, maxLayerCount * maxBiomeCount, textureFormat, true);

		for (int i = 0; i < biomeSettings.Count; i++) {

			layerCounts[i] = biomeSettings[i].textureData.textureLayers.Length;

			for (int j = 0; j < biomeSettings[i].textureData.textureLayers.Length; j++) {

				TextureLayer curLayer = biomeSettings[i].textureData.textureLayers[j];
				baseColours[i * maxLayerCount + j] = curLayer.tint;
				baseStartHeights[i * maxLayerCount + j] = curLayer.startHeight;
				baseBlends[i * maxLayerCount + j] = curLayer.blendStrength;
				baseColourStrength[i * maxLayerCount + j] = curLayer.tintStrength;
				baseTextureScales[i * maxLayerCount + j] = curLayer.textureScale;

				if (curLayer.texture != null) {
					texturesArray.SetPixels(curLayer.texture.GetPixels(0, 0, textureSize, textureSize), i * maxLayerCount + j);
				}	
			}
		}
		texturesArray.Apply();

		material.SetFloatArray("layerCounts", layerCounts);
		material.SetColorArray("baseColours", baseColours);
		material.SetFloatArray("baseStartHeights", baseStartHeights);
		material.SetFloatArray("baseBlends", baseBlends);
		material.SetFloatArray("baseColourStrengths", baseColourStrength);
		material.SetFloatArray("baseTextureScales", baseTextureScales);
		
		material.SetTexture("baseTextures", texturesArray);
		material.SetFloat("minHeight", minHeight);
		material.SetFloat("maxHeight", maxHeight);
		material.SetInt("chunkWidth", meshSettings.meshWorldSize);

		material.SetTexture("roadTexture", roadSettings.roadTexture.textureLayers[0].texture);
		material.SetFloat("roadTextureScale", roadSettings.roadTexture.textureLayers[0].textureScale);
	}

	public void DrawMapInEditor() {
		this.ApplyToMaterial(this.previewMaterial);
		this.Init();

		this.previewTextureObject = new Renderer();
		this.previewMeshFilter = new MeshFilter();

		this.previewMeshObject = new GameObject("Preview Object");
		this.previewMeshObject.AddComponent<HideOnPlay>();
		this.previewMeshRenderer  = this.previewMeshObject.AddComponent<MeshRenderer>();
		this.previewMeshFilter = this.previewMeshObject.AddComponent<MeshFilter>();
		this.previewMeshCollider = this.previewMeshObject.AddComponent<MeshCollider>();
		this.previewMeshRenderer.material = this.previewMaterial;

		ResetPreview();

		int width = this.meshSettings.numVerticesPerLine;
		int height = this.meshSettings.numVerticesPerLine;

		float[,] humidityMap = HeightMapGenerator.GenerateHeightMap(width,
                                                            height,
                                                            this.humidityMapSettings,
                                                            this,
                                                            centre,
															HeightMapGenerator.NormalizeMode.Global,
                                                            this.humidityMapSettings.seed);
		float[,] temperatureMap = HeightMapGenerator.GenerateHeightMap(width,
                                                               height,
                                                               this.temperatureMapSettings,
                                                               this,
                                                               centre,
															   HeightMapGenerator.NormalizeMode.Global,
                                                               this.temperatureMapSettings.seed);
		

		if (drawMode == DrawMode.NoiseMapTexture) {
			NoiseMapSettings noiseMapSettings= this.biomeSettings[noiseMapBiomeIndex].heightMapSettings;
			float[,] heightMap = HeightMapGenerator.GenerateHeightMap(width,
                                                           height,
                                                           noiseMapSettings,
                                                           this,
                                                           centre,
														   HeightMapGenerator.NormalizeMode.GlobalBiome,
                                                           noiseMapSettings.seed);
			DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
		} 
		else if (drawMode == DrawMode.FalloffMapTexture) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(width)));
		}
		else if (drawMode == DrawMode.BiomesMesh) {
            DrawBiomeMesh(width, height, humidityMap);
        }
        else if (drawMode == DrawMode.BiomesTexture) {
            DrawBiomes(width, height, humidityMap, temperatureMap);
        }
        else if (drawMode == DrawMode.HumidityMapTexture) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(humidityMap));
		}
		else if (drawMode == DrawMode.TemperatureMapTexture) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(temperatureMap));
		}
		else if (drawMode == DrawMode.SingleBiomeMesh) {
            DrawSingleBiome(width, height, humidityMap);
        }
    }

	private void ResetPreview() {
		GameObject prevPreviewObject = GameObject.Find("Preview Object");
		if (prevPreviewObject){
			DestroyImmediate(prevPreviewObject.gameObject);
		}
	}

    private void DrawSingleBiome(int width, int height, float[,] humidityMap)
    {
		BiomeSettings[] oldBiomes = new BiomeSettings[this.biomeSettings.Count];
		float oldTransitionDistance = this.transitionDistance;

		try {
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

			DrawBiomeMesh(width, height, humidityMap);

		} finally {
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

    private void DrawBiomeMesh(int width, int height, float[,] humidityMap)
    {
		#if (PROFILE && UNITY_EDITOR)
		float startTime = 0f;
		if (terrainSettings.IsMainThread()) {
        	startTime = Time.realtimeSinceStartup;
		}
        #endif
		ChunkData chunkData = ChunkDataGenerator.GenerateChunkData(this, centre, null);

		MeshData meshData = MeshGenerator.GenerateTerrainMesh(chunkData.biomeData.heightNoiseMap, this.meshSettings, editorPreviewLOD);
        DrawMesh(meshData);

		TerrainChunk.UpdateMaterial(chunkData, this, new MaterialPropertyBlock(), previewMeshObject.GetComponents<MeshRenderer>()[0]);

		for (int i = 0; i < chunkData.objects.Count; i++) {
			chunkData.objects[i].Spawn(this.previewMeshObject.transform);
		}

		#if (PROFILE && UNITY_EDITOR)
		if (terrainSettings.IsMainThread()) {
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

	public void DrawTexture(Texture2D texture) {
		this.previewTextureObject.sharedMaterial.mainTexture = texture;
		this.previewTextureObject.transform.localScale = new Vector3(-96, 1, 96);
	}

	public void DrawMesh(MeshData meshData) {
		this.previewMeshFilter.sharedMesh = meshData.CreateMesh();
	}

	public float minHeight {
		get {
			float minHeight = float.MaxValue;
			for (int i = 0; i< biomeSettings.Count; i++) {
				if (biomeSettings[i].heightMapSettings.minHeight < minHeight) {
					minHeight = biomeSettings[i].heightMapSettings.minHeight;
				}
			}
			return minHeight;
		}
	}

	public float maxHeight {
		get {
			float maxHeight = float.MinValue;
			for (int i = 0; i< biomeSettings.Count; i++) {
				if (biomeSettings[i].heightMapSettings.maxHeight > maxHeight) {
					maxHeight = biomeSettings[i].heightMapSettings.maxHeight;
				}
			}
			return maxHeight;
		}
	}

	public bool IsMainThread() {
		return this.mainThread.Equals(System.Threading.Thread.CurrentThread);
	}

	#if UNITY_EDITOR

	public void OnValidate() {
		// TODO ensure no overlapping biome values
		humidityMapSettings.OnValidate();
		temperatureMapSettings.OnValidate();
		erosionSettings.OnValidate();
		meshSettings.OnValidate();

		for (int i = 0; i < biomeSettings.Count; i++) {
			if (biomeSettings[i] != null) {
				biomeSettings[i].OnValidate();
			}
		}
	}

	#endif
}