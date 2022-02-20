using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("Objects/Rotate")]
public class RotateObjectsNode : BiomeGraphNode
{
    [Input] public ObjectPositionData positionDataIn;
    [Output] public ObjectPositionData positionDataOut;

    public bool randomRotation = false;
    public Vector3 rotation = new Vector3(0f, 0f, 0f);
    public Vector3 minRotation = new Vector3(0f, 0f, 0f);
    public Vector3 maxRotation = new Vector3(0f, 360f, 0f);

    public override object GetValue(NodePort port)
    {   
        ObjectPositionData positionData = GetInputValue<ObjectPositionData>("positionDataIn", this.positionDataIn);
        if (port.fieldName == "positionDataOut" && positionData != null)
        {
            return GetPositionData(positionData);
        }
        else
        {
            return null;
        }
    }

    private ObjectPositionData GetPositionData(ObjectPositionData positionDataIn)
    {
        System.Random prng = new System.Random(seed);
        int randIdx = prng.Next(0, this.numRandomValues);
        int length = positionDataIn.positions.Length;
        for (int i = 0; i < length; i++)
        {
            if (this.randomRotation)
            {
                float randomX = this.randomValues[randIdx] * (this.maxRotation.x - this.minRotation.x) + this.minRotation.x;
                float randomY = this.randomValues[randIdx + 1] * (this.maxRotation.y - this.minRotation.y) + this.minRotation.y;
                float randomZ = this.randomValues[randIdx + 2] * (this.maxRotation.x - this.minRotation.z) + this.minRotation.z;
                randIdx += 3;
                if (randIdx >= this.numRandomValues - 3)
                {
                    randIdx = 0;
                }
                positionDataIn.positions.rotations[i] = Quaternion.Euler(randomX, randomY, randomZ);
            }
            else
            {
                positionDataIn.positions.rotations[i] = Quaternion.Euler(this.rotation.x, this.rotation.y, this.rotation.z);
            }
        }
        return positionDataIn;
    }
}
