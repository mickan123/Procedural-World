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

        List<int> indices = new List<int>(positionData.positions.Count);
        for (int i = 0; i < positionData.positions.Count; i++)
        {
            float height = positionData.positions.yCoords[i];
            if (height > maxHeight || height < minHeight)
            {
                positionData.positions.filtered[i] = true;
            }
            else
            {
                float percentage = (height - minHeight) / (maxHeight - minHeight);
                float minProb = threadSafeCurve.Evaluate(percentage);
                if (Common.NextFloat(prng, 0f, 1f) > minProb)
                {
                    positionData.positions.filtered[i] = true;
                }
            }
        }
        
        return positionData;
    }
}
