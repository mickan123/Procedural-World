using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class HydraulicErosionNode : Node
{
    [Input] public float a;
    [Output] public float b;
    public ErosionSettings settings;
    
    public override object GetValue(NodePort port) {
        
        float a = GetInputValue<float>("a", this.a);

        if (port.fieldName == "b") {
            return 2 * a;
        }
        else {
            return null;
        }
    }
}
