using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("HeightMap/Scale")]
public class HeightMapScaleNode : BiomeGraphNode
{
    [Input] public float[] heightMapIn;
    [Output] public float[] heightMapOut;

    public float scale;
    public AnimationCurve heightCurve;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName != "heightMapOut" || this.heightMapIn == null)
        {
            return null;
        }

        float[] heightMapIn = GetInputValue<float[]>("heightMapIn", this.heightMapIn);
        int width = heightMapIn.Length;
        float[] result = new float[width];
        ScaleHeightMap(heightMapIn, ref result);
    
        return result;
    }

    public void ScaleHeightMap(float[] heightMap, ref float[] result)
    {
        int width = heightMap.Length;

        AnimationCurve heightCurve_threadsafe = new AnimationCurve(this.heightCurve.keys);

        for (int i = 0; i < width; i++)
        {
            result[i] = scale * heightCurve_threadsafe.Evaluate(heightMap[i]) * heightMap[i];
        }
    }

}
