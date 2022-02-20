using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("")]
public class BiomeGraphNode : Node
{
    [HideInInspector] public int seed;

    protected readonly int numRandomValues = 16384;
    protected readonly float[] randomValues;

    public BiomeGraphNode() 
    {
        System.Random prng = new System.Random(this.seed);
        randomValues = new float[numRandomValues];
        for (int i = 0; i < numRandomValues; i++) 
        {
            randomValues[i] = (float)prng.NextDouble();
        }
    }
}
