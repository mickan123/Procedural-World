using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/ObjectsOutput")]
public class ObjectsOutputNode : BiomeGraphNode
{
    [Input(ShowBackingValue.Never, ConnectionType.Override)] public ObjectPositionData positionData;

    public bool hide = false;

    public ObjectSpawner GetValue()
    {
        ObjectPositionData positionData = GetInputValue<ObjectPositionData>("positionData");

        List<Vector3> updatedPoints = new List<Vector3>();
        List<Vector3> updatedScales = new List<Vector3>();
        List<Quaternion> updatedRotations = new List<Quaternion>();
        for (int i = 0; i < positionData.positions.Count; i++)
        {
            if (!positionData.positions.filtered[i])
            {
                updatedPoints.Add(positionData.positions.positions[i]);
                updatedScales.Add(positionData.positions.scales[i]);
                updatedRotations.Add(positionData.positions.rotations[i]);
            }
        }
        ObjectPositions updatedPositions = new ObjectPositions(updatedPoints, updatedScales, updatedRotations);
        positionData.positions = updatedPositions;

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
