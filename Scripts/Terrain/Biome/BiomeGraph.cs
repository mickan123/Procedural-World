using UnityEngine;
using XNode;
using System;
using System.Collections.Generic;

[Serializable, CreateAssetMenu(menuName = "Procedural Generation Settings/BiomeGraph")]
public class BiomeGraph : NodeGraph
{
    public Dictionary<System.Threading.Thread, HeightMapGraphData> heightMapData;

    public bool initialized = false;

    private readonly object locker = new object();

    public float[] GetHeightMap(TerrainSettings terrainSettings, Vector2 sampleCentre, int width)
    {
        lock(locker)
        {
            this.heightMapData[System.Threading.Thread.CurrentThread] = new HeightMapGraphData(
                terrainSettings, sampleCentre, width
            );
        }
        
        foreach (Node node in this.nodes)
        {
            if (node is HeightMapOutputNode)
            {
                var typedNode = node as HeightMapOutputNode;
                float[] heightMap = typedNode.GetValue();

                lock(locker)
                {
                    heightMapData.Remove(System.Threading.Thread.CurrentThread);
                }
                
                return heightMap;
            }
        }
        lock(locker)
        {
            heightMapData.Remove(System.Threading.Thread.CurrentThread);
        }
        return null;
    }

    public bool ContainsKey(System.Threading.Thread key) 
    {
        if (initialized && this.heightMapData != null && this.heightMapData.ContainsKey(key))
        {
            return true;
        }
        return false;
    }

    public float[] GetHeightMap(
        BiomeInfo info,
        TerrainSettings terrainSettings,
        Vector2 sampleCentre,
        int biome,
        int width
    )
    {
        lock(locker)
        {
            this.heightMapData[System.Threading.Thread.CurrentThread] = new HeightMapGraphData(
                terrainSettings, sampleCentre, width, biome, info
            );
        }
        
        foreach (Node node in this.nodes)
        {
            if (node is HeightMapOutputNode)
            {
                var typedNode = node as HeightMapOutputNode;
                float[] heightMap = typedNode.GetValue();

                lock(locker)
                {
                    heightMapData.Remove(System.Threading.Thread.CurrentThread);
                }

                return heightMap;
            }
        }
        
        lock(locker)
        {
            heightMapData.Remove(System.Threading.Thread.CurrentThread);
        }

        return null;
    }

    public List<ObjectSpawner> GetObjectSpawners(
        TerrainSettings terrainSettings, 
        Vector2 sampleCentre, 
        BiomeInfo biomeInfo, 
        int biome, 
        float[] heightMap, 
        float[] roadStrengthMap
    )
    {
        lock(locker)
        {
            this.heightMapData[System.Threading.Thread.CurrentThread] = new HeightMapGraphData(
                terrainSettings, sampleCentre, biomeInfo, biomeInfo.width, biome, heightMap, roadStrengthMap
            );
        }
        
        List<ObjectSpawner> objectSpawners = new List<ObjectSpawner>();

        foreach (Node node in this.nodes)
        {
            if (node is ObjectsOutputNode)
            {
                ObjectsOutputNode typedNode = node as ObjectsOutputNode;
                objectSpawners.Add(typedNode.GetValue());
            }
        }

        lock(locker)
        {
            heightMapData.Remove(System.Threading.Thread.CurrentThread);
        }

        return objectSpawners;
    }

    public RoadSettings GetRoadSettings()
    {
        foreach (Node node in this.nodes)
        {
            if (node is RoadOutputNode)
            {
                var typedNode = node as RoadOutputNode;
                return typedNode.roadSettings;
            }
        }
        return null;
    }

    public void Init(System.Random prng)
    {
        heightMapData = new Dictionary<System.Threading.Thread, HeightMapGraphData>();

        foreach (BiomeGraphNode node in this.nodes)
        {
            node.seed = prng.Next(-100000, 100000);

            if (node is SimplexNoiseNode)
            {
                SimplexNoiseNode typedNode = node as SimplexNoiseNode;
                typedNode.noiseMapSettings.seed = prng.Next(-100000, 100000);
            }
            else if (node is RidgedTurbulenceNode)
            {
                RidgedTurbulenceNode typedNode = node as RidgedTurbulenceNode;
                typedNode.noiseMapSettings.seed = prng.Next(-100000, 100000);
            }
            else if (node is TerracedNoiseNode)
            {
                TerracedNoiseNode typedNode = node as TerracedNoiseNode;
                typedNode.noiseMapSettings.seed = prng.Next(-100000, 100000);
            }
        }

        this.initialized = true;
    }

