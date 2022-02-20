using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/Math")]
public class HeightMapMathNode : BiomeGraphNode
{
    [Input] public float[][] heightMapInA;
    [Input] public float[][] heightMapInB;
    [Output] public float[][] heightMapOut;

    public enum MathType { Add, Subtract, Multiply }
    public MathType mathType = MathType.Add;

    public override object GetValue(NodePort port)
    {
        float[][] heightMapInA = GetInputValue<float[][]>("heightMapInA", this.heightMapInA);
        float[][] heightMapInB = GetInputValue<float[][]>("heightMapInB", this.heightMapInB);

        int width = heightMapInA.Length;
        int height = heightMapInA[0].Length;

        float[][] result = new float[width][];
        for (int i = 0; i < width; i++)
        {
            result[i] = new float[height];
        }

        if (port.fieldName == "heightMapOut")
        {
            switch (mathType)
            {
                default:
                case MathType.Add:
                    AddHeightMaps(heightMapInA, heightMapInB, ref result);
                    break;
                case MathType.Subtract:
                    SubHeightMaps(heightMapInA, heightMapInB, ref result);
                    break;
                case MathType.Multiply:
                    MulHeightMaps(heightMapInA, heightMapInB, ref result);
                    break;
            }
        }
        return result;
    }

    public void AddHeightMaps(float[][] a, float[][] b, ref float[][] result)
    {
        int width = a.Length;
        int height = a[0].Length;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                result[x][y] = a[x][y] + b[x][y];
            }
        }
    }

    public void SubHeightMaps(float[][] a, float[][] b, ref float[][] result)
    {
        int width = a.Length;
        int height = a[0].Length;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                result[x][y] = a[x][y] - b[x][y];
            }
        }
    }

    public void MulHeightMaps(float[][] a, float[][] b, ref float[][] result)
    {
        int width = a.Length;
        int height = a[0].Length;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                result[x][y] = a[x][y] * b[x][y];
            }
        }
    }

}
