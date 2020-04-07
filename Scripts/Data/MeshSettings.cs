using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(), System.Serializable]
public class MeshSettings : UpdatableData {

	public const int numSupportedLODs = 5;
	public const int numSupportedChunkSizes = 9;
	public const int numSupportedFlatshadedChunkSizes = 3;
	public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

	public int meshScale = 1;
	public bool useFlatShading;

	[Range(0, numSupportedChunkSizes - 1)]
	public int chunkSizeIndex;
	[Range(0, numSupportedFlatshadedChunkSizes - 1)]
	public int flatshadedChunkSizeIndex; 

	#if UNITY_EDITOR

	public void ValidateValues() {
		// Placeholder for custom validation for mesh settings
	}

	protected override void OnValidate() {
		ValidateValues();
		base.OnValidate();
	}

	#endif

	// Num vertices per line of a mesh rendered at LOD = 0. Includes the two extra vertices that are excluded from final mesh, but used for calculating normals. 
	public int numVerticesPerLine {
		get {
			return supportedChunkSizes[(useFlatShading) ? flatshadedChunkSizeIndex : chunkSizeIndex] + 5; // +2 for correct normal +2 for high res border +1 as we dealing with squares
		}
	}

	public int meshWorldSize {
		get {
			return (numVerticesPerLine -  3) * meshScale;
		}
	}
	
}
