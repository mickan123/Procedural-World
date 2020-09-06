using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(), System.Serializable]
public class ErosionSettings : UpdatableData {

    public float gravity = 4;

    [Header("Smoothing Settings")]
    public int smoothFilterWidth = 3; // Size of smoothing filter
    public int smoothWidth = 25; // Number of units from edge to smooth
	
    [Header("Hydraulic Erosion Settings")]
	public int numHydraulicErosionIterations = 50000;
    [Range(2, 10)]
    public int erosionBrushRadius = 3;

    public int maxLifetime = 30;
    public float sedimentCapacityFactor = 4;
    public float minSedimentCapacity = .01f;
    public float depositSpeed = 0.3f;
    public float erodeSpeed = 0.3f;

    [Range(0, 1)]
    public float evaporateSpeed = .01f;
    
    public float startSpeed = 1;
    public float startWater = 1;
	
    [Range(0, 1)]
    public float inertia = 0.5f;

    [Header("Thermal Erosion Settings")]
    public int numThermalErosionIterations = 10;
    public float talusAngle = 1;
    public float thermalErosionRate = 1;
    public float hardness = 1;

	[HideInInspector]
    public int seed;  // Set by global seed

    #if UNITY_EDITOR

	public void ValidateValues() {
        smoothFilterWidth = Mathf.Max(0, smoothFilterWidth);
        smoothWidth = Mathf.Max(0, smoothWidth);
	}

	protected override void OnValidate() {
		ValidateValues();
		base.OnValidate();
	}

	#endif
}
