using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class HeightMapMathNode : Node
{
    [Input] public float[,] heightMapInA;
    [Input] public float[,] heightMapInB;
    [Output] public float[,] heightMapOut;

    public enum MathType { Add, Subtract, Multiply }
    public MathType mathType = MathType.Add;

    public override object GetValue(NodePort port) {

        float[,] heightMapInA = GetInputValue<float[,]>("heightMapInA", this.heightMapInA);
        float[,] heightMapInB = GetInputValue<float[,]>("heightMapInB", this.heightMapInB);

        int width = heightMapInA.GetLength(0);
        int height = heightMapInA.GetLength(1);

        float[,] result = new float[width, height];

        if (port.fieldName == "heightMapOut") {
            switch (mathType) {
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

    public void AddHeightMaps(float[,] a, float[,] b, ref float[,] result) {
        int width = a.GetLength(0);
        int height = a.GetLength(1);

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                result[x, y] = a[x, y] + b[x, y];
            }
        }
    }

    public void SubHeightMaps(float[,] a, float[,] b, ref float[,] result) {
        int width = a.GetLength(0);
        int height = a.GetLength(1);

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                result[x, y] = a[x, y] - b[x, y];
            }
        }
    }

    public void MulHeightMaps(float[,] a, float[,] b, ref float[,] result) {
        int width = a.GetLength(0);
        int height = a.GetLength(1);

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                result[x, y] = a[x, y] * b[x, y];
            }
        }
    }

}
