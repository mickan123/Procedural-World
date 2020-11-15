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
            return FilterBySlope(GetInputValue<ObjectPositionData>("positionDataIn", this.positionDataIn));
        }
        else
        {
            return null;
        }
    }

    private ObjectPositionData FilterBySlope(ObjectPositionData positionData)
    {
        if (positionData == null)
        {
            return null;
        }
        List<int> indices = new List<int>(positionData.positions.Count);
        for (int i = 0; i < positionData.positions.Count; i++)
        {
            Vector3 curPoint = positionData.positions[i].position;
            float angle = Common.CalculateAngle(curPoint.x, curPoint.z, positionData.heightMap);
            if (angle < maxAngle && angle > minAngle)
            {
                indices.Add(i);
            }
        }
        List<ObjectPosition> updatedPositions = new List<ObjectPosition>(indices.Count);
        for (int i = 0; i < indices.Count; i++)
        {
            updatedPositions.Add(positionData.positions[indices[i]]);
        }
        positionData.positions = updatedPositions;
        return positionData;
    }
}
