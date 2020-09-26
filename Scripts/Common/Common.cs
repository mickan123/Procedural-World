using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class Common {

	// Thread safe random float in range [begin, end]
	public static float NextFloat(System.Random prng, float begin, float end)
	{
		float value = (float)prng.NextDouble();

		value = value * (end - begin) + begin;

		return value;
	}


	public static float[,] CopyArray(float[,] reference) {
		float[,] array = new float[reference.GetLength(0), reference.GetLength(1)];

		for (int i = 0; i < reference.GetLength(0); i++) {
			for (int j = 0; j < reference.GetLength(1); j++) {
				array[i, j] = reference[i, j];
			}
		}
		
		return array;
	}

	// Copys array values from b into a
	public static void CopyArrayValues(float[,] src, float[,] dest) {
		for (int i = 0; i < src.GetLength(0); i++) {
			for (int j = 0; j < src.GetLength(1); j++) {
				dest[i, j] = src[i, j];
			}
		}	
	}

	// Evenly smooths value from 0 to 1 in range [min, max]
	public static float SmoothRange(float value, float min, float max) {
		value = Mathf.Clamp(value, min, max);
		value = (value - min) / (max - min);
		return value;
	}

	public static Editor DisplayScriptableObjectEditor(SerializedProperty property, Object targetObject, Editor targetEditor) {
        EditorGUILayout.PropertyField(property, true, GUILayout.Height(-2));

        if (property.objectReferenceValue != null) {
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, GUIContent.none, true, EditorStyles.foldout);
        }

        if (property.isExpanded) {
            if (targetEditor == null) {
                targetEditor = Editor.CreateEditor(targetObject);
            }
            EditorGUI.indentLevel++;

            targetEditor.OnInspectorGUI();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
        EditorGUILayout.Space();

		return targetEditor;
    }

	public static float HeightFromFloatCoord(Vector2 coord, float[,] heightMap) {
        return HeightFromFloatCoord(coord.x, coord.y, heightMap);
    }

    public static float HeightFromFloatCoord(float x, float y, float[,] heightMap) {
        int maxIndex = heightMap.GetLength(0) - 1;
        int indexX = Mathf.Clamp((int)x, 0, maxIndex);
        int indexY = Mathf.Clamp((int)y, 0, maxIndex);
        
        x = x - indexX;
        y = y - indexY;

        float heightNW = heightMap[indexX, indexY];
        float heightNE = heightMap[Mathf.Min(indexX + 1, maxIndex), indexY];
        float heightSW = heightMap[indexX, Mathf.Min(indexY + 1, maxIndex)];
        float heightSE = heightMap[Mathf.Min(indexX + 1, maxIndex), Mathf.Min(indexY + 1, maxIndex)];

        float height = heightNW * (1 - x) * (1 - y) 
                     + heightNE *  x      * (1 - y) 
                     + heightSW * (1 - x) * y
                     + heightSE *  x      * y;   
        
        return height;
    }
}
