using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapPreview : MonoBehaviour {

	public Renderer textureRender;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

	public enum DrawMode { NoiseMap, MeshNoBiome, BiomeMesh, FalloffMap, Biomes, HumidityMap, TemperatureMap, NearestBiome, DistanceToNearestBiome };
	public DrawMode drawMode;

	public MeshSettings meshSettings;
	public BiomeSettings biomeSettings;
	public NoiseMapSettings heightMapSettings;
	
	public BiomeTextureData textureData;

	public Material terrainMaterial;

	[Range(0, MeshSettings.numSupportedLODs - 1)]
	public int EditorPreviewLOD;
	
	public bool autoUpdate;

	public void DrawTexture(Texture2D texture) {
		textureRender.sharedMaterial.mainTexture = texture;
		textureRender.transform.localScale = new Vector3(-96, 1, 96);
	}

	public void DrawMesh(MeshData meshData) {
		meshFilter.sharedMesh = meshData.CreateMesh();
	}

	public void DrawMapInEditor() {
		biomeSettings.ApplyToMaterial(terrainMaterial);

		int width = meshSettings.numVerticesPerLine;
		int height = meshSettings.numVerticesPerLine;

		NoiseMap humidityMap = NoiseMapGenerator.GenerateNoiseMap(width,
                                                            height,
                                                            biomeSettings.humidityMapSettings,
                                                            biomeSettings,
                                                            Vector2.zero,
															NoiseMapGenerator.NormalizeMode.Global,
                                                            biomeSettings.humidityMapSettings.seed);
		NoiseMap temperatureMap = NoiseMapGenerator.GenerateNoiseMap(width,
                                                               height,
                                                               biomeSettings.temperatureMapSettings,
                                                               biomeSettings,
                                                               Vector2.zero,
															   NoiseMapGenerator.NormalizeMode.Global,
                                                               biomeSettings.temperatureMapSettings.seed);
		
		if (drawMode == DrawMode.NoiseMap) {
			NoiseMap heightMap = NoiseMapGenerator.GenerateNoiseMap(width,
                                                           height,
                                                           heightMapSettings,
                                                           biomeSettings,
                                                           Vector2.zero,
														   NoiseMapGenerator.NormalizeMode.GlobalBiome,
                                                           heightMapSettings.seed);
			DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
		} 
		else if (drawMode == DrawMode.MeshNoBiome) {
			NoiseMap heightMap = NoiseMapGenerator.GenerateNoiseMap(width,
                                                           height,
                                                           heightMapSettings,
                                                           biomeSettings,
                                                           Vector2.zero,
														   NoiseMapGenerator.NormalizeMode.GlobalBiome,
                                                           heightMapSettings.seed);
			DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, EditorPreviewLOD));
		}
		else if (drawMode == DrawMode.FalloffMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(new NoiseMap(FalloffGenerator.GenerateFalloffMap(width), 0, 1)));
		}
		else if (drawMode == DrawMode.BiomeMesh)
        {
            DrawBiomeMesh(width, height, humidityMap);
        }
        else if (drawMode == DrawMode.Biomes)
        {
            DrawBiomes(width, height, humidityMap, temperatureMap);
        }
        else if (drawMode == DrawMode.HumidityMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(humidityMap));
		}
		else if (drawMode == DrawMode.TemperatureMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(temperatureMap));
		}

		else if (drawMode == DrawMode.NearestBiome)
        {
            DrawNearestBiomes(width, height, humidityMap, temperatureMap);
        }
        else if (drawMode == DrawMode.DistanceToNearestBiome) {
			BiomeInfo biomeInfo = NoiseMapGenerator.GenerateBiomeInfo(width, height, humidityMap, temperatureMap, biomeSettings);
			DrawTexture(TextureGenerator.TextureFromHeightMap(new NoiseMap(biomeInfo.mainBiomeStrength, 0, 1)));
		}
	}

    private void DrawNearestBiomes(int width, int height, NoiseMap humidityMap, NoiseMap temperatureMap)
    {
        BiomeInfo biomeInfo = NoiseMapGenerator.GenerateBiomeInfo(width, height, humidityMap, temperatureMap, biomeSettings);

        int numBiomes = biomeSettings.biomes.Length;
        float[,] nearestBiomeTextureMap = new float[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                nearestBiomeTextureMap[i, j] = (float)biomeInfo.nearestBiomeMap[i, j] / (float)(numBiomes - 1);
            }
        }
        DrawTexture(TextureGenerator.TextureFromHeightMap(new NoiseMap(nearestBiomeTextureMap, 0, 1)));
    }

    private void DrawBiomeMesh(int width, int height, NoiseMap humidityMap)
    {
        BiomeInfo biomeInfo = NoiseMapGenerator.GenerateBiomeInfo(width,
																  height,
																  humidityMap,
																  humidityMap,
																  biomeSettings);
        NoiseMap heightMap = NoiseMapGenerator.GenerateBiomeNoiseMap(width,
                                                                     height,
                                                                     biomeSettings,
                                                                     humidityMap,
                                                                     humidityMap,
                                                                     Vector2.zero,
                                                                     biomeInfo);
        DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, EditorPreviewLOD));
    }

    private void DrawBiomes(int width, int height, NoiseMap humidityMap, NoiseMap temperatureMap)
    {
        BiomeInfo biomeInfo = NoiseMapGenerator.GenerateBiomeInfo(width, height, humidityMap, temperatureMap, biomeSettings);

        int numBiomes = biomeSettings.biomes.Length;
        float[,] biomeTextureMap = new float[width, height];
        float[,] nearestBiomeTextureMap = new float[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                biomeTextureMap[i, j] = (float)biomeInfo.biomeMap[i, j] / (float)(numBiomes - 1);
                nearestBiomeTextureMap[i, j] = (float)biomeInfo.nearestBiomeMap[i, j] / (float)(numBiomes - 1);
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
		biomeSettings.ApplyToMaterial(terrainMaterial);
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
		if (biomeSettings != null) {
			biomeSettings.OnValuesUpdated -= OnValuesUpdated;
			biomeSettings.OnValuesUpdated += OnValuesUpdated;
		}
	}
}
