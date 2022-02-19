using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/Filters/SlopeFilter")]
public class FilterObjectsSlopeNode : BiomeGraphNode
{
    [Input] public ObjectPositionData positionDataIn;
    [Output] public ObjectPositionData positionDataOut;

    public float minAngle;
    public float maxAngle;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "positionDataOut")
        {
            ObjectPositionData data = GetInputValue<ObjectPositionData>("positionDataIn", this.positionDataIn);
            FilterBySlope(data);
            return data;
        }
        else
        {
            return null;
        }
    }

    private void FilterBySlope(ObjectPositionData positionData)
    {
        if (positionData == null)
        {
            return;
        }

        // Generate slope at every point
        int maxIndex = positionData.heightMap.GetLength(0) - 1;
        float[,] slopeMap = new float[positionData.heightMap.GetLength(0), positionData.heightMap.GetLength(1)];
        for (int x = 0; x < slopeMap.GetLength(0); x++)
        {
            for (int y = 0; y < slopeMap.GetLength(1); y++)
            {
                float height = positionData.heightMap[x, y];

                // Compute the differentials by stepping over 1 in both directions.
                float dx = positionData.heightMap[Mathf.Min(x + 1, maxIndex), y] - height;

                float dy = positionData.heightMap[x, Mathf.Min(y + 1, maxIndex)] - height;

                float dMax = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                slopeMap[x, y] = Mathf.Rad2Deg * Mathf.Atan2(
                    dMax, 
                    1
                );
            }
        }

        // Iterate over points and filter those that don't match slope criteria
        for (int i = 0; i < positionData.positions.Length; i++)
        {
            float xIn = positionData.positions.xCoords[i];
            float yIn = positionData.positions.zCoords[i];

            int coordX = (int)xIn;
            int coordZ = (int)yIn;

            // Calculate offset inside the cell (0,0) = at NW node, (1,1) = at SE node
            float x = xIn - coordX;
            float y = yIn - coordZ;

            float xSlope = x * slopeMap[coordX, coordZ] + (1f - x) * slopeMap[Mathf.Min(coordX + 1, maxIndex), coordZ];
            float ySlope = y * slopeMap[coordX, coordZ] + (1f - y) * slopeMap[coordX, Mathf.Min(coordZ + 1, maxIndex)];

            float angle = (xSlope + ySlope) / 2f;

            if (angle > maxAngle || angle < minAngle)
            {
                positionData.positions.filtered[i] = true;
            }
        }
    }
}
