using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class HeightMapConstantNode : Node
{
    [Output] public float[,] heightMapOut;

    public float value = 1f;

    public override object GetValue(NodePort port) {

        HeightMapNodeGraph heightMapGraph = this.graph as HeightMapNodeGraph;

        int width = heightMapGraph.width;
        int height = heightMapGraph.height;

        float[,] result = new float[width, height];

        if (port.fieldName == "heightMapOut") {
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    result[i, j] = value;
                }
            }
        }
        
        return result;
    }
}
