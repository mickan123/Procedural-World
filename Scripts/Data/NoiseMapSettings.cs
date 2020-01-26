using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseMapSettings : UpdatableData {

	public NoiseSettings noiseSettings;

	public float heightMultiplier;
	public AnimationCurve heightCurve;

	public event System.Action subscribeUpdatedValues;

	[HideInInspector]
	public int seed; // Set by global seed

	public float minHeight {
		get {
			return heightMultiplier * heightCurve.Evaluate(0);
		}
	}

	public float maxHeight {
		get {
			return heightMultiplier * heightCurve.Evaluate(1);
		}
		
	}

	#if UNITY_EDITOR

	public virtual void ValidateValues() {
		noiseSettings.ValidateValues();
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
