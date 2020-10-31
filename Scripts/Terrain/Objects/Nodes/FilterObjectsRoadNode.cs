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
            Vector3 point = positionData.positions[i].position;
            float roadStrength = Common.HeightFromFloatCoord(point.x, point.z, biomeGraph.roadStrengthMap);

            if (Common.NextFloat(prng, 0f, 0.5f) > roadStrength)
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
