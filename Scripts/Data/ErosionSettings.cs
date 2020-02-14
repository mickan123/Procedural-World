using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ErosionSettings : UpdatableData {

    public event System.Action subscribeUpdatedValues;
	
	public int numErosionIterations = 50000;
    [Range(2, 10)]
    public int erosionBrushRadius = 3;

    public int maxLifetime = 30;
    public float sedimentCapacityFactor = 4;
    public float minSedimentCapacity = .01f;
    public float depositSpeed = 0.3f;
    public float erodeSpeed = 0.3f;

    public float evaporateSpeed = .01f;
    public float gravity = 4;
    public float startSpeed = 1;
    public float startWater = 1;
	
    [Range(0, 1)]
    public float inertia = 0.5f;

	[HideInInspector]
    public int seed;  // Set by global seed

    #if UNITY_EDITOR

	public virtual void ValidateValues() {
		
	}

	protected override void OnValidate() {
		ValidateValues();

		if (subscribeUpdatedValues != null) {
			subscribeUpdatedValues();
		}
		base.OnValidate();
	}

	#endif

}
