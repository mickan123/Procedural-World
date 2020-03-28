using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject {

	protected event System.Action onValuesUpdate;
	public bool autoUpdate;

	#if UNITY_EDITOR
	
	protected virtual void OnValidate() {
		if (autoUpdate) {
			UnityEditor.EditorApplication.update += NotifyOfUpdatedValues; // Delay call of method till shader is compiled
		}
	}

	public void SubscribeValuesUpdated(System.Action callback) {
		this.onValuesUpdate += callback;
	}

	public void UnsubscribeValuesUpdated(System.Action callback) {
		this.onValuesUpdate -= callback;
	}

	public void NotifyOfUpdatedValues() {
		UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
		if (onValuesUpdate != null) {
			onValuesUpdate();
		}
	}

	#endif
}
