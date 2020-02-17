using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainObject : UpdatableData {
	[Range(0, 1)]
	public float probability;

	public GameObject terrainObject;

	// Allow us to spawn certain objects in clumps
	public NoiseMapSettings noiseMapSettings;  

	

	public void spawn(Vector3 position) {
		Instantiate(terrainObject, position, Quaternion.identity);
	}
}
