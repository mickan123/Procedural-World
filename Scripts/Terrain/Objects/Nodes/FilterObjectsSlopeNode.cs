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
        // Generate slope at every point
        int width = positionData.width;

        float[] slopes = Common.CalculateSlopes(positionData.heightMap, width);

        // Create a padded slopes array so we don't have to do any bounds checking 
        // in later calculations for improved performance

        int paddedWidth = width + 1;
        float[] paddedSlopes = new float[paddedWidth * paddedWidth];
        
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < width; j++)
            {
                paddedSlopes[i * paddedWidth + j] = slopes[i * width + j];
            }
        }
        for (int i = 0; i < width; i++)
        {
            paddedSlopes[i * paddedWidth + width] = paddedSlopes[i * paddedWidth + width - 1];
            paddedSlopes[width * paddedWidth + i] = paddedSlopes[(width - 1) * paddedWidth + i];
        }

        // Iterate over points and filter those that don't match slope criteria
        int length = positionData.positions.Length;
        
        float minAngleSlope = Mathf.Tan(minAngle * Mathf.Deg2Rad);
        float maxAngleSlope = Mathf.Tan(maxAngle * Mathf.Deg2Rad);

        for (int i = 0; i < length; i++)
        {
            if (positionData.positions.filtered[i])
            {
                continue;
            }

            float xIn = positionData.positions.xCoords[i];
            float yIn = positionData.positions.zCoords[i];

            int coordX = (int)xIn;
            int coordZ = (int)yIn;

            // Calculate offset inside the cell (0,0) = at NW node, (1,1) = at SE node
            float x = xIn - coordX;
            float y = yIn - coordZ;

            float xSlope = x * paddedSlopes[coordX * paddedWidth + coordZ] + (1f - x) * paddedSlopes[(coordX + 1) * paddedWidth + coordZ];
            float ySlope = y * paddedSlopes[coordX * paddedWidth + coordZ] + (1f - y) * paddedSlopes[coordX * paddedWidth + coordZ + 1];

            float slope = (xSlope + ySlope) / 2f;

            if (slope > maxAngleSlope || slope < minAngleSlope)
            {
                positionData.positions.filtered[i] = true;
            }
        }
    }
}
