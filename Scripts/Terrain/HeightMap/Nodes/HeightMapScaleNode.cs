using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class HeightMapScaleNode : Node
{
    [Input] public HeightMapWrapper heightMapIn;
    [Output] public HeightMapWrapper heightMapOut;

    public float scale;
    public AnimationCurve heightCurve;

    public override object GetValue(NodePort port) {

        HeightMapWrapper heightMapIn = GetInputValue<HeightMapWrapper>("heightMapIn", this.heightMapIn);

        int width = heightMapIn.heightMap.GetLength(0);
        int height = heightMapIn.heightMap.GetLength(1);

        float[,] heightMap = new float[width, height];
        HeightMapWrapper result = new HeightMapWrapper(heightMap);

        if (port.fieldName == "heightMapOut") {
            ScaleHeightMap(heightMapIn, ref result);
        }

        return result;
    }

    public void ScaleHeightMap(HeightMapWrapper heightMap, ref HeightMapWrapper result) {
        int width = heightMap.heightMap.GetLength(0);
        int height = heightMap.heightMap.GetLength(1);

        AnimationCurve heightCurve_threadsafe = new AnimationCurve(this.heightCurve.keys);

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                result.heightMap[x, y] = scale * heightCurve_threadsafe.Evaluate(heightMap.heightMap[x, y]) * heightMap.heightMap[x, y];
            }
        }
    }

}
