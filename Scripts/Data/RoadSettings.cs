using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(), System.Serializable]
public class RoadSettings : UpdatableData
{
    public GameObject roadPrefab;

	public float width = 1f;

	public Material roadMaterial;

	public int stepSize = 3;

    #if UNITY_EDITOR

	public virtual void ValidateValues() {
        // Placeholder for custom validation
	}

	protected override void OnValidate() {
		ValidateValues();
		base.OnValidate();
	}

	#endif
}
