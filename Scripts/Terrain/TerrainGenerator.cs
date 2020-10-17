using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour
{
    public Transform viewer;
    public Material mapMaterial;

    public LODInfo[] detailLevels;

    public int colliderLODIndex;

    public TerrainSettings terrainSettings;

    private WorldManager generator;

    void Start()
    {
        terrainSettings.ApplyToMaterial(mapMaterial);
        terrainSettings.Init();

        generator = new WorldManager(detailLevels, colliderLODIndex, terrainSettings, viewer, this.transform, mapMaterial);
        generator.UpdateVisibleChunks();
    }

    void Update()
    {
        generator.Update();
    }

    void OnDestroy()
    {
        generator.destroyed = true;
    }
}