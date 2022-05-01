using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public Transform viewer;
    public Material mapMaterial;

    public LODInfo[] lodLevels;
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
        gameObject.AddComponent<ThreadedDataRequester>();
        terrainSettings.ApplyToMaterial(mapMaterial);
        terrainSettings.Init();

        maxChunkViewDist = lodLevels[lodLevels.Length - 1].chunkDistanceThreshold;

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
                        StartCoroutine(spawner.SpawnBillboardDetailsMesh());
                    }
                    else if (spawner.detailMode == ObjectSpawner.DetailMode.Triangle || spawner.detailMode == ObjectSpawner.DetailMode.Circle)
                    {
                        StartCoroutine(spawner.SpawnDetailsMesh());
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
            this.lodLevels,
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

#if UNITY_EDITOR
    public void OnValidate()
    {
        int prevDist = -1;
        int prevLod = -1;
        for (int i = 0; i < lodLevels.Length; i++)
        {
            lodLevels[i].chunkDistanceThreshold = Mathf.Clamp(lodLevels[i].chunkDistanceThreshold, 1, 15); // Ensure it is always within 1-15 range
            lodLevels[i].chunkDistanceThreshold = Mathf.Max(prevDist, lodLevels[i].chunkDistanceThreshold); // Ensure cur dist threshold is always larger than prev
            lodLevels[i].lod = Mathf.Max(prevLod, lodLevels[i].lod); // Ensure cur Lod level is always larger than prev
            prevDist = lodLevels[i].chunkDistanceThreshold;
            prevLod = lodLevels[i].lod;
        }

        this.colliderLODIndex = Mathf.Clamp(this.colliderLODIndex, 0, lodLevels.Length - 1);
    }
#endif

}

[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;

    [Range(1, 15)]
    public int chunkDistanceThreshold;

}