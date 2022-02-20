using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/Scale")]
public class HeightMapScaleNode : BiomeGraphNode
{
    [Input] public float[][] heightMapIn;
    [Output] public float[][] heightMapOut;

    public float scale;
    public AnimationCurve heightCurve;

    public override object GetValue(NodePort port)
    {
        float[][] heightMapIn = GetInputValue<float[][]>("heightMapIn", this.heightMapIn);

        int width = heightMapIn.Length;
        int height = heightMapIn[0].Length;

        float[][] result = new float[width][];
        for (int i = 0; i < width; i++)
        {
            result[i] = new float[height];
        }

        if (port.fieldName == "heightMapOut")
        {
            ScaleHeightMap(heightMapIn, ref result);
        }

        return result;
    }

    public void ScaleHeightMap(float[][] heightMap, ref float[][] result)
    {
        int width = heightMap.Length;
        int height = heightMap[0].Length;

        AnimationCurve heightCurve_threadsafe = new AnimationCurve(this.heightCurve.keys);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                result[x][y] = scale * heightCurve_threadsafe.Evaluate(heightMap[x][y]) * heightMap[x][y];
            }
        }
    }

}