    public float GetMaxPossibleHeight()
    {
        float maxHeight = float.MinValue;
        foreach (Node node in this.nodes)
        {
            if (node is SimplexNoiseNode)
            {
                var typedNode = node as SimplexNoiseNode;
                float height = GetMaxHeight(typedNode.noiseMapSettings.simplexNoiseSettings);

                if (height > maxHeight)
                {
                    maxHeight = height;
                }
            }
        }
        return maxHeight;
    }

    private float GetMaxHeight(PerlinNoiseSettings settings)
    {
        float height = 0f;
        float amplitude = 1;
        for (int i = 0; i < settings.octaves; i++)
        {
            height += amplitude;
            amplitude *= settings.persistance;
        }
        return height;
    }

    public float GetMinHeight()
    {
        float minHeight = float.MaxValue;
        foreach (Node node in this.nodes)
        {
            if (node is HeightMapScaleNode)
            {
                var typedNode = node as HeightMapScaleNode;
                float height = typedNode.scale * typedNode.heightCurve.Evaluate(0); ;

                if (height < minHeight)
                {
                    minHeight = height;
                }
            }
        }
        return minHeight;
    }

    public float GetMaxHeight()
    {
        float maxHeight = float.MinValue;
        foreach (Node node in this.nodes)
        {
            if (node is HeightMapScaleNode)
            {
                var typedNode = node as HeightMapScaleNode;
                float height = typedNode.scale * typedNode.heightCurve.Evaluate(1);

                if (height > maxHeight)
                {
                    maxHeight = height;
                }
            }
        }
        return maxHeight;
    }

    public float GetMaxRoadWidth()
    {
        float maxWidth = 0f;
        foreach (Node node in this.nodes)
        {
            if (node is RoadOutputNode)
            {
                var typedNode = node as RoadOutputNode;
                float width = typedNode.roadSettings.width;

                if (width > maxWidth)
                {
                    maxWidth = width;
                }
            }
        }
        return maxWidth;
    }
}

// Contains all the data needed to calculate the heightmap
public struct HeightMapGraphData
{
    public TerrainSettings terrainSettings;
    public Vector2 sampleCentre;
    public int width;

    public int biome;
    public BiomeInfo biomeInfo;

    public float[] heightMap;
    public float[] roadStrengthMap;

    public HeightMapGraphData(TerrainSettings terrainSettings, Vector2 sampleCentre, int width)
    {
        this.terrainSettings = terrainSettings;
        this.sampleCentre = sampleCentre;
        this.width = width;
        this.biome = 0;
        this.biomeInfo = new BiomeInfo();
        this.biomeInfo.width = width;

        this.heightMap = new float[width * width];
        this.roadStrengthMap = new float[width * width];
    }

    public HeightMapGraphData(TerrainSettings terrainSettings, Vector2 sampleCentre, int width, int biome, BiomeInfo biomeInfo)
    {
        this.terrainSettings = terrainSettings;
        this.sampleCentre = sampleCentre;
        this.width = width;

        this.biome = biome;
        this.biomeInfo = biomeInfo;

        this.heightMap = new float[width * width];
        this.roadStrengthMap = new float[width * width];
    }

    public HeightMapGraphData(TerrainSettings terrainSettings, Vector2 sampleCentre, BiomeInfo biomeInfo, int width, int biome, float[] heightMap, float[] roadStrengthMap)
    {
        this.terrainSettings = terrainSettings;
        this.sampleCentre = sampleCentre;
        this.biomeInfo = biomeInfo;
        this.biome = biome;
        this.heightMap = heightMap;
        this.roadStrengthMap = roadStrengthMap;

        this.width = width;
    }
}