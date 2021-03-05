using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WorldManager
{
    LODInfo[] detailLevels;
    int colliderLODIndex;
    TerrainSettings terrainSettings;
    Transform viewer;
    Transform parent;
    Material mapMaterial;

    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    float meshWorldSize;
    int maxChunkViewDist;

    Vector2 viewerPosition;
    Vector2 viewerPositionOld;

    Dictionary<ChunkCoord, TerrainChunkData> terrainChunkDictionary = new Dictionary<ChunkCoord, TerrainChunkData>();
    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

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
        meshWorldSize = terrainSettings.meshSettings.meshWorldSize - 1;
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

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

        for (int yOffset = -maxChunkViewDist; yOffset <= maxChunkViewDist; yOffset++)
        {
            for (int xOffset = -maxChunkViewDist; xOffset <= maxChunkViewDist; xOffset++)
            {
                ChunkCoord viewedChunkCoord = new ChunkCoord(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDictionary[viewedChunkCoord].terrainChunk.UpdateTerrainChunk();
                    }
                    else
                    {
                        GenerateChunk(viewedChunkCoord);
                    }
                }
            }
        }
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
        terrainChunkDictionary.Add(coord, new TerrainChunkData(newChunk));
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

public struct TerrainChunkData
{
    public TerrainChunk terrainChunk;

    public bool doneStage1Erosion;

    public float[,] heightMap;

    public TerrainChunkData(TerrainChunk terrainChunk)
    {
        this.terrainChunk = terrainChunk;
        doneStage1Erosion = false;
        heightMap = null;
    }
}

[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;
    public int chunkDistanceThreshold;
}