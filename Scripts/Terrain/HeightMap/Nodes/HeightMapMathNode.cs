using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/Math")]
public class HeightMapMathNode : BiomeGraphNode
{
    [Input] public float[] heightMapInA;
    [Input] public float[] heightMapInB;
    [Output] public float[] heightMapOut;

    public enum MathType { Add, Subtract, Multiply }
    public MathType mathType = MathType.Add;

    public override object GetValue(NodePort port)
    {
        if (this.heightMapInA == null || this.heightMapInA == null)
        {
            return null;
        }
        float[] heightMapInA = GetInputValue<float[]>("heightMapInA", this.heightMapInA);
        float[] heightMapInB = GetInputValue<float[]>("heightMapInB", this.heightMapInB);

        int width = heightMapInA.Length;

        float[] result = new float[width];

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

    public void AddHeightMaps(float[] a, float[] b, ref float[] result)
    {
        int width = a.Length;
        for (int i = 0; i < width; i++)
        {
            result[i] = a[i] + b[i];
        }
    }

    public void SubHeightMaps(float[] a, float[] b, ref float[] result)
    {
        int width = a.Length;
        for (int i = 0; i < width; i++)
        {
            result[i] = a[i] - b[i];
        }
    }

    public void MulHeightMaps(float[] a, float[] b, ref float[] result)
    {
        int width = a.Length;
        for (int i = 0; i < width; i++)
        {
            result[i] = a[i] * b[i];
        }
    }

}
