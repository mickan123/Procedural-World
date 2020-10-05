using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable, CreateAssetMenu(menuName = "Procedural Generation Settings/NoiseMapSettings")]
public class NoiseMapSettings : ScriptableObject {

	public enum NoiseType { Perlin, Simplex, SandDune }
	public NoiseType noiseType;
	public PerlinNoiseSettings perlinNoiseSettings;
	public PerlinNoiseSettings simplexNoiseSettings;
	public SandDuneSettings sandDuneSettings;
	
	[HideInInspector] public int seed; // Set by global seed
	
	#if UNITY_EDITOR

	public virtual void OnValidate() {

		if (perlinNoiseSettings != null) {
			perlinNoiseSettings.ValidateValues();
		}
		if (sandDuneSettings != null) {
			sandDuneSettings.ValidateValues();
		}
	}

	#endif
}

[System.Serializable()]
public class PerlinNoiseSettings {
	public float scale = 50;
	public int octaves = 4;
	[Range(0, 1)] public float persistance = 0.3f;
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
	[Range(0, 1)] public float xm = 0.7f;
	[Range(0, 1)] public float p = 1f; // Profile of sand dune
	private readonly float reposeAngle = 34.0f; // Maximum slope of sand is 34 degrees
	public float sigma {
		get {
			return Mathf.Tan(Mathf.Deg2Rad * reposeAngle);;
		}
	}
	
	public SandDunePeriod[] sandDunePeriods;
	public void ValidateValues() {
		if (sandDunePeriods != null) {
			for (int i = 0; i < sandDunePeriods.Length; i++) {
				if (sandDunePeriods[i] != null) {
					sandDunePeriods[i].duneOffset = Mathf.Max(0, sandDunePeriods[i].duneOffset);
					sandDunePeriods[i].maxDuneVariation = Mathf.Max(0, sandDunePeriods[i].maxDuneVariation);
					sandDunePeriods[i].duneGap = Mathf.Max(0, sandDunePeriods[i].duneGap);
					sandDunePeriods[i].duneWidth = Mathf.Max(1, sandDunePeriods[i].duneWidth);
				}
			}
		}
	}
}

[System.Serializable()]
public class SandDunePeriod {
	public float duneWidth = 25f;
	public float duneOffset = 0f;
	public float maxDuneVariation = 50f;
	public float duneGap = 3f;
	[Range(0, 1)] public float duneThreshold = 0.3f; // Height needs to be above this value to spawn a dune
}
