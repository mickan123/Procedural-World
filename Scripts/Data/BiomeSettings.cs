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

	#if UNITY_EDITOR
	
	private event System.Action validateValuesSubscription;

	public void SubscribeChanges(Action onValidate) {
		this.validateValuesSubscription += onValidate;
		for (int i = 0; i < terrainObjectSettings.Length; i++) {
			terrainObjectSettings[i].SubscribeChanges(onValidate);
		}
		heightMapSettings.SubscribeChanges(onValidate);
		textureData.SubscribeChanges(onValidate);
    }

	public void UnsubscribeChanges(Action onValidate) {
		this.validateValuesSubscription -= onValidate;
		for (int i = 0; i < terrainObjectSettings.Length; i++) {
			terrainObjectSettings[i].UnsubscribeChanges(onValidate);
		}
		heightMapSettings.UnsubscribeChanges(onValidate);
		textureData.UnsubscribeChanges(onValidate);
	}

	public void ValidateValues() {
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
		if (validateValuesSubscription != null) {
			validateValuesSubscription();
		}
		base.OnValidate();
	}

	#endif
    
}
