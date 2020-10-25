using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[XNode.Node.CreateNodeMenuAttribute("")]
public class BiomeGraphNode : Node
{
    [HideInInspector] public int seed;
}
