using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class BiomeSettings : UpdatableData {
	
	public BiomeTextureData textureData;
	public NoiseMapSettings heightMapSettings;

	public TerrainObjectSettings[] terrainObjectSettings;

	[Range(0,1)]
	public float startHumidity;
	[Range(0,1)]
	public float endHumidity;
	[Range(0,1)]
	public float startTemperature;
	[Range(0,1)]
	public float endTemperature;
	
	public event System.Action subscribeUpdatedValues;

	public void SubscribeChildren(Action onValidate) {
		for (int i = 0; i < terrainObjectSettings.Length; i++) {
			terrainObjectSettings[i].subscribeUpdatedValues += onValidate;
		}
		heightMapSettings.subscribeUpdatedValues += onValidate;
		textureData.subscribeUpdatedValues += onValidate;
    }

	public void UnsubscribeChildren(Action onValidate) {
		for (int i = 0; i < terrainObjectSettings.Length; i++) {
			terrainObjectSettings[i].subscribeUpdatedValues -= onValidate;
		}
		heightMapSettings.subscribeUpdatedValues -= onValidate;
		textureData.subscribeUpdatedValues -= onValidate;
	}

	public virtual void ValidateValues() {
		for (int i = 0; i < terrainObjectSettings.Length; i++) {
			terrainObjectSettings[i].ValidateValues();
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
		if (subscribeUpdatedValues != null) {
			subscribeUpdatedValues();
		}
		base.OnValidate();
	}

    
}
