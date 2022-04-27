using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager
{
    private TerrainSettings terrainSettings;
    private Transform viewer;
    private LODInfo[] detailLevels;
    private int colliderLODIndex;
    private Transform parent;
    private Material mapMaterial;

    private const float viewerMoveThresholdForChunkUpdate = 5f;
    private const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    private int maxChunkViewDist;

    private Vector2 viewerPosition;
    private Vector2 viewerPositionOld;

    private Dictionary<ChunkCoord, TerrainChunk> terrainChunkDictionary = new Dictionary<ChunkCoord, TerrainChunk>();
    private List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    public bool destroyed = false;

    public WorldManager(LODInfo[] detailLevels, int colliderLODIndex, TerrainSettings terrainSettings, Transform viewer, Transform parent, Material mapMaterial)
    {
        this.detailLevels = detailLevels;
        this.colliderLODIndex = colliderLODIndex;
        this.terrainSettings = terrainSettings;
        this.viewer = viewer;
        this.parent = parent;
        this.mapMaterial = mapMaterial;

        maxChunkViewDist = detailLevels[detailLevels.Length - 1].chunkDistanceThreshold;
    }

    public void Update()
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
            this.parent,
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