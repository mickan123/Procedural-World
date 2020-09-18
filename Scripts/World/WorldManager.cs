using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WorldManager {
	LODInfo[] detailLevels;
	int colliderLODIndex;
	TerrainSettings terrainSettings;
	Transform viewer;
	Transform parent;
	Material mapMaterial;

	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

	float meshWorldSize;
	int chunksVisibleInViewDst;

	Vector2 viewerPosition;
	Vector2 viewerPositionOld;

	Dictionary<ChunkCoord, TerrainChunkData> terrainChunkDictionary = new Dictionary<ChunkCoord, TerrainChunkData>();
	List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

	public bool destroyed = false;

	public WorldManager(LODInfo[] detailLevels, int colliderLODIndex, TerrainSettings terrainSettings, Transform viewer, Transform parent, Material mapMaterial) {
		this.detailLevels = detailLevels;
		this.colliderLODIndex = colliderLODIndex;
		this.terrainSettings = terrainSettings;
		this.viewer = viewer;
		this.parent = parent;
		this.mapMaterial = mapMaterial;

		float maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
		meshWorldSize = terrainSettings.meshSettings.meshWorldSize - 1;
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);
	}

	public void Update() {
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);

		if (viewerPosition != viewerPositionOld) {
			foreach (TerrainChunk chunk in visibleTerrainChunks) {
				chunk.UpdateCollisionMesh();
			}
		}

		if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks();
		}
	}

	public void UpdateVisibleChunks() {
		HashSet<ChunkCoord> alreadyUpdatedChunkCoords = new HashSet<ChunkCoord> ();
		for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--) {
			alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
			visibleTerrainChunks[i].UpdateTerrainChunk();
		}
			
		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
				ChunkCoord viewedChunkCoord = new ChunkCoord(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
				if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord)) {
					if (terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
						terrainChunkDictionary[viewedChunkCoord].terrainChunk.UpdateTerrainChunk();
					} else {
						TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, 
																 this.terrainSettings,
																 this.detailLevels, 
																 this.colliderLODIndex, 
																 this.parent, 
																 mapMaterial,
																 viewer);
						terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunkData(newChunk));
						newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
						newChunk.Load(this);
					}
				}
			}
		}
	}

	void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible) {
		if (isVisible) {
			visibleTerrainChunks.Add(chunk);
		} else {
			visibleTerrainChunks.Remove(chunk);
		}
	}
	private static readonly int[,] neighBouroffsets = { { 1 , 0}, { 0 , 1}, { -1, 0}, { 0 , -1},
                                               			{ 1 , 1}, { 1 ,-1}, { -1, 1}, {-1 , -1} };

	public void DoneErosion(ChunkCoord chunkCoord, float[,] heightMap) {
		TerrainChunkData chunkData = terrainChunkDictionary[chunkCoord];
		chunkData.doneStage1Erosion = true;
		chunkData.heightMap = heightMap;
		terrainChunkDictionary[chunkCoord] = chunkData;
	}

	public void UpdateChunkBorder(ChunkCoord curChunkCoord, ChunkCoord adjacentChunkCoord, float[,] heightMap, float[,] mask) {
		while (!terrainChunkDictionary.ContainsKey(adjacentChunkCoord) 
			|| !terrainChunkDictionary[adjacentChunkCoord].doneStage1Erosion) {
			System.Threading.Thread.Sleep(500);
		}	

		int padding = terrainSettings.erosionSettings.maxLifetime;
		int mapSize = heightMap.GetLength(0);
		float[,] adjacentHeightMap = terrainChunkDictionary[adjacentChunkCoord].heightMap;
		
		if (curChunkCoord.y < adjacentChunkCoord.y) {
			for (int i = 0; i < mapSize; i++) {
				for (int j = 0; j < 2 * padding; j++) {
					heightMap[i, mapSize - (2 * padding) + j] = adjacentHeightMap[i, j + 3]; // Fuck knows why its I have to add 3
					mask[i, mapSize - (2 * padding) + j] = 1;
				}
			}
		}
		else if (curChunkCoord.y > adjacentChunkCoord.y) {
			for (int i = 0; i < mapSize; i++) {
				for (int j = 0; j < 2 * padding; j++) {
					heightMap[i, j + 3] = adjacentHeightMap[i, mapSize - (2 * padding) + j];
					mask[i, j + 3] = 1;
				}
			}
		}
		if (curChunkCoord.x > adjacentChunkCoord.x) {
			for (int i = 0; i < 2 * padding; i++) {
				for (int j = 0; j < mapSize; j++) {
					heightMap[i + 3, j] = adjacentHeightMap[mapSize - (2 * padding) + i, j];
					mask[i + 3, j] = 1;
				}
			}
		}
		else if (curChunkCoord.x < adjacentChunkCoord.x) {
			for (int i = 0; i < 2 * padding; i++) {
				for (int j = 0; j < mapSize; j++) {
					heightMap[mapSize - (2 * padding) + i, j] = adjacentHeightMap[i + 3, j];
					mask[mapSize - (2 * padding) + i, j] = 1;
				}
			}
		}
	}
}

public struct ChunkCoord : IEquatable<ChunkCoord> {
	public int x;
	public int y;

	public ChunkCoord(int x, int y) {
		this.x = x;
		this.y = y;
	}

	public bool Equals(ChunkCoord other)
    {
        return Equals(other, this);
    }

	public override bool Equals(object obj) {
		if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

		var other = (ChunkCoord) obj;

        return this.x == other.x && this.y == other.y;
    }

	public override int GetHashCode()
    {
        var calculation = x + y;
        return calculation.GetHashCode();
    }

	public override string ToString() {
		return this.x + "," + this.y;
	}


	public static bool operator == (ChunkCoord coord1, ChunkCoord coord2) {
		return coord1.Equals(coord2);
	}

	public static bool operator != (ChunkCoord coord1, ChunkCoord coord2) {
		return !coord1.Equals(coord2);
	}
}


public struct TerrainChunkData {

	public TerrainChunk terrainChunk;
	public Dictionary<ChunkCoord, List<Drop>> drops;

	public bool doneStage1Erosion;

	public float[,] heightMap;

	public TerrainChunkData(TerrainChunk terrainChunk) {
		this.terrainChunk = terrainChunk;
		drops = new Dictionary<ChunkCoord, List<Drop>>();
		doneStage1Erosion = false;
		heightMap = null;
	}
}

[System.Serializable]
public struct LODInfo {
	[Range(0, MeshSettings.numSupportedLODs - 1)]
	public int lod;
	public float visibleDstThreshold;

	public float sqrVisibleDstThreshold {
		get {
			return visibleDstThreshold * visibleDstThreshold;
		}
	}
}