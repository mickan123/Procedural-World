using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/ObjectsOutput")]
public class ObjectsOutputNode : BiomeGraphNode
{
    [Input(ShowBackingValue.Never, ConnectionType.Override)] public ObjectPositionData positionData;

    public bool hide = false;
    public bool isDetail = false;
    public bool staticBatch = false;

    public GameObject[] terrainObjects;
    public Material[] detailMaterials;
    public ObjectSpawner.DetailMode detailMode;

    public ObjectSpawner GetValue()
    {
        ObjectPositionData positionData = GetInputValue<ObjectPositionData>("positionData");
        FilterByBiome(positionData);

        List<float> updatedXCoords = new List<float>();
        List<float> updatedYCoords = new List<float>();
        List<float> updatedZCoords = new List<float>();
        List<Vector3> updatedScales = new List<Vector3>();
        List<Quaternion> updatedRotations = new List<Quaternion>();
        for (int i = 0; i < positionData.positions.Count; i++)
        {
            if (!positionData.positions.filtered[i])
            {
                updatedXCoords.Add(positionData.positions.xCoords[i]);
                updatedYCoords.Add(positionData.positions.yCoords[i]);
                updatedZCoords.Add(positionData.positions.zCoords[i]);
                updatedScales.Add(positionData.positions.scales[i]);
                updatedRotations.Add(positionData.positions.rotations[i]);
            }
        }
        ObjectPositions updatedPositions = new ObjectPositions(updatedXCoords, updatedYCoords, updatedZCoords, updatedScales, updatedRotations);
        positionData.positions = updatedPositions;

        if (this.isDetail)
        {
            return new ObjectSpawner(
                this.detailMaterials,
                this.detailMode,
                positionData.positions,
                new System.Random(seed),
                this.hide
            );
        }
        else
        {
            return new ObjectSpawner(
                this.terrainObjects,
                positionData.positions,
                new System.Random(seed),   
                this.staticBatch,
                this.hide
            );
        }
    }

    private void FilterByBiome(ObjectPositionData positionData)
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];

        System.Random prng = new System.Random(this.seed);

        for (int i = 0; i < positionData.positions.Count; i++)
        {
            float rand = (float)prng.NextDouble();

            int coordX = (int)positionData.positions.xCoords[i];
            int coordZ = (int)positionData.positions.zCoords[i];
            float biomeStrength = heightMapData.biomeInfo.biomeStrengths[coordX, coordZ, heightMapData.biome];

            if (rand > biomeStrength * biomeStrength * biomeStrength)
            {
                positionData.positions.filtered[i] = true;
            }
        }

    }
}
