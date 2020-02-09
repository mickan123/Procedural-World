using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapPreview : MonoBehaviour {

	public Renderer textureRender;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

	public enum DrawMode { NoiseMap, MeshNoBiome, BiomeMesh, FalloffMap, Biomes, HumidityMap, TemperatureMap };
	public DrawMode drawMode;

	public MeshSettings meshSettings;
	public WorldSettings worldSettings;
	public NoiseMapSettings heightMapSettings;
	
	public BiomeTextureData textureData;

	public Material terrainMaterial;

	[Range(0, MeshSettings.numSupportedLODs - 1)]
	public int EditorPreviewLOD;
	
	public bool autoUpdate;

	public int seed;

	public void DrawTexture(Texture2D texture) {
		textureRender.sharedMaterial.mainTexture = texture;
		textureRender.transform.localScale = new Vector3(-96, 1, 96);
	}

	public void DrawMesh(MeshData meshData) {
		meshFilter.sharedMesh = meshData.CreateMesh();
	}

	public void DrawMapInEditor() {
		worldSettings.ApplyToMaterial(terrainMaterial);
		worldSettings.Init();
		worldSettings.seed = this.seed;

		int width = meshSettings.numVerticesPerLine;
		int height = meshSettings.numVerticesPerLine;

		NoiseMap humidityMap = NoiseMapGenerator.GenerateNoiseMap(width,
                                                            height,
                                                            worldSettings.humidityMapSettings,
                                                            worldSettings,
                                                            Vector2.zero,
															NoiseMapGenerator.NormalizeMode.Global,
                                                            worldSettings.humidityMapSettings.seed);
		NoiseMap temperatureMap = NoiseMapGenerator.GenerateNoiseMap(width,
                                                               height,
                                                               worldSettings.temperatureMapSettings,
                                                               worldSettings,
                                                               Vector2.zero,
															   NoiseMapGenerator.NormalizeMode.Global,
                                                               worldSettings.temperatureMapSettings.seed);
		
		if (drawMode == DrawMode.NoiseMap) {
			NoiseMap heightMap = NoiseMapGenerator.GenerateNoiseMap(width,
                                                           height,
                                                           heightMapSettings,
                                                           worldSettings,
                                                           Vector2.zero,
														   NoiseMapGenerator.NormalizeMode.GlobalBiome,
                                                           heightMapSettings.seed);
			DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
		} 
		else if (drawMode == DrawMode.MeshNoBiome) {
			NoiseMap heightMap = NoiseMapGenerator.GenerateNoiseMap(width,
                                                           height,
                                                           heightMapSettings,
                                                           worldSettings,
                                                           Vector2.zero,
														   NoiseMapGenerator.NormalizeMode.GlobalBiome,
                                                           heightMapSettings.seed);
			DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, EditorPreviewLOD));
		}
		else if (drawMode == DrawMode.FalloffMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(new NoiseMap(FalloffGenerator.GenerateFalloffMap(width), 0, 1)));
		}
		else if (drawMode == DrawMode.BiomeMesh) {
            DrawBiomeMesh(width, height, humidityMap);
        }
        else if (drawMode == DrawMode.Biomes) {
            DrawBiomes(width, height, humidityMap, temperatureMap);
        }
        else if (drawMode == DrawMode.HumidityMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(humidityMap));
		}
		else if (drawMode == DrawMode.TemperatureMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(temperatureMap));
		}
	}

    private void DrawBiomeMesh(int width, int height, NoiseMap humidityMap)
    {
        BiomeInfo biomeInfo = NoiseMapGenerator.GenerateBiomeInfo(width,
																  height,
																  humidityMap,
																  humidityMap,
																  worldSettings);
        NoiseMap heightMap = NoiseMapGenerator.GenerateBiomeNoiseMap(width,
                                                                     height,
                                                                     worldSettings,
                                                                     humidityMap,
                                                                     humidityMap,
                                                                     Vector2.zero,
                                                                     biomeInfo);																	 
        DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, EditorPreviewLOD));
    }

    private void DrawBiomes(int width, int height, NoiseMap humidityMap, NoiseMap temperatureMap)
    {
        BiomeInfo biomeInfo = NoiseMapGenerator.GenerateBiomeInfo(width, height, humidityMap, temperatureMap, worldSettings);

        int numBiomes = worldSettings.biomes.Length;
        float[,] biomeTextureMap = new float[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                biomeTextureMap[i, j] = (float)biomeInfo.biomeMap[i, j] / (float)(numBiomes - 1);
            }
        }

        DrawTexture(TextureGenerator.TextureFromHeightMap(new NoiseMap(biomeTextureMap, 0, 1)));
    }

    void OnValuesUpdated() {
		if (!Application.isPlaying) {
			DrawMapInEditor();
		}
	}

	void OnTextureValuesUpdated() {
		worldSettings.ApplyToMaterial(terrainMaterial);
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
		if (worldSettings != null) {
			worldSettings.OnValuesUpdated -= OnValuesUpdated;
			worldSettings.OnValuesUpdated += OnValuesUpdated;

			worldSettings.UnsubscribeChildren();
			worldSettings.SubscribeChildren();
		}
	}
}
