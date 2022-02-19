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
            ObjectPositionData data = GetInputValue<ObjectPositionData>("positionDataIn", this.positionDataIn);
            FilterByHeight(data);
            return data;
        }
        else
        {
            return null;
        }
    }

    private void FilterByHeight(ObjectPositionData positionData)
    {
        AnimationCurve threadSafeCurve = new AnimationCurve(this.heightProbabilityCurve.keys);

        int length = positionData.positions.Length;
        for (int i = 0; i < length; i++)
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
                float randomVal = this.randomValues[i % this.numRandomValues];
                if (randomVal > minProb)
                {
                    positionData.positions.filtered[i] = true;
                }
            }
        }
    }
}
