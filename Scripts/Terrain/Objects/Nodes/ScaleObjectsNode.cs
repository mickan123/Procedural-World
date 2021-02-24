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

    private int maxRandomNums = 1000;

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
        if (this.uniformScale && !this.randomScale)
        {
            for (int i = 0; i < positionData.positions.Count; i++)
            {
                positionData.positions.scales[i] = new Vector3(this.scale, this.scale, this.scale);
            }
        }
        else if (!this.uniformScale && !this.randomScale)
        {
            for (int i = 0; i < positionData.positions.Count; i++)
            {
                positionData.positions.scales[i] = this.nonUniformScale;
            }
        }
        else if (this.randomScale)
        {
            List<float> randNums = new List<float>(maxRandomNums);
            for (int i = 0; i < maxRandomNums; i++) 
            {
                float value = (float)prng.NextDouble();

                value = value * (this.maxScaleUniform - this.minScaleUniform) + this.minScaleUniform;
                randNums.Add(value);
            }
            if (this.uniformScale)
            {
                for (int i = 0; i < positionData.positions.Count; i++)
                {
                    positionData.positions.scales[i] = new Vector3(randNums[i % maxRandomNums], randNums[i % maxRandomNums], randNums[i % maxRandomNums]);
                }
            }
            else
            {
                for (int i = 0; i < positionData.positions.Count; i++)
                {
                    positionData.positions.scales[i] = new Vector3(randNums[(i * 3) % maxRandomNums], randNums[(i * 3 + 1) % maxRandomNums], randNums[(i * 3 + 2) % maxRandomNums]);
                }
            }
        }
        return positionData;
    }
}
