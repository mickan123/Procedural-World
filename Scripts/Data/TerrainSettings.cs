using System.Collections;
using UnityEngine;
using System.Threading;
using MyBox;

[CreateAssetMenu(), System.Serializable]
public class TerrainSettings : UpdatableData {

	// Custom editor toolbar tabs settings
	[HideInInspector] public int toolbarTop;
	[HideInInspector] public int toolbarBottom;
	[HideInInspector] public string currentTab;
	
	// Biome settings
	public NoiseMapSettings humidityMapSettings;
	public NoiseMapSettings temperatureMapSettings;
	[Range(0,1)] public float transitionDistance;
	public BiomeSettings[] biomeSettings;

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

		for (int i = 0; i < biomeSettings.Length; i++) {
			biomeSettings[i].heightMapSettings.seed = prng.Next(-100000, 100000);

			for (int j = 0; j < biomeSettings[i].terrainObjectSettings.Length; j++) {
				if (biomeSettings[i].terrainObjectSettings[j] != null) {
					biomeSettings[i].terrainObjectSettings[j].noiseMapSettings.seed = prng.Next(-100000, 100000);
				}
			}
		}
	}

	public void ApplyToMaterial(Material material) {

		float[] layerCounts = new float[biomeSettings.Length];

		Color[] baseColours = new Color[maxLayerCount * maxBiomeCount];
		float[] baseStartHeights = new float[maxLayerCount * maxBiomeCount];
		float[] baseBlends = new float[maxLayerCount * maxBiomeCount];
		float[] baseColourStrength = new float[maxLayerCount * maxBiomeCount];
		float[] baseTextureScales = new float[maxLayerCount * maxBiomeCount];
		Texture2DArray texturesArray = new Texture2DArray(textureSize, textureSize, maxLayerCount * maxBiomeCount, textureFormat, true);

		for (int i = 0; i < biomeSettings.Length; i++) {

			layerCounts[i] = biomeSettings[i].textureData.layers.Length;

			for (int j = 0; j < biomeSettings[i].textureData.layers.Length; j++) {

				TextureLayer curLayer = biomeSettings[i].textureData.layers[j];
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

		material.SetTexture("roadTexture", roadSettings.roadTexture.layers[0].texture);
		material.SetFloat("roadTextureScale", roadSettings.roadTexture.layers[0].textureScale);
	}

	public float minHeight {
		get {
			float minHeight = float.MaxValue;
			for (int i = 0; i< biomeSettings.Length; i++) {
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
			for (int i = 0; i< biomeSettings.Length; i++) {
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

	protected override void OnValidate() {
		// TODO ensure no overlapping biome values
		humidityMapSettings.ValidateValues();
		temperatureMapSettings.ValidateValues();
		erosionSettings.ValidateValues();
		meshSettings.ValidateValues();

		for (int i = 0; i < biomeSettings.Length; i++) {
			biomeSettings[i].ValidateValues();
		}
		base.OnValidate();
	}

	#endif
}