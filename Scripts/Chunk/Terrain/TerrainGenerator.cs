using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour {

	public int colliderLODIndex;
	public LODInfo[] detailLevels;

	public WorldSettings worldSettings;
	public Transform viewer;
	public Material mapMaterial;

	WorldGenerator generator;


	void Start() {
		worldSettings.ApplyToMaterial(mapMaterial);
		worldSettings.Init();

		generator = new WorldGenerator(detailLevels, colliderLODIndex, worldSettings, viewer, this.transform, mapMaterial);
		generator.UpdateVisibleChunks();
	}

	void Update() {
		generator.Update();
	}

	void OnDestroy() {
		generator.destroyed = true;
	}
}