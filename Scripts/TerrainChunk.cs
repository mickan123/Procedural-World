using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk {

	const float colliderGenerationDistanceThreshold = 5;

	public event System.Action<TerrainChunk, bool> onVisibilityChanged;
	public Vector2 coord;

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

	BiomeData biomeData;
	NoiseMap heightMap;
	bool heightMapReceived;
	int previousLODIndex = -1;
	bool hasSetCollider;
	float maxViewDst;

	MeshSettings meshSettings;
	BiomeSettings biomeSettings;
	Transform viewer;

	public TerrainChunk(Vector2 coord, 
						BiomeSettings biomeSettings,
						MeshSettings meshSettings, 
						LODInfo[] detailLevels, 
						int colliderLODIndex, 
						Transform parent, 
						Material material,
						Transform viewer) {
		this.coord = coord;
		this.detailLevels = detailLevels;
		this.colliderLODIndex = colliderLODIndex;
		this.biomeSettings = biomeSettings;
		this.meshSettings = meshSettings;
		this.viewer = viewer;

		sampleCentre = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
		Vector2 position = coord * meshSettings.meshWorldSize; 
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

	public void Load() {
		ThreadedDataRequester.RequestData(() => BiomeNoiseMapGenerator.GenerateBiomeNoiseMaps(meshSettings.numVerticesPerLine, 
																							  meshSettings.numVerticesPerLine, 
																							  biomeSettings,
																							  sampleCentre), 
											OnBiomeMapReceived);
	}

	void OnBiomeMapReceived(object biomeDataObject) {
		this.biomeData = (BiomeData)biomeDataObject;

		// int width = this.biomeData.biomeInfo.biomeMap.GetLength(0);
		// for (int x = 0; x < width; x ++) {
		// 	for (int y = 0; y < width; y ++) {				 
		// 		this.biomeData.biomeInfo.mainBiomeStrength[x, y] = (float)this.biomeData.biomeInfo.nearestBiomeMap[x, y]*100f;
		// 		this.biomeData.biomeInfo.distToNearestBiome[x, y] *= 100f;
		// 		this.biomeData.temperatureNoiseMap.values[x, y] *= 500f;
		// 		this.biomeData.humidityNoiseMap.values[x, y] *= 500f;
		// 	}
		// }
		// this.heightMap = new NoiseMap(biomeData.biomeInfo.distToNearestBiome, 0f, 1f);

		this.heightMap = this.biomeData.heightNoiseMap;

		heightMapReceived = true;
		
		UpdateMaterial();
		UpdateTerrainChunk();
	}

    void UpdateMaterial()
    {	
		BiomeInfo info = this.biomeData.biomeInfo;		
		int width = info.biomeMap.GetLength(0);

		float numBiomes = this.biomeSettings.biomes.Length;
		Texture2D biomeMapTex = new Texture2D(width, width, TextureFormat.RGB24, false, false);
		for (int x = 0; x < width; x ++) {
			for (int y = 0; y < width; y ++) {				 
				float biome = (float)info.biomeMap[x, y];
				float nearestBiome = (float)info.nearestBiomeMap[x, y];
				float mainBiomeStrength = (float)info.mainBiomeStrength[x, y];
				biomeMapTex.SetPixel(x, y, new Color(biome, nearestBiome, mainBiomeStrength, 0f));
			}
		}
		biomeMapTex.Apply();

		byte[] _bytes = biomeMapTex.EncodeToPNG();
        System.IO.File.WriteAllBytes("./test.png", _bytes);

		Vector2 position = coord * meshSettings.meshWorldSize; 
		matBlock.SetVector("centre", new Vector4(position.x, 0, position.y, 0));
		matBlock.SetTexture("biomeMapTex", biomeMapTex);
		meshRenderer.SetPropertyBlock(matBlock);
    }

    Vector2 viewerPosition {
		get {
			return new Vector2(viewer.position.x, viewer.position.z);
		}
	}
	
	public void UpdateTerrainChunk() {
		if (heightMapReceived) {
			float viewerDstFromNearestEdge = Mathf.Sqrt (bounds.SqrDistance (viewerPosition));

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
						lodMesh.RequestMesh (heightMap, meshSettings);
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
			float sqrDstFromViewerToEdge = bounds.SqrDistance (viewerPosition);

			if (sqrDstFromViewerToEdge < detailLevels [colliderLODIndex].sqrVisibleDstThreshold) {
				if (!lodMeshes [colliderLODIndex].hasRequestedMesh) {
					lodMeshes [colliderLODIndex].RequestMesh (heightMap, meshSettings);
				}
			}

			if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
				if (lodMeshes [colliderLODIndex].hasMesh) {
					meshCollider.sharedMesh = lodMeshes [colliderLODIndex].mesh;
					hasSetCollider = true;
				}
			}
		}
	}

	public void SetVisible(bool visible) {
		meshObject.SetActive (visible);
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

	public void RequestMesh(NoiseMap heightMap, MeshSettings meshSettings) {
		hasRequestedMesh = true;
		ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataReceived);
	}

}
