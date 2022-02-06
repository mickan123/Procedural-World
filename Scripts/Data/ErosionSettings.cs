using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable, CreateAssetMenu(menuName = "Procedural Generation Settings/ErosionSettings")]
public class ErosionSettings : ScriptableObject
{
    public static ComputeShader erosionShader;
    [Range(1, 20)]public float gravity = 9.81f;

    public int numHydraulicErosionIterations = 1000000;

    [Range(0.1f, 3f)] public float sedimentCapacityFactor = 1;

    [Range(0, 1f)] public float timestep = 0.02f;

    [Range(0.1f, 2f)] public float sedimentDisolveFactor = 0.5f;
    [Range(0.1f, 3f)] public float sedimentDepositFactor = 1.0f;
    [Range(0, 10f)] public float sedimentSofteningFactor = 5f;


    [Range(1, 40f)] public float maxErosionDepth = 10f;
    
    [Range(0, 0.05f)] public float evaporateSpeed = .015f;

    [Range(0, 0.05f)] public float rainRate = 0.012f;

    [Range(0, 1f)] public float thermalErosionRate = 0.2f;

    [Range(0, 1f)] public float talusAngleCoeff = 0.8f;
    [Range(0, 1f)] public float talusAngleTangentBias = 0.1f;
    
    [HideInInspector]
    public int seed;  // Set by global seed

#if UNITY_EDITOR

    public void OnValidate()
    {

    }

#endif
}
