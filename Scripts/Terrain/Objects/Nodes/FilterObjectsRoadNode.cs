using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/Filters/RoadFilter")]
public class FilterObjectsRoadNode : BiomeGraphNode
{
    [Input] public ObjectPositionData positionDataIn;
    [Output] public ObjectPositionData positionDataOut;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "positionDataOut")
        {
            return FilterOnRoad(GetInputValue<ObjectPositionData>("positionDataIn", this.positionDataIn));
        }
        else
        {
            return null;
        }
    }

    private ObjectPositionData FilterOnRoad(ObjectPositionData positionData)
    {
        if (positionData == null)
        {
            return null;
        }
        var biomeGraph = this.graph as BiomeGraph;

        System.Random prng = new System.Random();

        List<int> indices = new List<int>(positionData.positions.Count);
        for (int i = 0; i < positionData.positions.Count; i++)
        {
            Vector3 point = positionData.positions.positions[i];
            float roadStrength = Common.HeightFromFloatCoord(point.x, point.z, biomeGraph.roadStrengthMap);

            if (Common.NextFloat(prng, 0f, 0.5f) > roadStrength)
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
