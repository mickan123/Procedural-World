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
        for (int i = 0; i < positionData.positions.Count; i++)
        {
            Vector3 curPoint = positionData.positions.positions[i];
            float angle = Common.CalculateAngle(curPoint.x, curPoint.z, positionData.heightMap);
            if (angle > maxAngle || angle < minAngle)
            {
                positionData.positions.filtered[i] = true;
            }
        }

        return positionData;
    }
}
