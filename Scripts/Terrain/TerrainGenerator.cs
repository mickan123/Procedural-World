using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public Transform viewer;
    public Material mapMaterial;

    public LODInfo[] detailLevels;
    public int colliderLODIndex;

    public TerrainSettings terrainSettings;

    private const float viewerMoveThresholdForChunkUpdate = 5f;
    private const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    private int maxChunkViewDist;

    private Vector2 viewerPosition;
    private Vector2 viewerPositionOld;

    private Dictionary<ChunkCoord, TerrainChunk> terrainChunkDictionary = new Dictionary<ChunkCoord, TerrainChunk>();
    private List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    void Start()
    {
        terrainSettings.ApplyToMaterial(mapMaterial);
        terrainSettings.Init();

        maxChunkViewDist = detailLevels[detailLevels.Length - 1].chunkDistanceThreshold;

        UpdateVisibleChunks();
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if (viewerPosition != viewerPositionOld)
        {
            foreach (TerrainChunk chunk in visibleTerrainChunks)
            {
                chunk.UpdateCollisionMesh();
            }
        }

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }

        List<TerrainChunk> chunksReadyToSpawnObjects = GetTerrainChunksReadyToSpawnObjects();
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

    public void UpdateVisibleChunks()
    {
        HashSet<ChunkCoord> alreadyUpdatedChunkCoords = new HashSet<ChunkCoord>();
        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
            visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        float chunkWidth = terrainSettings.meshSettings.meshWorldSize;

        int currentChunkCoordX = Mathf.FloorToInt(viewerPosition.x / chunkWidth);
        int currentChunkCoordY = Mathf.FloorToInt(viewerPosition.y / chunkWidth);

        for (int yOffset = -maxChunkViewDist; yOffset <= maxChunkViewDist; yOffset++)
        {
            for (int xOffset = -maxChunkViewDist; xOffset <= maxChunkViewDist; xOffset++)
            {
                ChunkCoord viewedChunkCoord = new ChunkCoord(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    {
                        GenerateChunk(viewedChunkCoord);
                    }
                }
            }
        }
    }

    public List<TerrainChunk> GetTerrainChunksReadyToSpawnObjects()
    {
        List<TerrainChunk> chunks = new List<TerrainChunk>();
        foreach (TerrainChunk chunk in visibleTerrainChunks)
        {
            if (chunk.readyToSpawnObjects)
            {
                chunks.Add(chunk);
                chunk.readyToSpawnObjects = false;
            }
        }
        return chunks;
    }

    private void GenerateChunk(ChunkCoord coord)
    {
        TerrainChunk newChunk = new TerrainChunk(
            coord,
            this.terrainSettings,
            this.detailLevels,
            this.colliderLODIndex,
            this.transform,
            mapMaterial,
            viewer
        );
        terrainChunkDictionary.Add(coord, newChunk);
        newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
        newChunk.Load();
    }

    void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        if (isVisible)
        {
            visibleTerrainChunks.Add(chunk);
        }
        else
        {
            visibleTerrainChunks.Remove(chunk);
        }
    }
}

[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;

    [Range(1, 15)]
    public int chunkDistanceThreshold;
}