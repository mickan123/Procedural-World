using UnityEngine;
using XNode;
using System;
using System.Collections.Generic;

[Serializable, CreateAssetMenu(menuName = "Procedural Generation Settings/BiomeGraph")]
public class BiomeGraph : NodeGraph
{
    public BiomeInfo biomeInfo;
    public TerrainSettings terrainSettings;
    public Vector2 sampleCentre;
    public int biome;
    public int width;
    public int height;

    public float[,] heightMap;
    public float[,] roadStrengthMap;

    private static readonly System.Object obj = new System.Object();

    public bool initialized = false;

    public float[,] GetHeightMap(TerrainSettings terrainSettings, Vector2 sampleCentre, int width, int height)
    {
        lock(obj)
        {
            this.terrainSettings = terrainSettings;
            this.sampleCentre = sampleCentre;
            this.width = width;
            this.height = height;

            foreach (Node node in this.nodes)
            {
                if (node is HeightMapOutputNode)
                {
                    var typedNode = node as HeightMapOutputNode;
                    return typedNode.GetValue();
                }
            }
            return null;
        }
    }

    public float[,] GetHeightMap(
        BiomeInfo info, 
        TerrainSettings terrainSettings, 
        Vector2 sampleCentre, 
        int biome,
        int width, 
        int height
    )
    {
        lock(obj)
        {
            this.biomeInfo = info;
            this.terrainSettings = terrainSettings;
            this.sampleCentre = sampleCentre;
            this.biome = biome;
            this.width = width;
            this.height = height;

            foreach (Node node in this.nodes)
            {
                if (node is HeightMapOutputNode)
                {
                    var typedNode = node as HeightMapOutputNode;
                    return typedNode.GetValue();
                }
            }
            return null;
        }
    }

    public List<ObjectSpawner> GetObjectSpawners(float[,] heightMap, float[,] roadStrengthMap) {
        lock(obj)
        {
            List<ObjectSpawner> objectSpawners = new List<ObjectSpawner>();

            this.heightMap = heightMap;
            this.roadStrengthMap = roadStrengthMap;

            foreach (Node node in this.nodes)
            {
                if (node is ObjectsOutputNode) {
                    ObjectsOutputNode typedNode = node as ObjectsOutputNode;
                    objectSpawners.Add(typedNode.GetValue());
                }
            }

            return objectSpawners;
        }
    }

    public RoadSettings GetRoadSettings()
    {
        lock(obj)
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
    }

    public void Init(System.Random prng) {
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