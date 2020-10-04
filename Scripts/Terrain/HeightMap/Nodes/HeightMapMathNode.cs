using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class HeightMapMathNode : Node
{
    [Input] public HeightMapWrapper heightMapInA;
    [Input] public HeightMapWrapper heightMapInB;
    [Output] public HeightMapWrapper heightMapOut;

    public enum MathType { Add, Subtract, Multiply }
    public MathType mathType = MathType.Add;

    public override object GetValue(NodePort port) {

        HeightMapWrapper heightMapInA = GetInputValue<HeightMapWrapper>("heightMapInA", this.heightMapInA);
        HeightMapWrapper heightMapInB = GetInputValue<HeightMapWrapper>("heightMapInB", this.heightMapInB);

        int width = heightMapInA.heightMap.GetLength(0);
        int height = heightMapInA.heightMap.GetLength(1);

        float[,] heightMap = new float[width, height];
        HeightMapWrapper result = new HeightMapWrapper(heightMap);

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

    public void AddHeightMaps(HeightMapWrapper a, HeightMapWrapper b, ref HeightMapWrapper result) {
        int width = a.heightMap.GetLength(0);
        int height = a.heightMap.GetLength(1);

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                result.heightMap[x, y] = a.heightMap[x, y] + b.heightMap[x, y];
            }
        }
    }

    public void SubHeightMaps(HeightMapWrapper a, HeightMapWrapper b, ref HeightMapWrapper result) {
        int width = a.heightMap.GetLength(0);
        int height = a.heightMap.GetLength(1);

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                result.heightMap[x, y] = a.heightMap[x, y] - b.heightMap[x, y];
            }
        }
    }

    public void MulHeightMaps(HeightMapWrapper a, HeightMapWrapper b, ref HeightMapWrapper result) {
        int width = a.heightMap.GetLength(0);
        int height = a.heightMap.GetLength(1);

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                result.heightMap[x, y] = a.heightMap[x, y] * b.heightMap[x, y];
            }
        }
    }

}
