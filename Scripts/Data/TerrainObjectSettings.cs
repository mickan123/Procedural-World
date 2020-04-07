using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(), System.Serializable()]
public class TerrainObjectSettings : UpdatableData {
	public float minRadius = 5f;
	public float maxRadius = 50f; 

	public float minHeight = 0f;
	public float maxHeight = 100f;

	public float minSlope = 0f;
	public float maxSlope = 1f;

	public float scale = 1f;

	public TerrainObject[] terrainObjects;

	public NoiseMapSettings noiseMapSettings;

	#if UNITY_EDITOR

	public void ValidateValues() {
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
		base.OnValidate();
	}

	#endif
}

public class SpawnObject {
	public TerrainObject[] terrainObjects;
	public List<ObjectPosition> positions;

	private System.Random prng;
	private float scale;
	
	public SpawnObject(TerrainObject[] terrainObjects, List<ObjectPosition> positions, float scale, System.Random prng) {
		if (terrainObjects == null) {
			this.terrainObjects = new TerrainObject[0];
		} else {
			this.terrainObjects = terrainObjects;
		}
		this.positions = positions;
		this.scale = scale;
		this.prng = prng;
	}

	public void SetParent(Transform transform) {
		for (int i = 0; i < terrainObjects.Length; i++) {
			terrainObjects[i].gameObject.transform.parent = transform;
		}
	}

	public void Spawn(Transform parent) {
		for (int i = 0; i < positions.Count; i++) {
			
			float rand = (float)prng.NextDouble();

			for (int j = 0; j < terrainObjects.Length; j++) {
				if (rand < terrainObjects[j].probability) {
					GameObject obj = UnityEngine.Object.Instantiate(terrainObjects[j].gameObject);
					obj.transform.parent = parent;
					obj.transform.position = positions[i].position;
					obj.transform.rotation = positions[i].rotation;
					obj.transform.localScale = new Vector3(scale, scale, scale);
					break;
				}
			}
		}
	}
}

[System.Serializable]
public struct TerrainObject {
	public GameObject gameObject;
	public float probability;

	public TerrainObject(GameObject gameObject, float probability) {
		this.gameObject = gameObject;
		this.probability = probability;
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
