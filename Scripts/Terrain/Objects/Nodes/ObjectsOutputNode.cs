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

        int numCoords = 0;
        int length = positionData.positions.Length;
        for (int i = 0; i < length; i++)
        {
            if (!positionData.positions.filtered[i])
            {
                numCoords += 1;
            }
        }

        float[] updatedXCoords = new float[numCoords];
        float[] updatedYCoords = new float[numCoords];
        float[] updatedZCoords = new float[numCoords];
        Vector3[] updatedScales = new Vector3[numCoords];
        Quaternion[] updatedRotations = new Quaternion[numCoords];
        
        int index = 0;
        for (int i = 0; i < length; i++)
        {
            if (!positionData.positions.filtered[i])
            {
                updatedXCoords[index] = positionData.positions.xCoords[i];
                updatedYCoords[index] = positionData.positions.yCoords[i];
                updatedZCoords[index] = positionData.positions.zCoords[i];
                updatedScales[index] = positionData.positions.scales[i];
                updatedRotations[index] = positionData.positions.rotations[i];
                index += 1;
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

        int length = positionData.positions.Length;
        int randIdx = 0;
        for (int i = 0; i < length; i++)
        {
            if (positionData.positions.filtered[i])
            {
                continue;
            }
            
            float rand = this.randomValues[randIdx];
            randIdx++;
            if (randIdx >= this.numRandomValues) 
            {
                randIdx = 0;
            }

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
