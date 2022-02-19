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

        float[,] angles = Common.CalculateAngles(positionData.heightMap);

        // Iterate over points and filter those that don't match slope criteria
        int length = positionData.positions.Length;
        for (int i = 0; i < length; i++)
        {
            float xIn = positionData.positions.xCoords[i];
            float yIn = positionData.positions.zCoords[i];

            int coordX = (int)xIn;
            int coordZ = (int)yIn;

            // Calculate offset inside the cell (0,0) = at NW node, (1,1) = at SE node
            float x = xIn - coordX;
            float y = yIn - coordZ;

            float xSlope = x * angles[coordX, coordZ] + (1f - x) * angles[Mathf.Min(coordX + 1, maxIndex), coordZ];
            float ySlope = y * angles[coordX, coordZ] + (1f - y) * angles[coordX, Mathf.Min(coordZ + 1, maxIndex)];

            float angle = (xSlope + ySlope) / 2f;

            if (angle > maxAngle || angle < minAngle)
            {
                positionData.positions.filtered[i] = true;
            }
        }
    }
}
