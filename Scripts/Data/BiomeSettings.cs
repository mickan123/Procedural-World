using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BiomeSettings {
	
	public BiomeTextureData textureData;
	public NoiseMapSettings heightMapSettings;

	[Range(0,1)]
	public float startHumidity;
	[Range(0,1)]
	public float endHumidity;
	[Range(0,1)]
	public float startTemperature;
	[Range(0,1)]
	public float endTemperature;
}
