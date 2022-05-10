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
    public bool generateCollider = false;

    public GameObject[] terrainObjects;

    public DetailRenderMode renderMode;
    public bool usePrototypeMesh; // Only valid for renderMode grass as it can use texture or gameobject
    public GameObject detailPrototype;
    public Texture2D detailTexture;
    public Color dryColor;
    public Color healthyColor;

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
                // Have to swap x and z due to how height map is represented in Unity TerrainData object
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
                this.GetDetailPrototype(positionData),
                this.GetDetailDensity(updatedPositions, positionData.width),
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
                this.generateCollider,
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

            float biomeStrength = heightMapData.biomeInfo.GetBiomeStrength(coordX, coordZ, heightMapData.biome);
            if (rand > biomeStrength * biomeStrength * biomeStrength)
            {
                positionData.positions.filtered[i] = true;
            }
        }
    }

    private int[,] GetDetailDensity(ObjectPositions positions, int width)
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        HeightMapGraphData heightMapData = biomeGraph.heightMapData[System.Threading.Thread.CurrentThread];
        TerrainSettings terrainSettings = heightMapData.terrainSettings;

        int[,] detailDensity = new int[width * terrainSettings.detailResolutionFactor, width * terrainSettings.detailResolutionFactor];

        for (int i = 0; i < positions.xCoords.Length; i++)
        {
            float xCoord = positions.xCoords[i];
            float yCoord = positions.zCoords[i];
            int x = (int)(xCoord * terrainSettings.detailResolutionFactor);
            int y = (int)(yCoord * terrainSettings.detailResolutionFactor);

            detailDensity[x, y]++;
        }

        return detailDensity;
    }

    private DetailPrototype GetDetailPrototype(ObjectPositionData positionData)
    {
        DetailPrototype det = new DetailPrototype();

        det.minHeight = positionData.minHeight;
        det.maxHeight = positionData.maxHeight;
        det.minWidth = positionData.minWidth;
        det.maxWidth = positionData.maxWidth;
        
        det.healthyColor = this.healthyColor;
        det.dryColor = this.dryColor;
        det.renderMode = this.renderMode;

        if (det.renderMode == DetailRenderMode.GrassBillboard)
        {
            det.usePrototypeMesh = false;
            det.prototypeTexture = this.detailTexture;
        }
        else if (det.renderMode == DetailRenderMode.VertexLit)
        {
            det.usePrototypeMesh = true;
            det.prototype = this.detailPrototype;
        }
        else if (det.renderMode == DetailRenderMode.Grass)
        {   
            if (this.usePrototypeMesh)
            {
                det.usePrototypeMesh = true;
                det.prototype = this.detailPrototype;
            }
            else
            {
                det.usePrototypeMesh = false;
                det.prototypeTexture = this.detailTexture;
            }
        }

        return det;
    }

}
