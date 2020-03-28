using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class BiomeTextureData : UpdatableData {

	public TextureLayer[] layers;

	#if UNITY_EDITOR

	private event System.Action subscribeUpdatedValues;

	public void ValidateValues() {
		for (int i = 0; i < layers.Length; i++) {
			layers[i].ValidateValues();
		}		
	}

	public void SubscribeChanges(System.Action onValidate) {
		this.subscribeUpdatedValues += onValidate;
	}

	public void UnsubscribeChanges(System.Action onValidate) {
		this.subscribeUpdatedValues -= onValidate;
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

[System.Serializable]
public class TextureLayer {
	public Texture2D texture;
	public Color tint;
	[Range(0,1)]
	public float tintStrength;
	[Range(0,1)]
	public float startHeight;
	[Range(0,1)]
	public float blendStrength;
	public float textureScale;

	public void ValidateValues() {
		
	}
}
