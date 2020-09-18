using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyBox;


[ExecuteInEditMode]
public class MapPreview : MonoBehaviour {

	#if UNITY_EDITOR
	
	[Separator("Renderer Objects", true)]
	public Renderer textureRender;
	public MeshFilter meshFilter;
	public Material terrainMaterial;

	public enum DrawMode { SingleBiomeMesh, BiomesMesh, NoiseMapTexture, FalloffMapTexture, BiomesTexture, HumidityMapTexture, TemperatureMapTexture };
	[Separator("Drawing Settings", true)]
	public int seed;
	[Range(0, MeshSettings.numSupportedLODs - 1)] public int EditorPreviewLOD;
	public DrawMode drawMode;
	[ConditionalField(nameof(drawMode), false, DrawMode.SingleBiomeMesh)]public int drawSingleBiomeIndex;
	[ConditionalField(nameof(drawMode), false, DrawMode.NoiseMapTexture)] public int noiseMapBiomeIndex;
	public Vector2 centre;
	
	[Separator("World Settings", true)]
	public bool autoUpdate;
	[DisplayInspector] public TerrainSettings terrainSettings;

	public void Start() {
		UpdatableData.mapUpdate -= OnValuesUpdated;
		UpdatableData.mapUpdate += OnValuesUpdated;
	}

	public void DrawTexture(Texture2D texture) {
		textureRender.sharedMaterial.mainTexture = texture;
		textureRender.transform.localScale = new Vector3(-96, 1, 96);
	}

	public void DrawMesh(MeshData meshData) {
		meshFilter.sharedMesh = meshData.CreateMesh();
	}

	public void DrawMapInEditor() {
		UpdatableData.mapUpdate -= OnValuesUpdated;
		UpdatableData.mapUpdate += OnValuesUpdated;

		terrainSettings.ApplyToMaterial(terrainMaterial);
		terrainSettings.Init();
		terrainSettings.seed = this.seed;


		ResetMapPreview();

		int width = terrainSettings.meshSettings.numVerticesPerLine;
		int height = terrainSettings.meshSettings.numVerticesPerLine;

		float[,] humidityMap = HeightMapGenerator.GenerateHeightMap(width,
                                                            height,
                                                            terrainSettings.humidityMapSettings,
                                                            terrainSettings,
                                                            centre,
															HeightMapGenerator.NormalizeMode.Global,
                                                            terrainSettings.humidityMapSettings.seed);
		float[,] temperatureMap = HeightMapGenerator.GenerateHeightMap(width,
                                                               height,
                                                               terrainSettings.temperatureMapSettings,
                                                               terrainSettings,
                                                               centre,
															   HeightMapGenerator.NormalizeMode.Global,
                                                               terrainSettings.temperatureMapSettings.seed);
		

		if (drawMode == DrawMode.NoiseMapTexture) {
			NoiseMapSettings noiseMapSettings= terrainSettings.biomeSettings[noiseMapBiomeIndex].heightMapSettings;
			float[,] heightMap = HeightMapGenerator.GenerateHeightMap(width,
                                                           height,
                                                           noiseMapSettings,
                                                           terrainSettings,
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

	private void ResetMapPreview() {
		// Cleanup previously spawned objects
		for (int i = 0; i < this.transform.childCount; i++) {
			Transform child = this.transform.GetChild(i);
			if (child.name != "Preview Texture" && child.name != "Preview Mesh") {
				DestroyImmediate(child.gameObject);
				i--;
			}
		}

		for (int i = 0; i < this.meshFilter.transform.childCount; i++) {
			Transform child = this.meshFilter.transform.GetChild(i);
			DestroyImmediate(child.gameObject);
			i--;
		}
	}

    private void DrawSingleBiome(int width, int height, float[,] humidityMap)
    {
		BiomeSettings[] oldBiomes = new BiomeSettings[terrainSettings.biomeSettings.Length];
		float oldTransitionDistance = terrainSettings.transitionDistance;

		try {
			for (int i = 0; i < terrainSettings.biomeSettings.Length; i++)
			{
				oldBiomes[i] = (BiomeSettings)(BiomeSettings.CreateInstance("BiomeSettings"));
				oldBiomes[i].startHumidity = terrainSettings.biomeSettings[i].startHumidity;
				oldBiomes[i].endHumidity = terrainSettings.biomeSettings[i].endHumidity;
				oldBiomes[i].startTemperature = terrainSettings.biomeSettings[i].startTemperature;
				oldBiomes[i].endTemperature = terrainSettings.biomeSettings[i].endTemperature;

				terrainSettings.biomeSettings[i].startHumidity = 0f;
				terrainSettings.biomeSettings[i].endHumidity = 0f;
				terrainSettings.biomeSettings[i].startTemperature = 0f;
				terrainSettings.biomeSettings[i].endTemperature = 0f;
			}

			terrainSettings.biomeSettings[drawSingleBiomeIndex].endHumidity = 1f;
			terrainSettings.biomeSettings[drawSingleBiomeIndex].endTemperature = 1f;
			terrainSettings.transitionDistance = 0f;
			terrainSettings.ApplyToMaterial(terrainMaterial);

			DrawBiomeMesh(width, height, humidityMap);

		} finally {
			// Reset settings
			for (int i = 0; i < terrainSettings.biomeSettings.Length; i++)
			{
				terrainSettings.biomeSettings[i].startHumidity = oldBiomes[i].startHumidity;
				terrainSettings.biomeSettings[i].endHumidity = oldBiomes[i].endHumidity;
				terrainSettings.biomeSettings[i].startTemperature = oldBiomes[i].startTemperature;
				terrainSettings.biomeSettings[i].endTemperature = oldBiomes[i].endTemperature;
			}
			terrainSettings.transitionDistance = oldTransitionDistance;
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
		ChunkData chunkData = ChunkDataGenerator.GenerateChunkData(terrainSettings, centre, null);

		MeshData meshData = MeshGenerator.GenerateTerrainMesh(chunkData.biomeData.heightNoiseMap, terrainSettings.meshSettings, EditorPreviewLOD);
        DrawMesh(meshData);

		TerrainChunk.UpdateMaterial(chunkData, terrainSettings, new MaterialPropertyBlock(), meshFilter.GetComponents<MeshRenderer>()[0]);

		for (int i = 0; i < chunkData.objects.Count; i++) {
			chunkData.objects[i].Spawn(this.transform);
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
        BiomeInfo biomeInfo = BiomeHeightMapGenerator.GenerateBiomeInfo(width, height, humidityMap, temperatureMap, terrainSettings);

        int numBiomes = terrainSettings.biomeSettings.Length;
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

    void OnValuesUpdated() {
		if (!Application.isPlaying) {
			DrawMapInEditor();
		}
	}
 
	#endif
}
