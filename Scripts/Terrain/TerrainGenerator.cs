using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MyBox;

public class TerrainGenerator : MonoBehaviour {
	[Separator("Map Settings", true)]
	public Transform viewer;
	public Material mapMaterial;


	[Separator("Level of Detail Settings", true)]
	public LODInfo[] detailLevels;
	public int colliderLODIndex;

	[Separator("Terrain Generation Settings", true)]
	public TerrainSettings terrainSettings;
	

	private WorldManager generator;

	void Start() {
		terrainSettings.ApplyToMaterial(mapMaterial);
		terrainSettings.Init();

		generator = new WorldManager(detailLevels, colliderLODIndex, terrainSettings, viewer, this.transform, mapMaterial);
		generator.UpdateVisibleChunks();
	}

	void Update() {
		generator.Update();
	}

	void OnDestroy() {
		generator.destroyed = true;
	}
}