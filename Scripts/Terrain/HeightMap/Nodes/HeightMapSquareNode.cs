using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class HeightMapSquareNode: Node
{
    [Input] public float[,] heightMapIn;
    [Output] public float[,] heightMapOut;

    public override object GetValue(NodePort port) {

        float[,] heightMapIn = GetInputValue<float[,]>("heightMapIn", this.heightMapIn);

        int width = heightMapIn.GetLength(0);
        int height = heightMapIn.GetLength(1);

        float[,] result = new float[width, height];

        if (port.fieldName == "heightMapOut") {
            SquareHeightMap(heightMapIn, ref result);
        }
        
        return result;
    }

    public void SquareHeightMap(float[,] a, ref float[,] result) {
        int width = a.GetLength(0);
        int height = a.GetLength(1);

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                result[x, y] = a[x, y] * a[x, y];
            }
        }
    }
}
