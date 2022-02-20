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
        if (positionData == null) 
        {
            return null;
        }
        System.Random prng = new System.Random(seed);
        int randIdx = prng.Next(0, this.numRandomValues);
        int length = positionData.positions.Length;
        if (this.uniformScale && !this.randomScale)
        {
            for (int i = 0; i < length; i++)
            {
                positionData.positions.scales[i] = new Vector3(this.scale, this.scale, this.scale);
            }
        }
        else if (!this.uniformScale && !this.randomScale)
        {
            for (int i = 0; i < length; i++)
            {
                positionData.positions.scales[i] = this.nonUniformScale;
            }
        }
        else if (this.randomScale)
        {
            if (this.uniformScale)
            {
                for (int i = 0; i < length; i++)
                {
                    float randValue = this.randomValues[randIdx] * (this.maxScaleUniform - this.minScaleUniform) + this.minScaleUniform;
                    randIdx++;
                    if (randIdx >= this.numRandomValues)
                    {
                        randIdx = 0;
                    }

                    positionData.positions.scales[i].x = randValue;
                    positionData.positions.scales[i].y = randValue;
                    positionData.positions.scales[i].z = randValue;
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    float randValueX = this.randomValues[randIdx] * (this.maxScaleUniform - this.minScaleUniform) + this.minScaleUniform;
                    float randValueY = this.randomValues[randIdx + 1] * (this.maxScaleUniform - this.minScaleUniform) + this.minScaleUniform;
                    float randValueZ = this.randomValues[randIdx + 2] * (this.maxScaleUniform - this.minScaleUniform) + this.minScaleUniform;

                    randIdx += 3;
                    if (randIdx >= this.numRandomValues - 3)
                    {
                        randIdx = 0;
                    }

                    positionData.positions.scales[i].x = randValueX;
                    positionData.positions.scales[i].y = randValueY;
                    positionData.positions.scales[i].z = randValueZ;
                }
            }
        }
        return positionData;
    }
}
