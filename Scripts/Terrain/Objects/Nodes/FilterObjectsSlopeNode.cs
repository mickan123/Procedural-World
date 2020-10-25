using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/Filters/SlopeFilter")]
public class FilterObjectsSlopeNode : BiomeGraphNode
{
    [Input] public ObjectPositionData positionDataIn;
    [Output] public ObjectPositionData positionDataOut;

    public float minSlope;
    public float maxSlope;

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
        for (int i = 0; i < positionData.positions.Count; i++)
        {
            Vector3 curPoint = positionData.positions[i].position;
            float slope = Common.CalculateSlope(curPoint.x, curPoint.z, positionData.heightMap);
            if (slope > maxSlope || slope < minSlope)
            {
                positionData.positions.RemoveAt(i);
                i--;
            }
        }
        return positionDataIn;
    }
}
