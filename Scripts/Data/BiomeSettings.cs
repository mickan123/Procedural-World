using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class BiomeSettings : UpdatableData {

	public NoiseMapSettings humidityMapSettings;
	public NoiseMapSettings temperatureMapSettings;

	public Biome[] biomes;

	public int seed;

	#if UNITY_EDITOR

	protected override void OnValidate() {
		// TODO ensure no overlapping biome values
		base.OnValidate();
	}

	#endif

}

[System.Serializable]
public class Biome {
	public TextureData textureData;
	public NoiseMapSettings heightMapSettings;
	public int transitionDistance;

	public float sqrTransitionDistance {
		get {
			return (float)transitionDistance * (float)transitionDistance;
		}
	}

	[Range(0,1)]
	public float startHumidity;
	[Range(0,1)]
	public float endHumidity;
	[Range(0,1)]
	public float startTemperature;
	[Range(0,1)]
	public float endTemperature;

}