using System;
using UnityEngine;
using UnityEditor;

public class BiomeSettingsWindow : EditorWindow
{
    public BiomeSettings biomeSettings;
    public BiomeSettingsEditor editor;

    private Vector2 scrollPos;
    private GUISkin skin;
    private GUIStyle tabStyle;

    private void OnGUI()
    {
        if (editor == null)
        {
            editor = BiomeSettingsEditor.CreateEditor(biomeSettings) as BiomeSettingsEditor;
        }
        this.titleContent = new GUIContent("Biome Settings - " + biomeSettings.name);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        editor.OnInspectorGUI();

        EditorGUILayout.EndScrollView();

    }

    public void Init()
    {
        editor = BiomeSettingsEditor.CreateEditor(biomeSettings) as BiomeSettingsEditor;
    }

    public static BiomeSettingsWindow Open(BiomeSettings biomeSettings)
    {
        if (!biomeSettings) return null;

        BiomeSettingsWindow window = GetWindow(typeof(BiomeSettingsWindow), true, "Biome Settings - " + biomeSettings.name, true) as BiomeSettingsWindow;
        window.biomeSettings = biomeSettings;
        window.Init();
        return window;
    }
}