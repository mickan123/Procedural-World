using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using System.Linq;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/HydraulicErosion")]
public class HydraulicErosionNode : BiomeGraphNode
{
    public ErosionSettings erosionSettings;

    [Input] public float[,] heightMapIn;
    [Output] public float[,] heightMapOut;

    private static readonly int[,] neighBouroffsets = { { 1, 0 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "heightMapOut")
        {
            return ErodeHeightMap();
        }
        else
        {
            return null;
        }
    }

    public float[,] ErodeHeightMap()
    {
        float[,] heightMap = GetInputValue<float[,]>("heightMapIn", this.heightMapIn);

        var biomeGraph = this.graph as BiomeGraph;

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        int padding = biomeGraph.terrainSettings.erosionSettings.maxLifetime;

        float chunkWidth = biomeGraph.terrainSettings.meshSettings.numVerticesPerLine;
        ChunkCoord curChunkCoord = new ChunkCoord(Mathf.RoundToInt(biomeGraph.sampleCentre.x / chunkWidth), Mathf.RoundToInt(biomeGraph.sampleCentre.y / chunkWidth));

        float[,] outputHeightMap = null;
        
        if (biomeGraph.worldManager != null)
        {
            // Generate list of chunk coords for current chunk and neighbouring chunks
            List<ChunkCoord> adjacentChunkCoords = new List<ChunkCoord>();
            adjacentChunkCoords.Add(curChunkCoord);
            for (int i = 0; i < neighBouroffsets.GetLength(0); i++)
            {
                ChunkCoord adjacentVector = new ChunkCoord(Mathf.RoundToInt(curChunkCoord.x + neighBouroffsets[i, 0]),
                                                            Mathf.RoundToInt(curChunkCoord.y + neighBouroffsets[i, 1]));

                adjacentChunkCoords.Add(adjacentVector);
            }

            adjacentChunkCoords = adjacentChunkCoords.OrderBy(v => Mathf.Abs(v.x) + Mathf.Abs(v.y)).ToList();
            float[,] erosionMask = new float[width, height];

            for (int i = 0; i < adjacentChunkCoords.Count; i++)
            {
                if (adjacentChunkCoords[i] == curChunkCoord)
                {
                    erosionMask = CalculateBiomeBlendingMask(erosionMask, padding);
                    outputHeightMap = HydraulicErosion.Erode(heightMap, erosionMask, biomeGraph.terrainSettings, biomeGraph.biomeInfo, biomeGraph.worldManager, biomeGraph.sampleCentre);
                    biomeGraph.worldManager.DoneErosion(curChunkCoord, heightMap);
                    break;
                }
                else
                {
                    biomeGraph.worldManager.UpdateChunkBorder(curChunkCoord, adjacentChunkCoords[i], heightMap, erosionMask);
                }
            }
        }
        else
        {
            float[,] erosionMask = new float[width, height];
            CalculateBiomeBlendingMask(erosionMask, padding);
            outputHeightMap = HydraulicErosion.Erode(heightMap, erosionMask, biomeGraph.terrainSettings, biomeGraph.biomeInfo, biomeGraph.worldManager, biomeGraph.sampleCentre);
        }

        return outputHeightMap;
    }

    // This creates a mask of weights that blend the edges of biomes as they have some overlap due to padding
    public static float[,] CalculateBiomeBlendingMask(float[,] mask, int padding)
    {
        int mapSize = mask.GetLength(0);
        for (int i = 0; i < mask.GetLength(0); i++)
        {
            for (int j = 0; j < mask.GetLength(1); j++)
            {
                if (mask[i, j] == 0)
                {
                    mask[i, j] = 1f;
                }
                else
                {
                    int xDistFromEdge = Mathf.Min(i, Mathf.Abs(i - mapSize));
                    int yDistFromEdge = Mathf.Min(j, Mathf.Abs(j - mapSize));
                    int distFromEdge = Mathf.Min(xDistFromEdge, yDistFromEdge);
                    mask[i, j] = Mathf.Max(distFromEdge - padding - 3, 0) / (float)(padding);
                }
            }
        }
        return mask;
    }
}
