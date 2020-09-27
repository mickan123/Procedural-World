using System.Collections.Generic;
using UnityEngine;
using MyBox;

[CreateAssetMenu(), System.Serializable()]
public class TerrainObjectSettings : ScriptableObject {

	public TerrainObject[] terrainObjects;
	public enum SpawnMode { PoissonDiskSampling, Random };
	public SpawnMode spawnMode;
	public int numRandomSpawns = 1024;

	// Spawn radius vars
	public bool varyRadius = false;
	public float radius = 5f;
	public float minRadius = 5f;
	public float maxRadius = 50f; 
	public NoiseMapSettings noiseMapSettings;

	// Height vars
	public bool constrainHeight = false;
	public float minHeight = 0f;
	public float maxHeight = 100f;
	public AnimationCurve heightProbabilityCurve = new AnimationCurve();

	// Slope vars
	public bool constrainSlope = false;
	public float minSlope = 0f;
	public float maxSlope = 1f;

	// Scale vars
	public bool uniformScale = true;
	public bool randomScale = false;
	public float scale = 1f;
	public Vector3 nonUniformScale = new Vector3(1f, 1f, 1f);
	public Vector3 minScaleNonUniform = new Vector3(1f, 1f, 1f);
	public Vector3 maxScaleNonUniform = new Vector3(1f, 1f, 1f);
	public float minScaleUniform = 1;
	public float maxScaleUniform = 1;

	// Translation vars
	public bool randomTranslation = false;
	public Vector3 translation = new Vector3(0f, 0f, 0f);
	public Vector3 minTranslation = new Vector3(0f, 0f, 0f);
	public Vector3 maxTranslation = new Vector3(0f, 0f, 0f);

	// Rotation vars
	public bool randomRotation = false;
	public Vector3 rotation = new Vector3(0f, 0f, 0f);
	public Vector3 minRotation = new Vector3(0f, 0f, 0f);
	public Vector3 maxRotation = new Vector3(0f, 360f, 0f);

	// Other vars
	public bool hide = false;
	public bool spawnOnRoad = false;
	
	public Vector3 GetScale(System.Random prng) {
		if (this.uniformScale && !this.randomScale) {
			return new Vector3(this.scale, this.scale, this.scale);
		} 
		else if(!this.uniformScale && !this.randomScale) {
			return this.nonUniformScale;
		} 
		else if(this.uniformScale && this.randomScale) {
			float randomScale = Common.NextFloat(prng, this.minScaleUniform, this.maxScaleUniform);
			return new Vector3(randomScale, randomScale, randomScale);
		} 
		else {
			float randomX = Common.NextFloat(prng, this.minScaleNonUniform.x, this.maxScaleNonUniform.x);
			float randomY = Common.NextFloat(prng, this.minScaleNonUniform.y, this.maxScaleNonUniform.y);
			float randomZ = Common.NextFloat(prng, this.minScaleNonUniform.z, this.maxScaleNonUniform.z);
			return new Vector3(randomX, randomY, randomZ);
		}
	}

	public Vector3 GetTranslation(System.Random prng) {
		if (this.randomTranslation) {
			float randomX = Common.NextFloat(prng, this.minTranslation.x, this.maxTranslation.x);
			float randomY = Common.NextFloat(prng, this.minTranslation.y, this.maxTranslation.y);
			float randomZ = Common.NextFloat(prng, this.minTranslation.z, this.maxTranslation.z);
			return new Vector3(randomX, randomY, randomZ);
		} else {
			return this.translation;
		}
	}

	public Quaternion GetRotation(System.Random prng) {
		if (this.randomRotation) {
			float randomX = Common.NextFloat(prng, this.minRotation.x, this.maxRotation.x);
			float randomY = Common.NextFloat(prng, this.minRotation.y, this.maxRotation.y);
			float randomZ = Common.NextFloat(prng, this.minRotation.z, this.maxRotation.z);
			return Quaternion.Euler(randomX, randomY, randomZ);
		} else {
			return Quaternion.Euler(rotation.x, rotation.y, rotation.z);
		}
	}

	#if UNITY_EDITOR

	public  void OnValidate() {
		if (noiseMapSettings != null) {
			noiseMapSettings.OnValidate();
		}
		
		minRadius = Mathf.Max(minRadius, 0f);
		maxRadius = Mathf.Max(maxRadius, minRadius);

		minHeight = Mathf.Max(minHeight, 0f);
		maxHeight = Mathf.Max(maxHeight, minHeight);

		minSlope = Mathf.Max(minSlope, 0f);
		maxSlope = Mathf.Max(maxSlope, minSlope);

		for (int i = 0; i < terrainObjects.Length; i++) {
			terrainObjects[i].probability = Mathf.Clamp01(terrainObjects[i].probability);
		}

		for (int i = 1; i < terrainObjects.Length; i++) {
			terrainObjects[i].probability = Mathf.Max(terrainObjects[i - 1].probability, 
													  terrainObjects[i].probability);
		}
	}

	#endif
}

public class SpawnObject {
	public TerrainObject[] terrainObjects;
	public List<ObjectPosition> positions;
	private List<GameObject> spawnedObjects;
	private System.Random prng;
	private bool hide;
	
	public SpawnObject(TerrainObject[] terrainObjects, 
						List<ObjectPosition> positions, 
						System.Random prng,
						bool hide) {
		if (terrainObjects == null) {
			this.terrainObjects = new TerrainObject[0];
		} else {
			this.terrainObjects = terrainObjects;
		}
		this.positions = positions;
		this.prng = prng;
		this.spawnedObjects = new List<GameObject>();
		this.hide = hide;
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
					obj.transform.localScale = positions[i].scale;
					obj.SetActive(!hide);
					spawnedObjects.Add(obj);
					break;
				}
			}
		}
		if (spawnedObjects.Count > 0) {
			StaticBatchingUtility.Combine(spawnedObjects.ToArray(), spawnedObjects[0]);
		}
	}
}

[System.Serializable]
public struct TerrainObject {
	public GameObject gameObject;
	[Range(0, 1)] public float probability;

	public TerrainObject(GameObject gameObject, float probability) {
		this.gameObject = gameObject;
		this.probability = probability;
	}
}

public struct ObjectPosition {
	public Vector3 position;
	public Quaternion rotation;
	public Vector3 scale;

	public ObjectPosition(Vector3 position, Vector3 scale, Quaternion rotation) {
		this.position = position;
		this.rotation = rotation;
		this.scale = scale;
	}
}
