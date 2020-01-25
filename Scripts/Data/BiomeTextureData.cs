using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class BiomeTextureData : UpdatableData {

	public TextureLayer[] layers;

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
}
