using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/Scale")]
public class HeightMapScaleNode : BiomeGraphNode
{
    [Input] public float[,] heightMapIn;
    [Output] public float[,] heightMapOut;

    public float scale;
    public AnimationCurve heightCurve;

    public override object GetValue(NodePort port)
    {

        float[,] heightMapIn = GetInputValue<float[,]>("heightMapIn", this.heightMapIn);

        int width = heightMapIn.GetLength(0);
        int height = heightMapIn.GetLength(1);

        float[,] result = new float[width, height];

        if (port.fieldName == "heightMapOut")
        {
            ScaleHeightMap(heightMapIn, ref result);
        }

        return result;
    }

    public void ScaleHeightMap(float[,] heightMap, ref float[,] result)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        AnimationCurve heightCurve_threadsafe = new AnimationCurve(this.heightCurve.keys);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                result[x, y] = scale * heightCurve_threadsafe.Evaluate(heightMap[x, y]) * heightMap[x, y];
            }
        }
    }

}
