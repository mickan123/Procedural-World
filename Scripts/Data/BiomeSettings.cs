using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class BiomeSettings : UpdatableData {
	
	public BiomeTextureData textureData;
	public NoiseMapSettings heightMapSettings;

	public List<TerrainObject> terrainObjects = new List<TerrainObject>();

	[Range(0,1)]
	public float startHumidity;
	[Range(0,1)]
	public float endHumidity;
	[Range(0,1)]
	public float startTemperature;
	[Range(0,1)]
	public float endTemperature;
}
