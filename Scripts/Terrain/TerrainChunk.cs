using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk {

	const float colliderGenerationDistanceThreshold = 200;

	public event System.Action<TerrainChunk, bool> onVisibilityChanged;
	public ChunkCoord coord;

	GameObject meshObject;
	Vector2 sampleCentre;
	Bounds bounds;

	MeshRenderer meshRenderer;
	MeshFilter meshFilter;
	MeshCollider meshCollider;
	MaterialPropertyBlock matBlock;

	LODInfo[] detailLevels;
	LODMesh[] lodMeshes;
	int colliderLODIndex;

	ChunkData chunkData;
	float[,] heightMap;
	bool heightMapReceived;
	int previousLODIndex = -1;
	bool hasSetCollider;
	float maxViewDst;

	MeshSettings meshSettings;
	TerrainSettings terrainSettings;
	Transform viewer;

	public TerrainChunk(ChunkCoord coord, 
						TerrainSettings terrainSettings,
						LODInfo[] detailLevels, 
						int colliderLODIndex, 
						Transform parent, 
						Material material,
						Transform viewer) {
		this.coord = coord;
		this.detailLevels = detailLevels;
		this.colliderLODIndex = colliderLODIndex;
		this.terrainSettings = terrainSettings;
		this.meshSettings = terrainSettings.meshSettings;
		this.viewer = viewer;

		sampleCentre = new Vector2(coord.x * meshSettings.meshWorldSize / meshSettings.meshScale, 
								   coord.y * meshSettings.meshWorldSize / meshSettings.meshScale);
		Vector2 position = new Vector2(coord.x * meshSettings.meshWorldSize, coord.y * meshSettings.meshWorldSize); 
		bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);
		
		meshObject = new GameObject("Terrain Chunk");
		meshRenderer = meshObject.AddComponent<MeshRenderer>();
		meshFilter = meshObject.AddComponent<MeshFilter>();
		meshCollider = meshObject.AddComponent<MeshCollider>();
		meshRenderer.material = material;
		matBlock = new MaterialPropertyBlock();

		meshObject.transform.position = new Vector3(position.x, 0, position.y);
		meshObject.transform.parent = parent;
		SetVisible(false);

		lodMeshes = new LODMesh[detailLevels.Length];
		for (int i = 0; i < detailLevels.Length; i++) {
			lodMeshes[i] = new LODMesh(detailLevels[i].lod);
			lodMeshes[i].updateCallback += UpdateTerrainChunk;
			if (i == colliderLODIndex) {
				lodMeshes[i].updateCallback += UpdateCollisionMesh;
			}
		}

		maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
	}

	public void Load(WorldManager worldGenerator) {
		ThreadedDataRequester.RequestData(() => ChunkDataGenerator.GenerateChunkData(terrainSettings, sampleCentre, worldGenerator), OnChunkDataReceived);											
	}

	void OnChunkDataReceived(object chunkData) {
		this.chunkData = (ChunkData)chunkData;

		this.heightMap = this.chunkData.biomeData.heightNoiseMap;
		
		heightMapReceived = true;
		
		UpdateMaterial(this.chunkData, terrainSettings, matBlock, meshRenderer);
		UpdateTerrainChunk();

		List<SpawnObject> spawnObjects = this.chunkData.objects;

		for (int i = 0; i < spawnObjects.Count; i++) {
			spawnObjects[i].Spawn(meshObject.transform);
		}
	}

	public static void UpdateMaterial(ChunkData chunkData, TerrainSettings terrainSettings, MaterialPropertyBlock matBlock, MeshRenderer renderer) {
		BiomeInfo info = chunkData.biomeData.biomeInfo;
		int width = info.biomeMap.GetLength(0);

		// Create texture to pass in biome maps and biome strengths
		int numBiomes = terrainSettings.biomes.Length;
		Texture2D biomeMapTex = new Texture2D(width, width, TextureFormat.RGBA32, false, false);
		
		int finalTexWidth = 256;
		int biomesPerTexture = 4;
		Texture2D[] biomeStrengthTextures = new Texture2D[terrainSettings.maxBiomeCount / biomesPerTexture + 1];
		for (int i = 0; i < terrainSettings.maxBiomeCount / biomesPerTexture + 1; i++) {
			biomeStrengthTextures[i] = new Texture2D(width, width, TextureFormat.RGBA32, false, false);
		}
		Texture2DArray biomeStrengthTexArray = new Texture2DArray(finalTexWidth,
																finalTexWidth,
																terrainSettings.maxBiomeCount,
																TextureFormat.RGBA32,
																false,
																false);

		biomeMapTex.filterMode = FilterMode.Trilinear; 
		biomeStrengthTexArray.filterMode = FilterMode.Point; // TODO: Should this be bilinear

		for (int x = 0; x < width; x ++) {
			for (int y = 0; y < width; y ++) {				 
				biomeMapTex.SetPixel(x, y, new Color(chunkData.road.roadStrengthMap[x, y], 0f, 0f, 0f));

				for (int w = 0; w < terrainSettings.maxBiomeCount; w += biomesPerTexture) {
					int texIndex = w / biomesPerTexture; 

					Color biomeStrengths = new Color((w < numBiomes) ? info.biomeStrengths[x, y, w] : 0f,
													(w + 1 < numBiomes) ? info.biomeStrengths[x, y, w + 1] : 0f,
													(w + 2 < numBiomes) ? info.biomeStrengths[x, y, w + 2] : 0f,
													(w + 3 < numBiomes) ? info.biomeStrengths[x, y, w + 3] : 0f);
					
					biomeStrengthTextures[texIndex].SetPixel(x, y, biomeStrengths);
				}
			}
		}
		
		for (int i = 0; i < biomeStrengthTextures.Length; i++) {
			TextureScale.Bilinear(biomeStrengthTextures[i], finalTexWidth, finalTexWidth);
			biomeStrengthTextures[i].Apply();
			biomeStrengthTexArray.SetPixels(biomeStrengthTextures[i].GetPixels(), i);
		}
		biomeStrengthTexArray.Apply();
		matBlock.SetTexture("biomeStrengthMap", biomeStrengthTexArray);

		biomeMapTex.Apply();
		matBlock.SetTexture("biomeMapTex", biomeMapTex);

		renderer.SetPropertyBlock(matBlock);
	}

	public static void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
	{
		byte[] _bytes =_texture.EncodeToPNG();
		System.IO.File.WriteAllBytes(_fullPath, _bytes);
		Debug.Log(_bytes.Length / 1024  + "Kb was saved as: " + _fullPath);
	}

    Vector2 viewerPosition {
		get {
			return new Vector2(viewer.position.x, viewer.position.z);
		}
	}
	
	public void UpdateTerrainChunk() {
		if (heightMapReceived) {
			float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

			bool wasVisible = IsVisible ();
			bool visible = viewerDstFromNearestEdge <= maxViewDst;

			if (visible) {
				int lodIndex = 0;

				for (int i = 0; i < detailLevels.Length - 1; i++) {
					if (viewerDstFromNearestEdge > detailLevels [i].visibleDstThreshold) {
						lodIndex = i + 1;
					} else {
						break;
					}
				}

				if (lodIndex != previousLODIndex) {
					LODMesh lodMesh = lodMeshes [lodIndex];
					if (lodMesh.hasMesh) {
						previousLODIndex = lodIndex;
						meshFilter.mesh = lodMesh.mesh;
					} else if (!lodMesh.hasRequestedMesh) {
						lodMesh.RequestMesh(heightMap, meshSettings);
					}
				}
			}

			if (wasVisible != visible) {
				SetVisible (visible);
				if (onVisibilityChanged != null) {
					onVisibilityChanged(this, visible);
				}
			}
		}
	}

	public void UpdateCollisionMesh() {
		if (!hasSetCollider) {
			float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

			if (sqrDstFromViewerToEdge < detailLevels [colliderLODIndex].sqrVisibleDstThreshold) {
				if (!lodMeshes [colliderLODIndex].hasRequestedMesh) {
					lodMeshes [colliderLODIndex].RequestMesh(heightMap, meshSettings);
				}
			}

			if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
				if (lodMeshes[colliderLODIndex].hasMesh) {
					meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
					hasSetCollider = true;
				}
			}
		}
	}

	public void SetVisible(bool visible) {
		meshObject.SetActive(visible);
	}

	public bool IsVisible() {
		return meshObject.activeSelf;
	}
}

class LODMesh {

	public Mesh mesh;
	public bool hasRequestedMesh;
	public bool hasMesh;
	int lod;
	public event System.Action updateCallback;

	public LODMesh(int lod) {
		this.lod = lod;
	}

	void OnMeshDataReceived(object meshDataObject) {
		mesh = ((MeshData)meshDataObject).CreateMesh();
		hasMesh = true;

		updateCallback();
	}

	public void RequestMesh(float[,] heightMap, MeshSettings meshSettings) {
		hasRequestedMesh = true;
		ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap, meshSettings, lod), OnMeshDataReceived);
	}

}
