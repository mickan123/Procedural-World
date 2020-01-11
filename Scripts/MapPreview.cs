using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPreview : MonoBehaviour {

	public Renderer textureRender;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

	public enum DrawMode { NoiseMap, Mesh, FalloffMap };
	public DrawMode drawMode;

	public MeshSettings meshSettings;
	public NoiseMapSettings heightMapSettings;
	public TextureData textureData;

	public Material terrainMaterial;

	[Range(0, MeshSettings.numSupportedLODs - 1)]
	public int EditorPreviewLOD;
	
	public bool autoUpdate;

	public void DrawTexture(Texture2D texture) {
		textureRender.sharedMaterial.mainTexture = texture;
		textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
		
		textureRender.gameObject.SetActive(true);
		meshFilter.gameObject.SetActive(false);
	}

	public void DrawMesh(MeshData meshData) {
		meshFilter.sharedMesh = meshData.CreateMesh();

		textureRender.gameObject.SetActive(false);
		meshFilter.gameObject.SetActive(true);
	}

	public void DrawMapInEditor() {
		textureData.ApplyToMaterial(terrainMaterial);
		textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
		NoiseMap heightMap = NoiseMapGenerator.GenerateNoiseMap(meshSettings.numVerticesPerLine, meshSettings.numVerticesPerLine, heightMapSettings, Vector2.zero);
		if (drawMode == DrawMode.NoiseMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
		} 
		else if (drawMode == DrawMode.Mesh) {
			DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, EditorPreviewLOD));
		}
		else if (drawMode == DrawMode.FalloffMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(new NoiseMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVerticesPerLine), 0, 1)));
		}
	}

	void OnValuesUpdated() {
		if (!Application.isPlaying) {
			DrawMapInEditor();
		}
	}

	void OnTextureValuesUpdated() {
		textureData.ApplyToMaterial(terrainMaterial);
	}

	void OnValidate() {
		if (meshSettings != null) {
			meshSettings.OnValuesUpdated -= OnValuesUpdated; // Ensure we don't subscribe multiple times
			meshSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if (heightMapSettings != null) {
			heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
			heightMapSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if (textureData != null) {
			textureData.OnValuesUpdated -= OnTextureValuesUpdated;
			textureData.OnValuesUpdated += OnTextureValuesUpdated;
		}
	}
}
