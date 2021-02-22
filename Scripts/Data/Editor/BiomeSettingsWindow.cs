using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

public class BiomeSettingsWindow : EditorWindow
{
    private BiomeSettings biomeSettings;
    private SerializedObject soTarget;

    private Vector2 scrollPos;
    private GUISkin skin;
    private GUIStyle tabStyle;

    private List<TextureSettingsBox> textureSettingsBoxes;

    private ReorderableList textureDataList;
    private Dictionary<TextureData, BiomeSettingsEditor> textureDataEditors;

    private TextureData curSelectedData;

    private float selectWidth = 0.01f;

    public enum ClickType { LeftEdge, TopEdge, RightEdge, BottomEdge, TopRightCorner, BottomRightCorner, BottomLeftCorner, TopLeftCorner, Pan, Empty }
    public Vector2 previousMousePos; // Used to get initial left click position to calculate panning
    public ClickType clickType = ClickType.Empty;

    public void Init() 
    {
        this.CreateTextureDataList();
    }

    private void OnGUI()
    {
        this.titleContent = new GUIContent("Biome Settings - " + biomeSettings.name);

        EditorGUI.BeginChangeCheck();

        // Create box that will hold all biome areas
        float xBorderBuffer = 20;
        float yBorderBuffer = 20;
        float startX = 2 * xBorderBuffer;
        float width = position.width - 3 * xBorderBuffer;
        float startY = yBorderBuffer;
        float height = Mathf.Min(position.height - 500, width);
 
        Rect textureSettingsRect = new Rect(startX, startY, width, height);
        GUI.Box(textureSettingsRect, "");

        // Add ticks to box
        int numHeightTicks = 5;
        int numSlopeTicks = 7;
        float tickwidth = 5f;
        GUIStyle tickLabelStyle = new GUIStyle();
        tickLabelStyle.fontSize = 10;
        tickLabelStyle.alignment = TextAnchor.MiddleCenter;

        // Height ticks
        Rect tickLine = new Rect(startX - tickwidth, startY, tickwidth, 1);
        for (int i = 0; i <= numHeightTicks; i++)
        {
            GUI.Box(tickLine, "");
            Rect labelRect = new Rect(tickLine.x - 20, tickLine.y - 10, 20, 20);
            float tickLabelValue = 1f - (1f / (float)numHeightTicks) * i;
            EditorGUI.LabelField(labelRect, tickLabelValue.ToString(), tickLabelStyle);
            tickLine.y += height / numHeightTicks;
        }

        // Slope ticks
        tickLine = new Rect(startX, startY + height - 1, 1, tickwidth);
        for (int i = 0; i < numSlopeTicks; i++) 
        {
            GUI.Box(tickLine, "");
            Rect labelRect = new Rect(tickLine.x - 5, tickLine.y + 5, 10, 10);
            EditorGUI.LabelField(labelRect, (i * 90 / (numSlopeTicks - 1)).ToString(), tickLabelStyle);
            tickLine.x += width / (numSlopeTicks - 1);
        }

        // Add axis labels
        var centreTextStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        EditorGUIUtility.RotateAroundPivot(-90, new Vector2(startX - xBorderBuffer, (startY + height) / 2));
        EditorGUI.LabelField(new Rect(startX - 50, startY - yBorderBuffer, width, height), "Height");
        EditorGUIUtility.RotateAroundPivot(90, new Vector2(startX - xBorderBuffer, (startY + height) / 2));
        EditorGUI.LabelField(new Rect(startX, height + 2 * EditorGUIUtility.singleLineHeight, width, EditorGUIUtility.singleLineHeight), "Slope (Degrees)", centreTextStyle);

        // Create boxes for each individual biome
        this.textureSettingsBoxes = new List<TextureSettingsBox>();
        for (int i = 0; i < this.biomeSettings.textureData.Count; i++)
        {
            if (this.biomeSettings.textureData[i] != null) 
            {   
                TextureSettingsBox box = new TextureSettingsBox(this.biomeSettings.textureData[i], textureSettingsRect, this.selectWidth);
                textureSettingsBoxes.Add(box);
                box.Draw();
            }
        }

        this.HandleEvents(textureSettingsRect);

        GUILayout.BeginArea(new Rect(
            xBorderBuffer, 
            textureSettingsRect.height + 3 * yBorderBuffer,
            position.width - 2 * xBorderBuffer, 
            position.height - textureSettingsRect.height - 2 * yBorderBuffer
        ));
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        this.textureDataList.DoLayoutList();

        EditorGUILayout.PropertyField(soTarget.FindProperty("biomeGraph"));
        EditorGUILayout.PropertyField(soTarget.FindProperty("hydraulicErosion"));
        EditorGUILayout.PropertyField(soTarget.FindProperty("thermalErosion"));
        EditorGUILayout.PropertyField(soTarget.FindProperty("allowRoads"));

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();

        if (EditorGUI.EndChangeCheck())
        {
            soTarget.ApplyModifiedProperties();
        }
    }

