using System.Collections;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;

public struct ObjectPositionData
{    
    public ObjectPositions positions;
    public float[] heightMap;
    public int width;

    public float minWidth;
    public float maxWidth;
    public float minHeight;
    public float maxHeight;
    
    public ObjectPositionData(ObjectPositions positions, float[] heightMap, int width)
    {
        this.positions = positions;
        this.heightMap = heightMap;
        this.width = width;

        this.minHeight = 1f;
        this.maxHeight = 1f;
        this.minWidth = 1f;
        this.maxWidth = 1f;
    }
}

public struct ObjectPositions
{
    public float[] xCoords;
    public float[] yCoords;
    public float[] zCoords;

    public Quaternion[] rotations;
    public Vector3[] scales;
    public bool[] filtered;

    public ObjectPositions(float[] xCoords, float[] yCoords, float[] zCoords, Vector3[] scales, Quaternion[] rotations)
    {
        this.xCoords = xCoords;
        this.yCoords = yCoords;
        this.zCoords = zCoords;
        this.rotations = rotations;
        this.scales = scales;
        this.filtered = new bool[this.xCoords.Length];
    }

    public ObjectPositions(float[] xCoords, float[] yCoords, float[] zCoords)
    {
        int numCoords = xCoords.Length;

        this.xCoords = xCoords;
        this.yCoords = yCoords;
        this.zCoords = zCoords;
        this.scales = new Vector3[numCoords];
        this.rotations = new Quaternion[numCoords];

        Vector3 defaultScale = new Vector3(1f, 1f, 1f);
        Quaternion defaultRotation = Quaternion.identity;
        
        for (int i = 0; i < numCoords; i++)
        {
            this.scales[i] = defaultScale;
            this.rotations[i] = defaultRotation;
        }
        this.filtered = new bool[numCoords];
    }
    
    public int Length {
        get {
            if (this.xCoords == null) {
                return 0;
            }
            return this.xCoords.Length;
        } 
    }
}

public class ObjectSpawner
{
    private static readonly Vector3[] defaultNormals = { 
        new Vector3(0f, 0f, -1f),
        new Vector3(-.866f, 0f, -0.5f),
        new Vector3(.866f, 0f, -0.5f),
        new Vector3(0f, 0f, 1f),
        new Vector3(.866f, 0f, 0.5f),
        new Vector3(-.866f, 0f, 0.5f),
    };

    private static readonly Vector2[] defaultUvs = { 
        new Vector2(0f, 1f),
        new Vector2(1f, 1f),
        new Vector2(1f, 0f),
        new Vector2(0f, 0f),
    };

    public bool isDetail;

    // Detail only settings
    public DetailPrototype detailPrototype;
    public int[,] detailDensity;

    // Mesh object only settings
    public GameObject[] terrainObjects;

    // Common settings
    public ObjectPositions positions;

    // Internal vars
    private GameObject[] spawnedObjects;
    private System.Random prng;
    private bool hide = false;
    private bool staticBatch = false;
    private bool generateCollider = false;

    public Transform parent;

    public ObjectSpawner(
        GameObject[] terrainObjects,
        ObjectPositions positions,
        System.Random prng,
        bool staticBatch,
        bool generateCollider,
        bool hide
    )
    {
        this.isDetail = false;
        if (terrainObjects == null)
        {
            this.terrainObjects = new GameObject[0];
        }
        else
        {
            this.terrainObjects = terrainObjects;
        }
        this.positions = positions;
        this.prng = prng;
        this.spawnedObjects = new GameObject[this.positions.Length];
        this.staticBatch = staticBatch;
        this.generateCollider = generateCollider;
        this.hide = hide;
    }

    // Constructor for detail
    public ObjectSpawner(
        DetailPrototype detailPrototype,
        int[,] detailDensity,
        System.Random prng,
        bool hide
    )
    {
        this.detailPrototype = detailPrototype;
        this.detailDensity = detailDensity;
        this.isDetail = true;
        this.prng = prng;
        this.hide = hide;

        // Set detail density to zeros if hiding
        if (this.hide)
        {
            this.detailDensity = new int[this.detailDensity.GetLength(0), this.detailDensity.GetLength(1)];
        }
    }

    public void Spawn(Transform parent, TerrainChunk chunk)
    {
        this.parent = parent;
        if (!this.isDetail)
        {
            IEnumerator iterator = this.SpawnMeshObjects(chunk);
            while (iterator.MoveNext()) {}
        }
    }


    public IEnumerator SpawnMeshObjects(TerrainChunk chunk)
    {
        int length = positions.Length;
        for (int i = 0; i < length; i++)
        {
            float rand = Common.NextFloat(prng, 0, terrainObjects.Length);
            int objIndex = (int)rand;
            GameObject obj = UnityEngine.Object.Instantiate(terrainObjects[objIndex].gameObject);
            obj.transform.parent = parent;

            float scale = chunk.terrainSettings.scale; // Scale in context of world position rather than size of object
            float y = chunk.terrain.SampleHeight(new Vector3(positions.zCoords[i] * scale, 0f, positions.xCoords[i] * scale));
            obj.transform.localPosition = new Vector3(positions.zCoords[i] * scale, y, positions.xCoords[i] * scale);

            
            obj.transform.rotation = positions.rotations[i];
            obj.transform.localScale = positions.scales[i];
            obj.SetActive(!hide);
            if (this.generateCollider) 
            {
                obj.AddComponent<MeshCollider>();
            }
            spawnedObjects[i] = obj;
            yield return null;
        }
        if (spawnedObjects.Length > 0 && staticBatch)
        {
            StaticBatchingUtility.Combine(spawnedObjects, spawnedObjects[0]);
        }
    }
}

