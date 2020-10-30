using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/Filters/HeightFilter")]
public class FilterObjectsHeightNode : BiomeGraphNode
{
    [Input] public ObjectPositionData positionDataIn;
    [Output] public ObjectPositionData positionDataOut;

    public float minHeight;
    public float maxHeight;

    public AnimationCurve heightProbabilityCurve;

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
        System.Random prng = new System.Random();
        AnimationCurve threadSafeCurve = new AnimationCurve(this.heightProbabilityCurve.keys);

        for (int i = 0; i < positionData.positions.Count; i++)
        {
            Vector3 curPoint = positionData.positions[i].position;
            float height = positionData.heightMap[(int)curPoint.x, (int)curPoint.z];
            if (height > maxHeight || height < minHeight)
            {
                positionData.positions.RemoveAt(i);
                i--;
            }
            else
            {
                float percentage = (height - minHeight) / (maxHeight - minHeight);
                float minProb = threadSafeCurve.Evaluate(percentage);
                if (Common.NextFloat(prng, 0f, 1f) > minProb)
                {
                    positionData.positions.RemoveAt(i);
                    i--;
                }
            }
        }
        return positionDataIn;
    }
}