    private void CreateTextureDataList()
    {
        SerializedProperty textureData = soTarget.FindProperty("textureData");
        this.textureDataList = new ReorderableList(
            soTarget,
            textureData,
            true, // Draggable
            true, // Display header
            true, // Add button
            true  // Subtract butotn
        );

        textureDataList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Texture Data=");
        };

        textureDataList.drawElementCallback = (Rect rect, int index, bool active, bool focused) =>
        {
            SerializedProperty property = textureDataList.serializedProperty.GetArrayElementAtIndex(index);
            Rect pos = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.ObjectField(pos, property);
            pos.y += 2 * EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(pos, property);
        };

        textureDataList.onSelectCallback = (ReorderableList list) =>
        {
            SerializedProperty property = textureDataList.serializedProperty.GetArrayElementAtIndex(list.index);
        };

        textureDataList.elementHeightCallback = (int index) =>
        {
            return EditorGUI.GetPropertyHeight(textureDataList.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none, true) + 3 * EditorGUIUtility.singleLineHeight;
        };

        textureDataList.onAddCallback = (ReorderableList list) =>
        {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;

            TextureData settings = ScriptableObject.CreateInstance("TextureData") as TextureData;
            this.biomeSettings.textureData.Add(settings);
        };
    }

    public static BiomeSettingsWindow Open(BiomeSettings biomeSettings)
    {
        if (!biomeSettings) return null;

        BiomeSettingsWindow window = GetWindow(typeof(BiomeSettingsWindow), true, "Biome Settings - " + biomeSettings.name, true) as BiomeSettingsWindow;
        window.biomeSettings = biomeSettings;
        window.soTarget = new SerializedObject(biomeSettings);
        window.Init();
        return window;
    }

    private void HandleEvents(Rect textureSettingsRect)
    {
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            this.LeftMouseDown();
        }
        if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
        {
            this.LeftMouseUp(textureSettingsRect);
        }
        if (Event.current.type == EventType.MouseUp && Event.current.button == 1)
        {
            // Right click
        }
        if (Event.current.type == EventType.MouseDrag)
        {
            this.HandleMouseDrag(textureSettingsRect);
        }

        switch (Event.current.keyCode)
        {
            case KeyCode.Delete:
                this.biomeSettings.textureData.Remove(this.curSelectedData);
                this.curSelectedData = null;
                Repaint();
                break;
        }
    }

    private void LeftMouseDown()
    {
        Vector2 mousePos = Event.current.mousePosition;

        for (int i = 0; i < this.textureSettingsBoxes.Count; i++)
        {
            if (this.textureSettingsBoxes[i].areaRect.Contains(mousePos))
            {
                this.curSelectedData = this.textureSettingsBoxes[i].textureData;
                this.clickType = ClickType.Pan;
            }
            for (int j = 0; j < this.textureSettingsBoxes[i].lineSelectRects.Count; j++)
            {
                if (this.textureSettingsBoxes[i].lineSelectRects[j].Contains(mousePos))
                {
                    this.curSelectedData = this.textureSettingsBoxes[i].textureData;
                    this.clickType = TextureSettingsBox.GetClickType(j);
                    break;
                }
            }
        }
        this.previousMousePos = mousePos;
    }


    private void HandleMouseDrag(Rect textureSettingsRect)
    {
        if (this.curSelectedData == null)
        {
            return;
        }

        Vector2 pos = MousePosToSlopeHeight(Event.current.mousePosition, textureSettingsRect);
        float slope = pos.x;
        float height = pos.y;

        float curStartSlope = this.curSelectedData.startSlope;
        float curEndSlope = this.curSelectedData.endSlope;
        float curStartHeight = this.curSelectedData.startHeight;
        float curEndHeight = this.curSelectedData.endHeight;

        float minWidthHeight = 0.02f;
        float minWidthSlope = 3f;

        if (this.clickType == ClickType.LeftEdge || this.clickType == ClickType.TopLeftCorner || this.clickType == ClickType.BottomLeftCorner)
        {
            this.curSelectedData.startSlope = Mathf.Min(slope, this.curSelectedData.endSlope - minWidthSlope);
        }
        if (this.clickType == ClickType.RightEdge || this.clickType == ClickType.TopRightCorner || this.clickType == ClickType.BottomRightCorner)
        {
            this.curSelectedData.endSlope = Mathf.Max(slope, this.curSelectedData.startSlope + minWidthSlope);
        }
        if (this.clickType == ClickType.TopEdge || this.clickType == ClickType.TopLeftCorner || this.clickType == ClickType.TopRightCorner)
        {
            this.curSelectedData.startHeight = Mathf.Min(height, this.curSelectedData.endHeight - minWidthHeight);
        }
        if (this.clickType == ClickType.BottomEdge || this.clickType == ClickType.BottomLeftCorner || this.clickType == ClickType.BottomRightCorner)
        {
            this.curSelectedData.endHeight = Mathf.Max(height, this.curSelectedData.startHeight + minWidthHeight);
        }
        if (this.clickType == ClickType.Pan)
        {
            Vector2 panDelta = Event.current.mousePosition - this.previousMousePos;
            float deltaSlope = 90f * panDelta.x / textureSettingsRect.width;
            float deltaHeight = panDelta.y / textureSettingsRect.height;

            // Clamp it so we can't move out of bounds
            if (this.curSelectedData.endSlope + deltaSlope > 90f)
            {
                deltaSlope = 90f - this.curSelectedData.endSlope;
            }
            if (this.curSelectedData.startSlope + deltaSlope < 0f)
            {
                deltaSlope = -this.curSelectedData.startSlope;
            }
            if (this.curSelectedData.endHeight + deltaHeight > 1f)
            {
                deltaHeight = 1f - this.curSelectedData.endHeight;
            }
            if (this.curSelectedData.startHeight + deltaHeight < 0f)
            {
                deltaHeight = -this.curSelectedData.startHeight;
            }

            this.curSelectedData.startSlope += deltaSlope;
            this.curSelectedData.endSlope += deltaSlope;
            this.curSelectedData.startHeight += deltaHeight;
            this.curSelectedData.endHeight += deltaHeight;

            this.previousMousePos = Event.current.mousePosition;
        }

        this.curSelectedData.OnValidate();

        CheckTextureSettingsOverlap(this.curSelectedData, textureSettingsRect);
        Repaint();
    }

    private Vector2 MousePosToSlopeHeight(Vector2 mousePos, Rect biomeZoneRect)
    {
        float slope = 90f * (mousePos.x - biomeZoneRect.x) / biomeZoneRect.width;
        float height = (mousePos.y - biomeZoneRect.y) / biomeZoneRect.height;

        slope = Mathf.Clamp(slope, 0f, 90f);
        height = Mathf.Clamp01(height);

        return new Vector2(slope, height);
    }

    private bool CheckTextureSettingsOverlap(TextureData settings, Rect biomeZoneRect)
    {
        float biomeX = biomeZoneRect.x + (settings.startSlope / 90f) * biomeZoneRect.width;
        float biomeWidth = biomeZoneRect.x + (settings.endSlope / 90f) * biomeZoneRect.width - biomeX;

        float biomeY = biomeZoneRect.y + settings.startHeight * biomeZoneRect.height;
        float biomeHeight = biomeZoneRect.y + settings.endHeight * biomeZoneRect.height - biomeY;

        Rect rect = new Rect(biomeX, biomeY, biomeWidth, biomeHeight);

        bool overlaps = false;

        for (int i = 0; i < this.textureSettingsBoxes.Count; i++)
        {
            if (settings == this.textureSettingsBoxes[i].textureData)
            {
                continue;
            }
            Rect overlappingRect = this.textureSettingsBoxes[i].areaRect;
            if (rect.Overlaps(overlappingRect))
            {
                TextureData overlappingSettings = this.textureSettingsBoxes[i].textureData;
                if (this.clickType == ClickType.LeftEdge)
                {
                    settings.startSlope = overlappingSettings.endSlope;
                }
                else if (this.clickType == ClickType.RightEdge)
                {
                    settings.endSlope = overlappingSettings.startSlope;
                }
                else if (this.clickType == ClickType.TopEdge)
                {
                    settings.startHeight = overlappingSettings.endHeight;
                }
                else if (this.clickType == ClickType.BottomEdge)
                {
                    settings.endHeight = overlappingSettings.startHeight;
                }
                else if (this.clickType == ClickType.TopRightCorner)
                {
                    float deltaSlope = (settings.endSlope - overlappingSettings.startSlope) / 90f;
                    float deltaHeight = overlappingSettings.endHeight - settings.startHeight;
                    if (deltaSlope < deltaHeight)
                    {
                        settings.endSlope = overlappingSettings.startSlope;
                    }
                    else
                    {
                        settings.startHeight = overlappingSettings.endHeight;
                    }
                }
                else if (this.clickType == ClickType.BottomRightCorner)
                {
                    float deltaSlope = (settings.endSlope - overlappingSettings.startSlope) / 90f;
                    float deltaHeight = settings.endHeight - overlappingSettings.startHeight;
                    if (deltaSlope < deltaHeight)
                    {
                        settings.endSlope = overlappingSettings.startSlope;
                    }
                    else
                    {
                        settings.endHeight = overlappingSettings.startHeight;
                    }
                }
                else if (this.clickType == ClickType.BottomLeftCorner)
                {
                    float deltaSlope = (overlappingSettings.endSlope - settings.startSlope) / 90f;
                    float deltaHeight = settings.endHeight - overlappingSettings.startHeight;
                    if (deltaSlope < deltaHeight)
                    {
                        settings.startSlope = overlappingSettings.endSlope;
                    }
                    else
                    {
                        settings.endHeight = overlappingSettings.startHeight;
                    }
                }
                else if (this.clickType == ClickType.TopLeftCorner)
                {
                    float deltaSlope = (overlappingSettings.endSlope - settings.startSlope) / 90f;
                    float deltaHeight = overlappingSettings.endHeight - settings.startHeight;
                    if (deltaSlope < deltaHeight)
                    {
                        settings.startSlope = overlappingSettings.endSlope;
                    }
                    else
                    {
                        settings.startHeight = overlappingSettings.endHeight;
                    }
                }
                else if (this.clickType == ClickType.Pan)
                {
                    this.HandlePanOverlap(rect, settings, overlappingRect, overlappingSettings);
                }
                overlaps = true;
            }
        }

        return overlaps;
    }

    private void HandlePanOverlap(Rect rect, TextureData settings, Rect overlappingRect, TextureData overlappingSettings)
    {
        float slopeCorrection = 0f;
        float heightCorrection = 0f;

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
            slopeCorrection = overlappingSettings.endSlope - settings.startSlope;
            heightCorrection = float.MaxValue;
        }
        else if (leftLine.Overlaps(overlappingRect) && topLine.Overlaps(overlappingRect) && rightLine.Overlaps(overlappingRect))
        {
            heightCorrection = overlappingSettings.endHeight - settings.startHeight;
            slopeCorrection = float.MaxValue;
        }
        else if (topLine.Overlaps(overlappingRect) && rightLine.Overlaps(overlappingRect) && bottomLine.Overlaps(overlappingRect))
        {
            slopeCorrection = overlappingSettings.startSlope - settings.endSlope;
            heightCorrection = float.MaxValue;
        }
        else if (rightLine.Overlaps(overlappingRect) && bottomLine.Overlaps(overlappingRect) && leftLine.Overlaps(overlappingRect))
        {
            heightCorrection = overlappingSettings.startHeight - settings.endHeight;
            slopeCorrection = float.MaxValue;
        }
        else if (leftLine.Overlaps(overlappingRect) && topLine.Overlaps(overlappingRect))
        {
            slopeCorrection = overlappingSettings.endSlope - settings.startSlope;
            heightCorrection = overlappingSettings.endHeight - settings.startHeight;
        }
        else if (topLine.Overlaps(overlappingRect) && rightLine.Overlaps(overlappingRect))
        {
            heightCorrection = overlappingSettings.endHeight - settings.startHeight;
            slopeCorrection = overlappingSettings.startSlope - settings.endSlope;
        }
        else if (rightLine.Overlaps(overlappingRect) && bottomLine.Overlaps(overlappingRect))
        {
            heightCorrection = overlappingSettings.startHeight - settings.endHeight;
            slopeCorrection = overlappingSettings.startSlope - settings.endSlope;
        }
        else if (bottomLine.Overlaps(overlappingRect) && leftLine.Overlaps(overlappingRect))
        {
            heightCorrection = overlappingSettings.startHeight - settings.endHeight;
            slopeCorrection = overlappingSettings.endSlope - settings.startSlope;
        }
        else if (leftLine.Overlaps(overlappingRect))
        {
            slopeCorrection = overlappingSettings.endSlope - settings.startSlope;
            heightCorrection = float.MaxValue;
        }
        else if (rightLine.Overlaps(overlappingRect))
        {
            slopeCorrection = overlappingSettings.startSlope - settings.endSlope;
            heightCorrection = float.MaxValue;
        }
        else if (topLine.Overlaps(overlappingRect))
        {
            heightCorrection = overlappingSettings.endHeight - settings.startHeight;
            slopeCorrection = float.MaxValue;
        }
        else if (bottomLine.Overlaps(overlappingRect))
        {
            heightCorrection = overlappingSettings.startHeight - settings.endHeight;
            slopeCorrection = float.MaxValue;
        }

        if ((Mathf.Abs(slopeCorrection) / 90f) < Mathf.Abs(heightCorrection))
        {
            settings.startSlope += slopeCorrection;
            settings.endSlope += slopeCorrection;
        }
        else
        {
            settings.startHeight += heightCorrection;
            settings.endHeight += heightCorrection;
        }
    }

    private void LeftMouseUp(Rect biomeZoneRect)
    {
        // this.curSelectedData = null;
        this.clickType = ClickType.Empty;
    }

    public struct TextureSettingsBox
    {
        public TextureData textureData;
        public Rect areaRect;
        public List<Rect> lineSelectRects;

        public TextureSettingsBox(TextureData textureData, Rect textureSettingsRect, float selectWidth)
        {
            this.textureData = textureData;

            float biomeX = textureSettingsRect.x + (textureData.startSlope / 90f) * textureSettingsRect.width;
            float biomeWidth = textureSettingsRect.x + (textureData.endSlope / 90f) * textureSettingsRect.width - biomeX;

            float biomeY = textureSettingsRect.y + textureData.startHeight * textureSettingsRect.height;
            float biomeHeight = textureSettingsRect.y + textureData.endHeight * textureSettingsRect.height - biomeY;

            this.areaRect = new Rect(biomeX, biomeY, biomeWidth, biomeHeight);

            this.lineSelectRects = new List<Rect>(4);
            float selectWidthInPixels = selectWidth * Math.Min(textureSettingsRect.height, textureSettingsRect.width);

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
            if (this.textureData.texture == null)
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
            }
            else
            {
                GUI.DrawTexture(this.areaRect, this.textureData.texture);
            }

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
            EditorGUI.LabelField(this.areaRect, textureData.name, centreTextStyle);
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
}