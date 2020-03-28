using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapPreview : MonoBehaviour {

	public Renderer textureRender;
	public MeshFilter meshFilter;

	public enum DrawMode { NoiseMap, MeshNoBiome, BiomesMesh, FalloffMap, Biomes, HumidityMap, TemperatureMap, SingleBiome };
	public DrawMode drawMode;
	
	public int singleBiome;  // If DrawMode is SingleBiome it will render this biome number

	public WorldSettings worldSettings;
	private NoiseMapSettings heightMapSettings;
	
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

		this.heightMapSettings = worldSettings.biomes[0].heightMapSettings;

		ResetMapPreview();

		int width = worldSettings.meshSettings.numVerticesPerLine;
		int height = worldSettings.meshSettings.numVerticesPerLine;

		HeightMap humidityMap = HeightMapGenerator.GenerateHeightMap(width,
                                                            height,
                                                            worldSettings.humidityMapSettings,
                                                            worldSettings,
                                                            Vector2.zero,
															HeightMapGenerator.NormalizeMode.Global,
                                                            worldSettings.humidityMapSettings.seed);
		HeightMap temperatureMap = HeightMapGenerator.GenerateHeightMap(width,
                                                               height,
                                                               worldSettings.temperatureMapSettings,
                                                               worldSettings,
                                                               Vector2.zero,
															   HeightMapGenerator.NormalizeMode.Global,
                                                               worldSettings.temperatureMapSettings.seed);
		

		if (drawMode == DrawMode.NoiseMap) {
			HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(width,
                                                           height,
                                                           heightMapSettings,
                                                           worldSettings,
                                                           Vector2.zero,
														   HeightMapGenerator.NormalizeMode.GlobalBiome,
                                                           heightMapSettings.seed);
			DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
		} 
		else if (drawMode == DrawMode.MeshNoBiome) {
			HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(width,
                                                           height,
                                                           heightMapSettings,
                                                           worldSettings,
                                                           Vector2.zero,
														   HeightMapGenerator.NormalizeMode.GlobalBiome,
                                                           heightMapSettings.seed);
			DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, worldSettings.meshSettings, EditorPreviewLOD));
		}
		else if (drawMode == DrawMode.FalloffMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(width))));
		}
		else if (drawMode == DrawMode.BiomesMesh) {
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
		else if (drawMode == DrawMode.SingleBiome) {
            DrawSingleBiome(width, height, humidityMap);
        }
    }

	private void ResetMapPreview() {
		// Cleanup previously spawned objects
		for (int i = 0; i < this.transform.childCount; i++) {
			Transform child = this.transform.GetChild(i);
			if (child.name != "Preview Texture" && child.name != "Preview Mesh") {
				DestroyImmediate(child.gameObject);
				i--;
			}
		}
	}

    private void DrawSingleBiome(int width, int height, HeightMap humidityMap)
    {
		BiomeSettings[] oldBiomes = new BiomeSettings[worldSettings.biomes.Length];
		float oldTransitionDistance = worldSettings.transitionDistance;

		try {
			for (int i = 0; i < worldSettings.biomes.Length; i++)
			{
				oldBiomes[i] = (BiomeSettings)(BiomeSettings.CreateInstance("BiomeSettings"));
				oldBiomes[i].startHumidity = worldSettings.biomes[i].startHumidity;
				oldBiomes[i].endHumidity = worldSettings.biomes[i].endHumidity;
				oldBiomes[i].startTemperature = worldSettings.biomes[i].startTemperature;
				oldBiomes[i].endTemperature = worldSettings.biomes[i].endTemperature;

				worldSettings.biomes[i].startHumidity = 0f;
				worldSettings.biomes[i].endHumidity = 0f;
				worldSettings.biomes[i].startTemperature = 0f;
				worldSettings.biomes[i].endTemperature = 0f;
			}

			worldSettings.biomes[singleBiome].endHumidity = 1f;
			worldSettings.biomes[singleBiome].endTemperature = 1f;
			worldSettings.transitionDistance = 0f;
			worldSettings.ApplyToMaterial(terrainMaterial);

			DrawBiomeMesh(width, height, humidityMap);

		} finally {
			// Reset settings
			for (int i = 0; i < worldSettings.biomes.Length; i++)
			{
				worldSettings.biomes[i].startHumidity = oldBiomes[i].startHumidity;
				worldSettings.biomes[i].endHumidity = oldBiomes[i].endHumidity;
				worldSettings.biomes[i].startTemperature = oldBiomes[i].startTemperature;
				worldSettings.biomes[i].endTemperature = oldBiomes[i].endTemperature;
			}
			worldSettings.transitionDistance = oldTransitionDistance;
		}
    }

    private void DrawBiomeMesh(int width, int height, HeightMap humidityMap)
    {

		BiomeData biomeData = BiomeHeightMapGenerator.GenerateBiomeNoiseMaps(width,
																			height,
																			worldSettings,
																			Vector2.zero);

		MeshData meshData = MeshGenerator.GenerateTerrainMesh(biomeData.heightNoiseMap.values, worldSettings.meshSettings, EditorPreviewLOD);
        DrawMesh(meshData);

		TerrainChunk.UpdateMaterial(biomeData.biomeInfo, worldSettings, Vector2.zero, new MaterialPropertyBlock(), meshFilter.GetComponents<MeshRenderer>()[0]);

		List<SpawnObject> terrainObjects = ObjectGenerator.GenerateBiomeObjects(biomeData.heightNoiseMap, 
																					biomeData.biomeInfo, 
																					worldSettings, 
																					Vector2.zero);

		for (int i = 0; i < terrainObjects.Count; i++) {
			terrainObjects[i].Spawn(this.transform);
		}
    }

    private void DrawBiomes(int width, int height, HeightMap humidityMap, HeightMap temperatureMap)
    {
        BiomeInfo biomeInfo = BiomeHeightMapGenerator.GenerateBiomeInfo(width, height, humidityMap, temperatureMap, worldSettings);

        int numBiomes = worldSettings.biomes.Length;
        float[,] biomeTextureMap = new float[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                biomeTextureMap[i, j] = (float)biomeInfo.biomeMap[i, j] / (float)(numBiomes - 1);
            }
        }

        DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(biomeTextureMap)));
    }

    void OnValuesUpdated() {
		if (!Application.isPlaying) {
			DrawMapInEditor();
		}
	}

	void OnValidate() {
		if (heightMapSettings != null) {
			heightMapSettings.UnsubscribeChanges(OnValuesUpdated);
			heightMapSettings.SubscribeChanges(OnValuesUpdated);
		}
		if (worldSettings != null) {
			worldSettings.UnsubscribeChanges(OnValuesUpdated);
			worldSettings.SubscribeChanges(OnValuesUpdated);
		}
	}
}
