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
            Vector3 curPoint = positionData.positions.positions[i];
            float height = positionData.heightMap[(int)curPoint.x, (int)curPoint.z];
            if (height > maxHeight || height < minHeight)
            {
                continue;
            }
            else
            {
                float percentage = (height - minHeight) / (maxHeight - minHeight);
                float minProb = threadSafeCurve.Evaluate(percentage);
                if (Common.NextFloat(prng, 0f, 1f) > minProb)
                {
                    continue;
                }
            }
            indices.Add(i);
        }

        List<Vector3> updatedPoints = new List<Vector3>(indices.Count);
        List<Vector3> updatedScales = new List<Vector3>(indices.Count);
        List<Quaternion> updatedRotations = new List<Quaternion>(indices.Count);
        for (int i = 0; i < indices.Count; i++)
        {
            updatedPoints.Add(positionData.positions.positions[indices[i]]);
            updatedScales.Add(positionData.positions.scales[indices[i]]);
            updatedRotations.Add(positionData.positions.rotations[indices[i]]);
        }
        ObjectPositions updatedPositions = new ObjectPositions(updatedPoints, updatedScales, updatedRotations);
        positionData.positions = updatedPositions;

        
        return positionData;
    }
}
