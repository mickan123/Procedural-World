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
        if (port.fieldName == "positionDataOut")
        {
            return GetPositionData(GetInputValue<ObjectPositionData>("positionDataIn", this.positionDataIn));
        }
        else
        {
            return null;
        }
    }

    private ObjectPositionData GetPositionData(ObjectPositionData positionData)
    {
        if (positionData == null)
        {
            return null;
        }

        System.Random prng = new System.Random(seed);
        
        for (int i = 0; i < positionData.positions.Count; i++)
        {
            if (this.randomTranslation)
            {
                positionData.positions.xCoords[i] += Common.NextFloat(prng, this.minTranslation.x, this.maxTranslation.x);
                positionData.positions.yCoords[i] += Common.NextFloat(prng, this.minTranslation.y, this.maxTranslation.y);
                positionData.positions.zCoords[i] += Common.NextFloat(prng, this.minTranslation.z, this.maxTranslation.z);
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
