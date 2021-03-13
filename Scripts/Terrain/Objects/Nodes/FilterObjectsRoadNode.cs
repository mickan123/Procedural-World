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
            ObjectPositionData data = GetInputValue<ObjectPositionData>("positionDataIn", this.positionDataIn);
            FilterOnRoad(data);
            return data;
        }
        else
        {
            return null;
        }
    }

    private void FilterOnRoad(ObjectPositionData positionData)
    {
        if (positionData == null)
        {
            return;
        }
        var biomeGraph = this.graph as BiomeGraph;

        System.Random prng = new System.Random();
        for (int i = 0; i < positionData.positions.Count; i++)
        {
            if (positionData.positions.filtered[i])
            {
                continue;
            }

            // 1 Offset due to out of mesh vertice at edge of heightmap, subtract half since
            // road strength corresponds to centre of square rather than corners
            float offset = 0.5f;

            float roadStrength = Common.HeightFromFloatCoord(
                positionData.positions.xCoords[i] + offset, 
                positionData.positions.zCoords[i] + offset, 
                biomeGraph.roadStrengthMap
            );

            if ((float)prng.NextDouble() * 0.5f < roadStrength)
            {
                positionData.positions.filtered[i] = true;
            }
        }
    }
}
