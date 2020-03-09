using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainObjectSettings : UpdatableData {
	[Range(0, 1)]
	public float probability;
	public float minRadius;
	public float maxRadius; // TODO: Enforce maxRadius > minRadius


	public GameObject terrainObject;

	public NoiseMapSettings noiseMapSettings;
}

public class TerrainObject {
	public GameObject terrainObject;
	public List<ObjectPosition> positions;
	
	public TerrainObject(GameObject terrainObject, List<ObjectPosition> positions) {
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
			obj.transform.position = positions[i].position;
			obj.transform.rotation = positions[i].rotation;
		}
	}
}

public struct ObjectPosition {
	public Vector3 position;
	public Quaternion rotation;

	public ObjectPosition(Vector3 position, Quaternion rotation) {
		this.position = position;
		this.rotation = rotation;
	}
}
