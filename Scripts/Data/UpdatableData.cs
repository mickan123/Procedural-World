using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject {

	public static event System.Action mapUpdate;
	protected event System.Action onValuesUpdate;
	public bool autoUpdate;

	#if UNITY_EDITOR
	
	protected virtual void OnValidate() {
		if (autoUpdate) {
			UnityEditor.EditorApplication.update += NotifyOfUpdatedValues; // Delay call of method till shader is compiled
		}
	}

	public virtual void SubscribeChanges(System.Action callback) {
		this.onValuesUpdate += callback;
	}

	public virtual void UnsubscribeChanges(System.Action callback) {
		this.onValuesUpdate -= callback;
	}

	public void NotifyOfUpdatedValues() {
		UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
		if (onValuesUpdate != null) {
			onValuesUpdate();
		}
		if (mapUpdate != null) {
			mapUpdate();
		}
	}

	#endif
}
