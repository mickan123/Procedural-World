using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/Translate")]
public class TranslateObjectsNode : BiomeGraphNode
{
    [Input] public ObjectPositionData positionDataIn;
    [Output] public ObjectPositionData positionDataOut;

    public bool randomTranslation;

    public Vector3 translation;

    public Vector3 minTranslation;
    public Vector3 maxTranslation;

    public override object GetValue(NodePort port)
    {
        BiomeGraph biomeGraph = this.graph as BiomeGraph;
        if (!biomeGraph.ContainsKey(System.Threading.Thread.CurrentThread) || port.fieldName != "positionDataOut" || (object)this.positionDataIn == null)
        {
            return null;
        }
        return GetPositionData(GetInputValue<ObjectPositionData>("positionDataIn", this.positionDataIn));
    }

    private ObjectPositionData GetPositionData(ObjectPositionData positionData)
    {
        System.Random prng = new System.Random(seed);
        int randIdx = prng.Next(0, this.numRandomValues);
        int length = positionData.positions.Length;
        for (int i = 0; i < length; i++)
        {
            if (this.randomTranslation)
            {
                positionData.positions.xCoords[i] += this.randomValues[randIdx] * (this.minTranslation.x - this.maxTranslation.x) + this.minTranslation.x;
                positionData.positions.yCoords[i] += this.randomValues[randIdx + 1] * (this.minTranslation.y - this.maxTranslation.y) + this.minTranslation.y;
                positionData.positions.zCoords[i] += this.randomValues[randIdx + 2] * (this.minTranslation.z - this.maxTranslation.z) + this.minTranslation.z;

                randIdx += 3;
                if (randIdx >= this.numRandomValues - 3)
                {
                    randIdx = 0;
                }
            }
            else
            {
                positionData.positions.xCoords[i] += this.translation.x;
                positionData.positions.yCoords[i] += this.translation.y;
                positionData.positions.zCoords[i] += this.translation.z;
            }
        }
        return positionData;
    }
}
