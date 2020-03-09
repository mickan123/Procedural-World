using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainObjectSettings : UpdatableData {
	public float minRadius = 5f;
	public float maxRadius = 50f; 

	public float minHeight = 0f;
	public float maxHeight = 100f;

	public float minSlope = 0f;
	public float maxSlope = 1f;

	public GameObject terrainObject;

	public NoiseMapSettings noiseMapSettings;

	public event System.Action subscribeUpdatedValues;

	public virtual void ValidateValues() {
		noiseMapSettings.ValidateValues();

		minRadius = Mathf.Max(minRadius, 0f);
		maxRadius = Mathf.Max(maxRadius, minRadius);

		minHeight = Mathf.Max(minHeight, 0f);
		maxHeight = Mathf.Max(maxHeight, minHeight);

		minSlope = Mathf.Max(minSlope, 0f);
		maxSlope = Mathf.Max(maxSlope, minSlope);
	}

	protected override void OnValidate() {
		ValidateValues();

		if (subscribeUpdatedValues != null) {
			subscribeUpdatedValues();
		}
		base.OnValidate();
	}

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
