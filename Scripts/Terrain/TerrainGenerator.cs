using System.Collections.Generic;
using UnityEngine;

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

        List<TerrainChunk> chunksReadyToSpawnObjects = generator.GetTerrainChunksReadyToSpawnObjects();
        foreach(TerrainChunk chunk in chunksReadyToSpawnObjects)
        {   

            List<ObjectSpawner> objectSpawners = chunk.chunkData.objects;
            
            foreach(ObjectSpawner spawner in objectSpawners)
            {
                spawner.parent = chunk.meshObject.transform;

                if (spawner.isDetail)
                {
                    if (spawner.detailMode == ObjectSpawner.DetailMode.GeometryShader)
                    {
                        spawner.GeometryShaderDetails();
                        break;
                    }

                    if (spawner.detailMode == ObjectSpawner.DetailMode.Billboard)
                    {
                        // meshes = this.GenerateBillboardDetailsMesh();
                    }
                    // else if (this.detailMode == DetailMode.Triangle)
                    // {
                    //     meshes = this.GenerateTriangleDetailsMesh();
                    // }
                    else if (spawner.detailMode == ObjectSpawner.DetailMode.Circle)
                    {
                        StartCoroutine(spawner.SpawnCircleDetailsMesh());
                    }
                }
                else
                {
                    StartCoroutine(spawner.SpawnMeshObjects());
                }
            }
        }
    }

    void OnDestroy()
    {
        generator.destroyed = true;
    }
}