﻿using UnityEngine;
using UnityEditor;

public abstract class ScriptlessEditor : Editor
{
    private static readonly string[] _dontIncludeMe = new string[]{"m_Script"};
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, _dontIncludeMe);

        serializedObject.ApplyModifiedProperties();
    }
}
