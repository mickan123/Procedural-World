using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class BiomeSettings : UpdatableData {

	public NoiseSettings humidityMapSettings;
	public NoiseSettings temperatureMapSettings;

	public Biome[] biomes;

	[System.Serializable]
	public class Biome {
		public TextureData textureData;
		public NoiseMapSettings heightMapSettings;

		[Range(0,1)]
		public float startHumidity;
		[Range(0,1)]
		public float startTemperature;
		[Range(0,1)]
		public float endHumidity;
		[Range(0,1)]
		public float endTemperature;

	}

	#if UNITY_EDITOR

	protected override void OnValidate() {
		// TODO ensure no overlapping biome values
		base.OnValidate();
	}

	#endif

}
