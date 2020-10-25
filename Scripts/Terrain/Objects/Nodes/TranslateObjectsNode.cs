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
        System.Random prng = new System.Random(seed);
        for (int i = 0; i < positionData.positions.Count; i++)
        {
            positionData.positions[i].position = positionData.positions[i].position + this.GetTranslation(prng);
        }
        return positionData;
    }

    private Vector3 GetTranslation(System.Random prng)
    {
        if (this.randomTranslation)
        {
            float randomX = Common.NextFloat(prng, this.minTranslation.x, this.maxTranslation.x);
            float randomY = Common.NextFloat(prng, this.minTranslation.y, this.maxTranslation.y);
            float randomZ = Common.NextFloat(prng, this.minTranslation.z, this.maxTranslation.z);
            return new Vector3(randomX, randomY, randomZ);
        }
        else
        {
            return this.translation;
        }
    }
}
