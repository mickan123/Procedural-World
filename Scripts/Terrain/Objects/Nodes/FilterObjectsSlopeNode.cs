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
            Vector3 curPoint = positionData.positions.positions[i];
            float angle = Common.CalculateAngle(curPoint.x, curPoint.z, positionData.heightMap);
            if (angle < maxAngle && angle > minAngle)
            {
                indices.Add(i);
            }
        }
        List<Vector3> updatedPoints = new List<Vector3>(indices.Count);
        List<Vector3> updatedScales = new List<Vector3>(indices.Count);
        List<Quaternion> updatedRotations = new List<Quaternion>(indices.Count);
        for (int i = 0; i < indices.Count; i++)
        {
            updatedPoints.Add(positionData.positions.positions[indices[i]]);
            updatedScales.Add(positionData.positions.scales[indices[i]]);
            updatedRotations.Add(positionData.positions.rotations[indices[i]]);
        }
        ObjectPositions updatedPositions = new ObjectPositions(updatedPoints, updatedScales, updatedRotations);
        positionData.positions = updatedPositions;

        return positionData;
    }
}
