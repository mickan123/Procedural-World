using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/Square")]
public class HeightMapSquareNode : BiomeGraphNode
{
    [Input] public float[] heightMapIn;
    [Output] public float[] heightMapOut;

    public override object GetValue(NodePort port)
    {
        float[] heightMapIn = GetInputValue<float[]>("heightMapIn", this.heightMapIn);

        int width = heightMapIn.Length;

        float[] result = new float[width];

        if (port.fieldName == "heightMapOut")
        {
            SquareHeightMap(heightMapIn, ref result);
        }

        return result;
    }

    public void SquareHeightMap(float[] a, ref float[] result)
    {
        int width = a.Length;
        for (int i = 0; i < width; i++)
        {
            result[i] = a[i] * a[i];
        }
    }
}
