using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class DetailsOutputNode : BiomeGraphNode
{
    [Input(ShowBackingValue.Never, ConnectionType.Override)] public ObjectPositionData positionData;

    public bool hide = false;

    public ObjectSpawner GetValue()
    {
        ObjectPositionData positionData = GetInputValue<ObjectPositionData>("positionData");
        if (positionData.isDetail)
        {
            return new ObjectSpawner(
                positionData.detailMaterials,
                positionData.detailMode,
                positionData.positions,
                new System.Random(seed),
                this.hide
            );
        }
        else
        {
            return new ObjectSpawner(
                positionData.terrainObjects,
                positionData.positions,
                new System.Random(seed),
                this.hide
            );
        }
    }
}
