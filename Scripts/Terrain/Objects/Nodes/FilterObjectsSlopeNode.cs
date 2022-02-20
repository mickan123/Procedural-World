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
        int mapSize = positionData.heightMap.Length;

        float[][] angles = Common.CalculateAngles(positionData.heightMap);

        // Create a padded angles array so we don't have to do any bounds checking 
        // in later calculations for improved performance
        float[][] paddedAngles = new float[mapSize + 1][];
        for (int i = 0; i < mapSize + 1; i++)
        {
            paddedAngles[i] = new float[mapSize + 1];
        }
        
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                paddedAngles[i][j] = angles[i][j];
            }
        }
        for (int i = 0; i < mapSize; i++)
        {
            paddedAngles[i][mapSize] = paddedAngles[i][mapSize - 1];
            paddedAngles[mapSize][i] = paddedAngles[mapSize - 1][i];
        }

        // Iterate over points and filter those that don't match slope criteria
        int length = positionData.positions.Length;
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

            float xSlope = x * paddedAngles[coordX][coordZ] + (1f - x) * paddedAngles[coordX + 1][coordZ];
            float ySlope = y * paddedAngles[coordX][coordZ] + (1f - y) * paddedAngles[coordX][coordZ + 1];

            float angle = (xSlope + ySlope) / 2f;

            if (angle > maxAngle || angle < minAngle)
            {
                positionData.positions.filtered[i] = true;
            }
        }
    }
}
