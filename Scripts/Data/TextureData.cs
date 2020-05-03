using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable, CreateAssetMenu()]
public class TextureData : UpdatableData {

	public TextureLayer[] layers;

	#if UNITY_EDITOR

	public void ValidateValues() {
		for (int i = 0; i < layers.Length; i++) {
			layers[i].ValidateValues();
		}		
	}

	protected override void OnValidate() {
		ValidateValues();
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
		// Placeholder for value validation
	}
}
