using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable, CreateAssetMenu()]
public class BiomeSettings : UpdatableData {
	
	public TextureData textureData;
	public NoiseMapSettings heightMapSettings;

	public List<TerrainObjectSettings> terrainObjectSettings = new List<TerrainObjectSettings>();

	public bool hydraulicErosion = true;
	public bool thermalErosion = true;

	public bool allowRoads = true;

	
	[Range(0,1)] public float startHumidity;
	[Range(0,1)] public float endHumidity;
	[Range(0,1)] public float startTemperature;
	[Range(0,1)] public float endTemperature;

	#if UNITY_EDITOR
	
	public void ValidateValues() {
		for (int i = 0; i < terrainObjectSettings.Count; i++) {
			if (terrainObjectSettings[i] != null) {
				terrainObjectSettings[i].ValidateValues();
			}
		}
		if (heightMapSettings != null) {
			heightMapSettings.ValidateValues();
		}
		if (textureData != null) {
			textureData.ValidateValues();
		}
	}

	protected override void OnValidate() {
		ValidateValues();
		base.OnValidate();
	}

	#endif
    
}
