using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/Square")]
public class HeightMapSquareNode : BiomeGraphNode
{
    [Input] public float[][] heightMapIn;
    [Output] public float[][] heightMapOut;

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
            SquareHeightMap(heightMapIn, ref result);
        }

        return result;
    }

    public void SquareHeightMap(float[][] a, ref float[][] result)
    {
        int width = a.Length;
        int height = a[0].Length;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                result[x][y] = a[x][y] * a[x][y];
            }
        }
    }
}
