using UnityEngine;
using XNode;
using System;
using System.Collections.Generic;

[Serializable, CreateAssetMenu(menuName = "Procedural Generation Settings/HeightMapNodeGraph")]
public class HeightMapNodeGraph : NodeGraph
{   
    public TerrainSettings terrainSettings;
    public Vector2 sampleCentre;
    public int width;
    public int height;

    public float[,] GetHeightMap(TerrainSettings terrainSettings, Vector2 sampleCentre, int width, int height) {
        this.terrainSettings = terrainSettings;
        this.sampleCentre = sampleCentre;
        this.width = width;
        this.height = height;

        foreach (Node node in this.nodes) {
            if (node is HeightMapOutputNode) {
                var typedNode = node as HeightMapOutputNode;
                return typedNode.GetValue();
            }
        }
        return null;
    }

    public float GetMaxPossibleHeight() {
        float maxHeight = float.MinValue;
        foreach (Node node in this.nodes) {
            if (node is SimplexNoiseNode) {
                var typedNode = node as SimplexNoiseNode;
                float height = GetMaxHeight(typedNode.noiseMapSettings.simplexNoiseSettings);

                if (height > maxHeight) {
                    maxHeight = height;
                }
            }
        }
        return maxHeight;
    }

    private float GetMaxHeight(PerlinNoiseSettings settings) {
        float height = 0f;
        float amplitude = 1;
		for (int i = 0; i < settings.octaves; i++) {
			height += amplitude; 
			amplitude *= settings.persistance;
		}
        return height;
    }

    public float GetMinHeight() {
        float minHeight = float.MaxValue;
        foreach (Node node in this.nodes) {
            if (node is HeightMapScaleNode) {
                var typedNode = node as HeightMapScaleNode;
                float height = typedNode.scale * typedNode.heightCurve.Evaluate(0);;

                if (height < minHeight) {
                    minHeight = height;
                }
            }
        }
        return minHeight;
    }

    public float GetMaxHeight() {
        float maxHeight = float.MinValue;
        foreach (Node node in this.nodes) {
            if (node is HeightMapScaleNode) {
                var typedNode = node as HeightMapScaleNode;
                float height = typedNode.scale * typedNode.heightCurve.Evaluate(1);

                if (height > maxHeight) {
                    maxHeight = height;
                }
            }
        }
        return maxHeight;
    }
}

