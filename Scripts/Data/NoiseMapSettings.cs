using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseMapSettings : UpdatableData {

	public enum NoiseType { Perlin, Simplex, SandDune }
	public NoiseType noiseType;

	[Header("Perlin Noise Settings")]
	public PerlinNoiseSettings perlinNoiseSettings;

	[Header("Sand Dune Settings")]
	public SandDuneSettings sandDuneSettings;
	
	[Header("Height Settings")]
	public float heightMultiplier;
	public AnimationCurve heightCurve;

	private event System.Action subscribeUpdatedValues;

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

	public void SubscribeChanges(System.Action onValidate) {
		this.subscribeUpdatedValues += onValidate;
	}

	public void UnsubscribeChanges(System.Action onValidate) {
		this.subscribeUpdatedValues -= onValidate;
	}

	public virtual void ValidateValues() {
		perlinNoiseSettings.ValidateValues();
		sandDuneSettings.ValidateValues();
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

[System.Serializable()]
public class PerlinNoiseSettings {
	public float scale = 50;
	public int octaves = 4;
	[Range(0, 1)]
	public float persistance = 0.3f;
	public float lacunarity = 2.8f;

	public void ValidateValues() {
		scale = Mathf.Max(scale, 0.01f);
		octaves = Mathf.Max(octaves, 1);
		octaves = Mathf.Min(octaves, 10);
		lacunarity = Mathf.Max(lacunarity, 1);
		persistance = Mathf.Clamp01(persistance);
	}
}

[System.Serializable()]
public class SandDuneSettings {
	[Range(0, 1)]
	public float xm = 0.7f;
	[Range(0, 1)]
	public float p = 1f; // Profile of sand dune
	public float duneWidth = 25f;
	public float minDuneOffset = 5f;
	public float maxDuneOffset = 25f;
	public float minDuneGap = 3f;
	public float maxDuneGap = 15f;

	private readonly float reposeAngle = 34.0f; // Maximum slope of sand is 34 degrees
	public float sigma {
		get {
			return Mathf.Tan(Mathf.Deg2Rad * reposeAngle);;
		}
	}

	public void ValidateValues() {
		minDuneOffset = Mathf.Max(0, minDuneOffset);
		maxDuneOffset = Mathf.Max(minDuneOffset, maxDuneOffset);
		minDuneGap = Mathf.Max(0, minDuneGap);
		maxDuneGap = Mathf.Max(minDuneGap, maxDuneGap);
		duneWidth = Mathf.Max(1, duneWidth);
	}
}