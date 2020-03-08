using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainObjectSettings : UpdatableData {
	[Range(0, 1)]
	public float probability;
	public float minRadius;

	public GameObject terrainObject;

	public NoiseMapSettings noiseMapSettings;
}

public class TerrainObject {
	public GameObject terrainObject;
	public List<Vector3> positions;
	
	public TerrainObject(GameObject terrainObject, List<Vector3> positions) {
		this.terrainObject = terrainObject;
		this.positions = positions;
	}

	public void SetParent(Transform transform) {
		terrainObject.transform.parent = transform;
	}

	public void Spawn(Transform parent) {
		for (int i = 0; i < positions.Count; i++) {
			GameObject obj = Object.Instantiate(terrainObject);
			obj.transform.parent = parent;
			obj.transform.position = positions[i];
			obj.transform.rotation = Quaternion.identity;
		}
	}
}
