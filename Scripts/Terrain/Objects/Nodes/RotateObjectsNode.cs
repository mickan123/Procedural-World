using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

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
        for (int i = 0; i < positionDataIn.positions.Count; i++)
        {
            positionDataIn.positions[i].rotation = this.GetRotation(prng);
        }
        return positionDataIn;
    }

    public Quaternion GetRotation(System.Random prng)
    {
        if (this.randomRotation)
        {
            float randomX = Common.NextFloat(prng, this.minRotation.x, this.maxRotation.x);
            float randomY = Common.NextFloat(prng, this.minRotation.y, this.maxRotation.y);
            float randomZ = Common.NextFloat(prng, this.minRotation.z, this.maxRotation.z);
            return Quaternion.Euler(randomX, randomY, randomZ);
        }
        else
        {
            return Quaternion.Euler(this.rotation.x, this.rotation.y, this.rotation.z);
        }
    }
}
