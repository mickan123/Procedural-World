using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/Scale")]
public class ScaleObjectsNode : BiomeGraphNode
{
    [Input] public ObjectPositionData positionDataIn;
    [Output] public ObjectPositionData positionDataOut;

    public bool uniformScale = true;
    public bool randomScale = false;

    public float scale = 1f;

    public Vector3 nonUniformScale = new Vector3(1f, 1f, 1f);
    public Vector3 minScaleNonUniform = new Vector3(1f, 1f, 1f);
    public Vector3 maxScaleNonUniform = new Vector3(1f, 1f, 1f);

    public float minScaleUniform = 1;
    public float maxScaleUniform = 1;

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
            positionData.positions[i].scale = this.GetScale(prng);
        }
        return positionData;
    }

    public Vector3 GetScale(System.Random prng)
    {
        if (this.uniformScale && !this.randomScale)
        {
            return new Vector3(this.scale, this.scale, this.scale);
        }
        else if (!this.uniformScale && !this.randomScale)
        {
            return this.nonUniformScale;
        }
        else if (this.uniformScale && this.randomScale)
        {
            float randomScale = Common.NextFloat(prng, this.minScaleUniform, this.maxScaleUniform);
            return new Vector3(randomScale, randomScale, randomScale);
        }
        else
        {
            float randomX = Common.NextFloat(prng, this.minScaleNonUniform.x, this.maxScaleNonUniform.x);
            float randomY = Common.NextFloat(prng, this.minScaleNonUniform.y, this.maxScaleNonUniform.y);
            float randomZ = Common.NextFloat(prng, this.minScaleNonUniform.z, this.maxScaleNonUniform.z);
            return new Vector3(randomX, randomY, randomZ);
        }
    }
}
