using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(), System.Serializable]
public class RoadSettings : ScriptableObject
{
	public TextureData roadTexture;
	public float width = 1f;
	public Material roadMaterial;
	public int stepSize = 3;
	public int smoothness = 1; // How much to smooth path
	public float blendFactor = 0.2f; // How much to blend road with existing terrain
	public float maxSlope = 1f; // Max allowed slope for road to be textured on

    #if UNITY_EDITOR

	public void OnValidate() {
		smoothness = Mathf.Max(1, smoothness);
	}

	#endif
}
