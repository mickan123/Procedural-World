using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable, CreateAssetMenu(menuName = "Procedural Generation Settings/TextureData")]
public class TextureData : ScriptableObject {

	public TextureLayer[] textureLayers;

	#if UNITY_EDITOR

	public void OnValidate() {
		if (textureLayers != null) {
			for (int i = 0; i < textureLayers.Length; i++) {
				if (textureLayers[i] != null) {
					textureLayers[i].ValidateValues();
				}
			}
		}	
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
