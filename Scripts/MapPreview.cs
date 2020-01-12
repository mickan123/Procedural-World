using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapPreview : MonoBehaviour {

	public Renderer textureRender;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

	public enum DrawMode { NoiseMap, MeshNoBiome, MeshBiome, FalloffMap, Biomes, HumidityMap, TemperatureMap, NearestBiome };
	public DrawMode drawMode;

	public int seed;

	public MeshSettings meshSettings;
	public BiomeSettings biomeSettings;
	public NoiseMapSettings heightMapSettings;
	
	public TextureData textureData;

	public Material terrainMaterial;

	[Range(0, MeshSettings.numSupportedLODs - 1)]
	public int EditorPreviewLOD;
	
	public bool autoUpdate;

	public void DrawTexture(Texture2D texture) {
		textureRender.sharedMaterial.mainTexture = texture;
		textureRender.transform.localScale = new Vector3(-96, 1, 96);
		
		//renderer.gameObject.SetActive(true);
		//meshFilter.gameObject.SetActive(false);
	}

	public void DrawMesh(MeshData meshData) {
		meshFilter.sharedMesh = meshData.CreateMesh();

		//textureRender.gameObject.SetActive(false);
		//meshFilter.gameObject.SetActive(true);
	}

	public void DrawMapInEditor() {
		textureData.ApplyToMaterial(terrainMaterial);
		textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

		int width = meshSettings.numVerticesPerLine;
		int height = meshSettings.numVerticesPerLine;

		NoiseMap humidityMap = NoiseMapGenerator.GenerateNoiseMap(width, height, biomeSettings.humidityMapSettings, Vector2.zero, seed);
		NoiseMap temperatureMap = NoiseMapGenerator.GenerateNoiseMap(width, height, biomeSettings.temperatureMapSettings, Vector2.zero, seed);
		
		if (drawMode == DrawMode.NoiseMap) {
			NoiseMap heightMap = NoiseMapGenerator.GenerateNoiseMap(width, height, heightMapSettings, Vector2.zero, seed);
			DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
		} 
		else if (drawMode == DrawMode.MeshNoBiome) {
			NoiseMap heightMap = NoiseMapGenerator.GenerateNoiseMap(width, height, heightMapSettings, Vector2.zero, seed);
			DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, EditorPreviewLOD));
		}
		else if (drawMode == DrawMode.FalloffMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(new NoiseMap(FalloffGenerator.GenerateFalloffMap(width), 0, 1)));
		}
		else if (drawMode == DrawMode.MeshBiome) {
			
			NoiseMap heightMap = NoiseMapGenerator.GenerateBiomeNoiseMap(width, 
																		 height, 
																		 biomeSettings,
																		 humidityMap, 
																		 temperatureMap,
																		 Vector2.zero,
																		 seed);
			DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, EditorPreviewLOD));
		}
		else if (drawMode == DrawMode.Biomes) {
			int[,] biomeMap = NoiseMapGenerator.GenerateBiomeMap(width, height, humidityMap, temperatureMap, biomeSettings);
			NearestBiomeInfo nearestBiomeInfo = NoiseMapGenerator.GenerateNearestBiomeInfo(width, height, biomeMap, biomeSettings);

			int numBiomes = biomeSettings.biomes.Length;
			float[,] biomeTextureMap = new float[width, height];
			float[,] nearestBiomeTextureMap = new float[width, height];
			for (int i = 0 ; i < width; i++) {
				for (int j = 0; j < height; j++) {
					biomeTextureMap[i, j] = (float)biomeMap[i, j] / (float)(numBiomes - 1);
					nearestBiomeTextureMap[i, j] = (float)nearestBiomeInfo.nearestBiomeMap[i, j] / (float)(numBiomes - 1);
				}
			}

			DrawTexture(TextureGenerator.TextureFromHeightMap(new NoiseMap(biomeTextureMap, 0, 1)));
		}
		else if (drawMode == DrawMode.HumidityMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(humidityMap));
		}
		else if (drawMode == DrawMode.TemperatureMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(temperatureMap));
		}

		else if (drawMode == DrawMode.NearestBiome) {
			int[,] biomeMap = NoiseMapGenerator.GenerateBiomeMap(width, height, humidityMap, temperatureMap, biomeSettings);
			NearestBiomeInfo nearestBiomeInfo = NoiseMapGenerator.GenerateNearestBiomeInfo(width, height, biomeMap, biomeSettings);

			int numBiomes = biomeSettings.biomes.Length;
			float[,] nearestBiomeTextureMap = new float[width, height];
			for (int i = 0 ; i < width; i++) {
				for (int j = 0; j < height; j++) {
					nearestBiomeTextureMap[i, j] = (float)nearestBiomeInfo.nearestBiomeMap[i, j] / (float)(numBiomes - 1);
				}
			}

			DrawTexture(TextureGenerator.TextureFromHeightMap(new NoiseMap(nearestBiomeTextureMap, 0, 1)));
		}
	}

	void OnValuesUpdated() {
		if (!Application.isPlaying) {
			DrawMapInEditor();
		}
	}

	void OnTextureValuesUpdated() {
		textureData.ApplyToMaterial(terrainMaterial);
	}

	void OnValidate() {
		if (meshSettings != null) {
			meshSettings.OnValuesUpdated -= OnValuesUpdated; // Ensure we don't subscribe multiple times
			meshSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if (heightMapSettings != null) {
			heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
			heightMapSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if (textureData != null) {
			textureData.OnValuesUpdated -= OnTextureValuesUpdated;
			textureData.OnValuesUpdated += OnTextureValuesUpdated;
		}
	}
}
