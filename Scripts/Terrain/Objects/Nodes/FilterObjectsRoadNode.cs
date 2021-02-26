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
        for (int i = 0; i < positionData.positions.Count; i++)
        {
            if (positionData.positions.filtered[i])
            {   
                continue;   
            }

            Vector3 point = positionData.positions.positions[i];
            float roadStrength = Common.HeightFromFloatCoord(point.x, point.z, biomeGraph.roadStrengthMap);


            if ((float)prng.NextDouble() * 0.5f < roadStrength)
            {
                positionData.positions.filtered[i] = true;
            }
        }

        return positionData;
    }
}
