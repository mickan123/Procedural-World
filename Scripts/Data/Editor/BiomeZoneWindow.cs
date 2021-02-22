using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BiomeZoneWindow : EditorWindow
{
    private TerrainSettings terrainSettings;
    private SerializedObject soTarget;
    private SerializedProperty biomeSettings;

    private SerializedProperty humidityMapGraph;
    private SerializedProperty temperatureMapGraph;
    private SerializedProperty transitionDistance;

    private Vector2 scrollPos;
    
    private BiomeSettings curSelectedSettings = null;
    private List<BiomeSettingsBox> biomeSettingsBoxes;

    private float selectWidth = 0.01f;

    public enum ClickType { LeftEdge, TopEdge, RightEdge, BottomEdge, TopRightCorner, BottomRightCorner, BottomLeftCorner, TopLeftCorner, Pan, Empty }
    public Vector2 previousMousePos; // Used to get initial left click position to calculate panning
    public ClickType clickType = ClickType.Empty;

    public void Init()
    {
        BiomeZoneWindow window = (BiomeZoneWindow)EditorWindow.GetWindow(typeof(BiomeZoneWindow));
        window.Show();
        
        humidityMapGraph = soTarget.FindProperty("humidityMapGraph");
        temperatureMapGraph = soTarget.FindProperty("temperatureMapGraph");
        transitionDistance = soTarget.FindProperty("transitionDistance");
        biomeSettings = soTarget.FindProperty("biomeSettings");
    }

    public static BiomeZoneWindow Open(TerrainSettings terrainSettings, SerializedObject soTarget)
    {
        if (!terrainSettings) return null;

        BiomeZoneWindow window = GetWindow(typeof(BiomeZoneWindow), true, "Biome Zone Window", true) as BiomeZoneWindow;
        window.terrainSettings = terrainSettings;
        window.soTarget = soTarget;
        window.Init();
        return window;
    }

    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Create box that will hold all biome areas
        float startX = 20;
        float width = position.width - 40;
        float startY = 20;
        float height = position.height - 140;

        Rect biomeZoneRect = new Rect(startX, startY, width, height);
        GUI.Box(biomeZoneRect, "");

        // Add axis labels
        var centreTextStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        EditorGUIUtility.RotateAroundPivot(-90, new Vector2(startX - 20, (startY + height) / 2));
        EditorGUI.LabelField(new Rect(startX, startY, width, height), "Temperature");
        EditorGUIUtility.RotateAroundPivot(90, new Vector2(startX - 20, (startY + height) / 2));
        EditorGUI.LabelField(new Rect(startX, height + EditorGUIUtility.singleLineHeight, width, EditorGUIUtility.singleLineHeight), "Humidity", centreTextStyle);

        // Create boxes for each individual biome
        this.biomeSettingsBoxes = new List<BiomeSettingsBox>();
        for (int i = 0; i < this.terrainSettings.biomeSettings.Count; i++)
        {
            BiomeSettingsBox box = new BiomeSettingsBox(this.terrainSettings.biomeSettings[i], biomeZoneRect, this.selectWidth);
            biomeSettingsBoxes.Add(box);
            box.Draw();
        }
        
        // Temperature and humidity height maps
        Rect pos = new Rect(startX, height + 3 * EditorGUIUtility.singleLineHeight, width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(pos, humidityMapGraph);
        pos.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(pos, temperatureMapGraph);
        pos.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(pos, transitionDistance);
        pos.y += 2 * EditorGUIUtility.singleLineHeight;

        this.HandleEvents(biomeZoneRect);

        if (EditorGUI.EndChangeCheck())
        {
            soTarget.ApplyModifiedProperties();
        }
        EditorGUILayout.EndScrollView();
    }

    public struct BiomeSettingsBox
    {
        public BiomeSettings biomeSettings;
        public Rect areaRect;
        public List<Rect> lineSelectRects;

        public BiomeSettingsBox(BiomeSettings settings, Rect biomeZoneRect, float selectWidth)
        {
            this.biomeSettings = settings;

            float biomeX = biomeZoneRect.x + biomeSettings.startHumidity * biomeZoneRect.width;
            float biomeWidth = biomeZoneRect.x + biomeSettings.endHumidity * biomeZoneRect.width - biomeX;

            float biomeY = biomeZoneRect.y + biomeSettings.startTemperature * biomeZoneRect.height;
            float biomeHeight = biomeZoneRect.y + biomeSettings.endTemperature * biomeZoneRect.height - biomeY;

            this.areaRect = new Rect(biomeX, biomeY, biomeWidth, biomeHeight);

            this.lineSelectRects = new List<Rect>(4);
            float selectWidthInPixels = selectWidth * Math.Min(biomeZoneRect.height, biomeZoneRect.width);

            // Create corner select rect
            this.lineSelectRects.Add(new Rect(
                this.areaRect.x - selectWidthInPixels,
                this.areaRect.y - selectWidthInPixels,
                2 * selectWidthInPixels,
                2 * selectWidthInPixels
            ));
            this.lineSelectRects.Add(new Rect(
                this.areaRect.x - selectWidthInPixels + this.areaRect.width,
                this.areaRect.y - selectWidthInPixels,
                2 * selectWidthInPixels,
                2 * selectWidthInPixels
            ));
            this.lineSelectRects.Add(new Rect(
                this.areaRect.x - selectWidthInPixels + this.areaRect.width,
                this.areaRect.y - selectWidthInPixels + this.areaRect.height,
                2 * selectWidthInPixels,
                2 * selectWidthInPixels
            ));
            this.lineSelectRects.Add(new Rect(
                this.areaRect.x - selectWidthInPixels,
                this.areaRect.y - selectWidthInPixels + this.areaRect.height,
                2 * selectWidthInPixels,
                2 * selectWidthInPixels
            ));

            // Create edge line select rects
            this.lineSelectRects.Add(new Rect(
                this.areaRect.x,
                this.areaRect.y - selectWidthInPixels,
                this.areaRect.width,
                2 * selectWidthInPixels
            ));
            this.lineSelectRects.Add(new Rect(
                this.areaRect.x + this.areaRect.width - selectWidthInPixels,
                this.areaRect.y,
                2 * selectWidthInPixels,
                this.areaRect.height
            ));
            this.lineSelectRects.Add(new Rect(
                this.areaRect.x,
                this.areaRect.y + this.areaRect.height - selectWidthInPixels,
                this.areaRect.width,
                2 * selectWidthInPixels
            ));
            this.lineSelectRects.Add(new Rect(
                this.areaRect.x - selectWidthInPixels,
                this.areaRect.y,
                2 * selectWidthInPixels,
                this.areaRect.height
            ));
        }

        public void Draw()
        {
            Handles.DrawLine(
                new Vector3(this.areaRect.x, this.areaRect.y),
                new Vector3(this.areaRect.x + this.areaRect.width, this.areaRect.y)
            );
            Handles.DrawLine(
                new Vector3(this.areaRect.x + this.areaRect.width, this.areaRect.y),
                new Vector3(this.areaRect.x + this.areaRect.width, this.areaRect.y + this.areaRect.height)
            );
            Handles.DrawLine(
                new Vector3(this.areaRect.x + this.areaRect.width, this.areaRect.y + this.areaRect.height),
                new Vector3(this.areaRect.x, this.areaRect.y + this.areaRect.height)
            );
            Handles.DrawLine(
                new Vector3(this.areaRect.x, this.areaRect.y + this.areaRect.height),
                new Vector3(this.areaRect.x, this.areaRect.y)
            );

            EditorGUIUtility.AddCursorRect(this.lineSelectRects[0], MouseCursor.ResizeUpLeft);
            EditorGUIUtility.AddCursorRect(this.lineSelectRects[1], MouseCursor.ResizeUpRight);
            EditorGUIUtility.AddCursorRect(this.lineSelectRects[2], MouseCursor.ResizeUpLeft);
            EditorGUIUtility.AddCursorRect(this.lineSelectRects[3], MouseCursor.ResizeUpRight);

            EditorGUIUtility.AddCursorRect(this.lineSelectRects[4], MouseCursor.ResizeVertical);
            EditorGUIUtility.AddCursorRect(this.lineSelectRects[5], MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(this.lineSelectRects[6], MouseCursor.ResizeVertical);
            EditorGUIUtility.AddCursorRect(this.lineSelectRects[7], MouseCursor.ResizeHorizontal);

            EditorGUIUtility.AddCursorRect(this.areaRect, MouseCursor.Pan);

            var centreTextStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            EditorGUI.LabelField(this.areaRect, biomeSettings.name, centreTextStyle);
        }

        // Get clicktype from index in lineSelectRects
        public static ClickType GetClickType(int i) 
        {
            ClickType[] clickTypes = { 
                ClickType.TopLeftCorner, ClickType.TopRightCorner, ClickType.BottomRightCorner, ClickType.BottomLeftCorner,
                ClickType.TopEdge, ClickType.RightEdge, ClickType.BottomEdge, ClickType.LeftEdge
            };
            
            if (i > clickTypes.Length)
            {
                return ClickType.Empty;
            }
            else
            {
                return clickTypes[i];
            }
        }
    }

    private void HandleEvents(Rect biomeZoneRect)
    {
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            this.LeftMouseDown();
        }
        if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
        {
            this.LeftMouseUp(biomeZoneRect);
        }
        if (Event.current.type == EventType.MouseUp && Event.current.button == 1)
        {
            // Right click
        }
        if (Event.current.type == EventType.MouseDrag)
        {
            this.HandleMouseDrag(biomeZoneRect);
        }
    }

    private void LeftMouseDown()
    {
        Vector2 mousePos = Event.current.mousePosition;
        
        for (int i = 0; i < this.biomeSettingsBoxes.Count; i++) 
        {
            if (this.biomeSettingsBoxes[i].areaRect.Contains(mousePos))
            {
                this.curSelectedSettings = this.biomeSettingsBoxes[i].biomeSettings;
                this.clickType = ClickType.Pan;
            }
            for (int j = 0; j < this.biomeSettingsBoxes[i].lineSelectRects.Count; j++)
            {
                if (this.biomeSettingsBoxes[i].lineSelectRects[j].Contains(mousePos))
                {
                    this.curSelectedSettings = this.biomeSettingsBoxes[i].biomeSettings;
                    this.clickType = BiomeSettingsBox.GetClickType(j);
                    break;
                }
            }
        }
        this.previousMousePos = mousePos;
    }

    private void HandleMouseDrag(Rect biomeZoneRect)
    {
        if (this.curSelectedSettings == null)
        {
            return;
        }

        Vector2 pos = MousePosToHumidityTemperature(Event.current.mousePosition, biomeZoneRect);
        float humidity = pos.x;
        float temperature = pos.y;
        
        float curStartHumidity = this.curSelectedSettings.startHumidity;
        float curEndHumidity = this.curSelectedSettings.endHumidity;
        float curStartTemperature = this.curSelectedSettings.startTemperature;
        float curEndTemperature = this.curSelectedSettings.endTemperature;

        float minWidth = 2 * this.selectWidth;

        if (this.clickType == ClickType.LeftEdge || this.clickType == ClickType.TopLeftCorner || this.clickType == ClickType.BottomLeftCorner)
        {
            this.curSelectedSettings.startHumidity = Mathf.Min(humidity, this.curSelectedSettings.endHumidity - minWidth);
        }
        if (this.clickType == ClickType.RightEdge || this.clickType == ClickType.TopRightCorner || this.clickType == ClickType.BottomRightCorner)
        {
            this.curSelectedSettings.endHumidity = Mathf.Max(humidity, this.curSelectedSettings.startHumidity + minWidth);
        }
        if (this.clickType == ClickType.TopEdge || this.clickType == ClickType.TopLeftCorner || this.clickType == ClickType.TopRightCorner)
        {
            this.curSelectedSettings.startTemperature = Mathf.Min(temperature, this.curSelectedSettings.endTemperature - minWidth);
        }
        if (this.clickType == ClickType.BottomEdge || this.clickType == ClickType.BottomLeftCorner || this.clickType == ClickType.BottomRightCorner)
        {
            this.curSelectedSettings.endTemperature = Mathf.Max(temperature, this.curSelectedSettings.startTemperature + minWidth);
        }
        if (this.clickType == ClickType.Pan)
        {
            Vector2 panDelta = Event.current.mousePosition - this.previousMousePos;
            float deltaHumidity = panDelta.x / biomeZoneRect.width;
            float deltaTemperature = panDelta.y / biomeZoneRect.height;

            // Clamp it so we can't move out of bounds
            if (this.curSelectedSettings.endHumidity + deltaHumidity > 1f)
            {
                deltaHumidity = 1f - this.curSelectedSettings.endHumidity;
            }
            if (this.curSelectedSettings.startHumidity + deltaHumidity < 0f )
            {
                deltaHumidity = -this.curSelectedSettings.startHumidity;
            }
            if (this.curSelectedSettings.endTemperature + deltaTemperature > 1f)
            {
                deltaTemperature = 1f - this.curSelectedSettings.endTemperature;
            }
            if (this.curSelectedSettings.startTemperature + deltaTemperature < 0f )
            {
                deltaTemperature = -this.curSelectedSettings.startTemperature;
            }
            
            this.curSelectedSettings.startHumidity += deltaHumidity;
            this.curSelectedSettings.endHumidity += deltaHumidity;
            this.curSelectedSettings.startTemperature += deltaTemperature;
            this.curSelectedSettings.endTemperature += deltaTemperature;

            this.previousMousePos = Event.current.mousePosition;
        }

        this.curSelectedSettings.OnValidate();
        
        CheckBiomeSettingsOverlap(this.curSelectedSettings, biomeZoneRect);
        Repaint();
    }

    private Vector2 MousePosToHumidityTemperature(Vector2 mousePos, Rect biomeZoneRect)
    {
        float humidity = (mousePos.x - biomeZoneRect.x) / biomeZoneRect.width;
        float temperature = (mousePos.y - biomeZoneRect.y) / biomeZoneRect.height;

        humidity = Mathf.Clamp01(humidity);
        temperature = Mathf.Clamp01(temperature);

        return new Vector2(humidity, temperature);
    }

    private bool CheckBiomeSettingsOverlap(BiomeSettings settings, Rect biomeZoneRect)
    {
        float biomeX = biomeZoneRect.x + settings.startHumidity * biomeZoneRect.width;
        float biomeWidth = biomeZoneRect.x + settings.endHumidity * biomeZoneRect.width - biomeX;

        float biomeY = biomeZoneRect.y + settings.startTemperature * biomeZoneRect.height;
        float biomeHeight = biomeZoneRect.y + settings.endTemperature * biomeZoneRect.height - biomeY;

        Rect rect = new Rect(biomeX, biomeY, biomeWidth, biomeHeight);

        for (int i = 0; i < this.biomeSettingsBoxes.Count; i++)
        {
            if (settings == this.biomeSettingsBoxes[i].biomeSettings)
            {
                continue;
            }
            Rect overlappingRect = this.biomeSettingsBoxes[i].areaRect;
            if (rect.Overlaps(overlappingRect))
            {
                BiomeSettings overlappingSettings = this.biomeSettingsBoxes[i].biomeSettings;
                if (this.clickType == ClickType.LeftEdge)
                {
                    settings.startHumidity = overlappingSettings.endHumidity;
                }
                else if (this.clickType == ClickType.RightEdge)
                {
                    settings.endHumidity = overlappingSettings.startHumidity;
                }
                else if (this.clickType == ClickType.TopEdge)
                {
                    settings.startTemperature = overlappingSettings.endTemperature;
                }
                else if (this.clickType == ClickType.BottomEdge)
                {
                    settings.endTemperature = overlappingSettings.startTemperature;
                }
                else if (this.clickType == ClickType.TopRightCorner) 
                {   
                    float deltaHumidity = settings.endHumidity - overlappingSettings.startHumidity;
                    float deltaTemperature = overlappingSettings.endTemperature - settings.startTemperature;
                    if (deltaHumidity < deltaTemperature)
                    {
                        settings.endHumidity = overlappingSettings.startHumidity;
                    }
                    else
                    {
                        settings.startTemperature = overlappingSettings.endTemperature;
                    }
                }
                else if (this.clickType == ClickType.BottomRightCorner)
                {
                    float deltaHumidity = settings.endHumidity - overlappingSettings.startHumidity;
                    float deltaTemperature = settings.endTemperature - overlappingSettings.startTemperature;
                    if (deltaHumidity < deltaTemperature)
                    {
                        settings.endHumidity = overlappingSettings.startHumidity;
                    }
                    else
                    {
                        settings.endTemperature = overlappingSettings.startTemperature;
                    }
                }
                else if (this.clickType == ClickType.BottomLeftCorner)
                {
                    float deltaHumidity = overlappingSettings.endHumidity - settings.startHumidity;
                    float deltaTemperature = settings.endTemperature - overlappingSettings.startTemperature;
                    if (deltaHumidity < deltaTemperature)
                    {
                        settings.startHumidity = overlappingSettings.endHumidity;
                    }
                    else
                    {
                        settings.endTemperature = overlappingSettings.startTemperature;
                    }
                }
                else if (this.clickType == ClickType.TopLeftCorner)
                {
                    float deltaHumidity = overlappingSettings.endHumidity - settings.startHumidity;
                    float deltaTemperature = overlappingSettings.endTemperature - settings.startTemperature;
                    if (deltaHumidity < deltaTemperature)
                    {
                        settings.startHumidity = overlappingSettings.endHumidity;
                    }
                    else
                    {
                        settings.startTemperature = overlappingSettings.endTemperature;
                    }
                }
                else if (this.clickType == ClickType.Pan)
                {
                    float humidityCorrection = 0f;
                    float temperatureCorrection = 0f;

                    // Left line overlap
                    Rect leftLine = new Rect(rect.x, rect.y, 0, rect.height);
                    Rect rightLine = new Rect(rect.x + rect.width, rect.y, 0, rect.height);
                    Rect topLine = new Rect(rect.x, rect.y, rect.width, 0);
                    Rect bottomLine = new Rect(rect.x, rect.y + rect.height, rect.width, 0);
                    
                    /* 12 Kinds of possible overlaps
                     * 4 sides times 3 types of overlaps
                     *    _____                          ______
                     *        _|____     ____|_     ____|___
                     *       | |             |_|__      |  |
                     *       |_|____     ______|    ____|__|
                     *    _____|                        |______
                     */ 
                    if (leftLine.Overlaps(overlappingRect) && topLine.Overlaps(overlappingRect) && bottomLine.Overlaps(overlappingRect))
                    {
                        humidityCorrection = overlappingSettings.endHumidity - settings.startHumidity;
                        temperatureCorrection = float.MaxValue;
                    }
                    else if (leftLine.Overlaps(overlappingRect) && topLine.Overlaps(overlappingRect) && rightLine.Overlaps(overlappingRect))
                    {
                        temperatureCorrection = overlappingSettings.endTemperature - settings.startTemperature;
                        humidityCorrection = float.MaxValue;
                    }
                    else if (topLine.Overlaps(overlappingRect) && rightLine.Overlaps(overlappingRect) && bottomLine.Overlaps(overlappingRect))
                    {
                        humidityCorrection = overlappingSettings.startHumidity - settings.endHumidity;
                        temperatureCorrection = float.MaxValue;
                    }
                    else if (rightLine.Overlaps(overlappingRect) && bottomLine.Overlaps(overlappingRect) && leftLine.Overlaps(overlappingRect))
                    {
                        temperatureCorrection = overlappingSettings.startTemperature - settings.endTemperature;
                        humidityCorrection = float.MaxValue;
                    }
                    else if (leftLine.Overlaps(overlappingRect) && topLine.Overlaps(overlappingRect))
                    {
                        humidityCorrection = overlappingSettings.endHumidity - settings.startHumidity;
                        temperatureCorrection = overlappingSettings.endTemperature - settings.startTemperature;
                    }
                    else if (topLine.Overlaps(overlappingRect) && rightLine.Overlaps(overlappingRect))
                    {
                        temperatureCorrection = overlappingSettings.endTemperature - settings.startTemperature;
                        humidityCorrection = overlappingSettings.startHumidity - settings.endHumidity;
                    }
                    else if (rightLine.Overlaps(overlappingRect) && bottomLine.Overlaps(overlappingRect))
                    {
                        temperatureCorrection = overlappingSettings.startTemperature - settings.endTemperature;
                        humidityCorrection = overlappingSettings.startHumidity - settings.endHumidity;
                    }
                    else if (bottomLine.Overlaps(overlappingRect) && leftLine.Overlaps(overlappingRect))
                    {
                        temperatureCorrection = overlappingSettings.startTemperature - settings.endTemperature;
                        humidityCorrection = overlappingSettings.endHumidity - settings.startHumidity;
                    }
                    else if (leftLine.Overlaps(overlappingRect))
                    {
                        humidityCorrection = overlappingSettings.endHumidity - settings.startHumidity;
                        temperatureCorrection = float.MaxValue;
                    }
                    else if (rightLine.Overlaps(overlappingRect))
                    {
                        humidityCorrection = overlappingSettings.startHumidity - settings.endHumidity;
                        temperatureCorrection = float.MaxValue;
                    }
                    else if (topLine.Overlaps(overlappingRect))
                    {
                        temperatureCorrection = overlappingSettings.endTemperature - settings.startTemperature;
                        humidityCorrection = float.MaxValue;
                    }
                    else if (bottomLine.Overlaps(overlappingRect))
                    {
                        temperatureCorrection = overlappingSettings.startTemperature - settings.endTemperature;
                        humidityCorrection = float.MaxValue;
                    }

                    if (Mathf.Abs(humidityCorrection) < Mathf.Abs(temperatureCorrection))
                    {
                        settings.startHumidity += humidityCorrection;
                        settings.endHumidity += humidityCorrection;
                    }
                    else
                    {
                        settings.startTemperature += temperatureCorrection;
                        settings.endTemperature += temperatureCorrection;
                    }
                }
                return true;
            }
        }

        return false;
    }

    private bool InRange(float val, float min, float max)
    {
        return val > min && val < max;
    }

    private void LeftMouseUp(Rect biomeZoneRect)
    {
        this.curSelectedSettings = null;
        this.clickType = ClickType.Empty;
    }
}
