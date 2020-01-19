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
		humidityMapSettings.ValidateValues();
		temperatureMapSettings.ValidateValues();
	}

	#endif

	public float minHeight {
		get {
			float minHeight = float.MaxValue;
			for (int i = 0; i< biomes.Length; i++) {
				if (biomes[i].heightMapSettings.minHeight < minHeight) {
					minHeight = biomes[i].heightMapSettings.minHeight;
				}
			}
			return minHeight;
		}
	}

	public float maxHeight {
		get {
			float maxHeight = float.MinValue;
			for (int i = 0; i< biomes.Length; i++) {
				if (biomes[i].heightMapSettings.maxHeight > maxHeight) {
					maxHeight = biomes[i].heightMapSettings.maxHeight;
				}
			}
			return maxHeight;
		}
	}

}

[System.Serializable]
public class Biome {
	public TextureData textureData;
	public NoiseMapSettings heightMapSettings;
	[Range(0,1)]
	public float transitionDistance;

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