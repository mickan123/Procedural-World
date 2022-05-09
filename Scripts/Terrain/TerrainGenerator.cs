using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public Transform viewer;
    public Material mapMaterial;

    public TerrainSettings terrainSettings;

    private const float viewerMoveThresholdForChunkUpdate = 5f;
    private const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    private int maxChunkViewDist = 1;

    private Vector2 viewerPosition;
    private Vector2 viewerPositionOld;

    private Dictionary<ChunkCoord, TerrainChunk> terrainChunkDictionary = new Dictionary<ChunkCoord, TerrainChunk>();
    private List<TerrainChunk> loadedTerrainChunks = new List<TerrainChunk>();

    void Start()
    {
        gameObject.AddComponent<ThreadedDataRequester>();
        terrainSettings.ApplyToMaterial(mapMaterial);
        terrainSettings.Init();
        UpdateVisibleChunks();
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        
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
                if (!spawner.isDetail)
                {
                    spawner.parent = chunk.chunkObject.transform;
                    StartCoroutine(spawner.SpawnMeshObjects(chunk));
                }
            }
        }
    }

    public void UpdateVisibleChunks()
    {
        HashSet<ChunkCoord> alreadyUpdatedChunkCoords = new HashSet<ChunkCoord>();
        for (int i = loadedTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoords.Add(loadedTerrainChunks[i].coord);
        }

        float chunkWidth = terrainSettings.width;

        int currentChunkCoordX = Mathf.FloorToInt(viewerPosition.x / chunkWidth);
        int currentChunkCoordY = Mathf.FloorToInt(viewerPosition.y / chunkWidth);

        for (int yOffset = -maxChunkViewDist; yOffset <= maxChunkViewDist; yOffset++)
        {
            for (int xOffset = -maxChunkViewDist; xOffset <= maxChunkViewDist; xOffset++)
            {
                ChunkCoord viewedChunkCoord = new ChunkCoord(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord) && !terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    GenerateChunk(viewedChunkCoord);
                }
            }
        }
    }

    public List<TerrainChunk> GetTerrainChunksReadyToSpawnObjects()
    {
        List<TerrainChunk> chunks = new List<TerrainChunk>();
        foreach (TerrainChunk chunk in loadedTerrainChunks)
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
            this.transform,
            mapMaterial
        );
        terrainChunkDictionary.Add(coord, newChunk);
        newChunk.onChunkLoaded += OnTerrainChunkLoaded;
        newChunk.Load();
    }

    void OnTerrainChunkLoaded(TerrainChunk chunk)
    {
        loadedTerrainChunks.Add(chunk);
    }


#if UNITY_EDITOR
    public void OnValidate()
    {

    }
#endif
}