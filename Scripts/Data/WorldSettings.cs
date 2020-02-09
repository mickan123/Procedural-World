﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class WorldSettings : UpdatableData {

	public NoiseMapSettings humidityMapSettings;
	public NoiseMapSettings temperatureMapSettings;
	const TextureFormat textureFormat = TextureFormat.RGB565;

	[Range(0,1)]
	public float transitionDistance;
	public int seed;

	public BiomeSettings[] biomes;

	const int textureSize = 512;
	public readonly int maxLayerCount = 8;
	public readonly int maxBiomeCount = 4;
	
	public float sqrTransitionDistance {
		get {
			return (float)transitionDistance * (float)transitionDistance;
		}
	}

	public void Init() {
		InitSeeds();
	}

	public void InitSeeds() {
		System.Random prng = new System.Random(seed);
		
		temperatureMapSettings.seed = prng.Next(-100000, 100000);
		humidityMapSettings.seed = prng.Next(-100000, 100000);

		for (int i = 0; i < biomes.Length; i++) {
			biomes[i].heightMapSettings.seed = prng.Next(-100000, 100000);
		}
	}

	public void ApplyToMaterial(Material material) {

		float[] layerCounts = new float[biomes.Length];
		float[] biomeTransitionDistances = new float[biomes.Length];

		Color[] baseColours = new Color[maxLayerCount * maxBiomeCount];
		float[] baseStartHeights = new float[maxLayerCount * maxBiomeCount];
		float[] baseBlends = new float[maxLayerCount * maxBiomeCount];
		float[] baseColourStrength = new float[maxLayerCount * maxBiomeCount];
		float[] baseTextureScales = new float[maxLayerCount * maxBiomeCount];
		Texture2DArray texturesArray = new Texture2DArray(textureSize, textureSize, maxLayerCount * maxBiomeCount, textureFormat, true);

		for (int i = 0; i < biomes.Length; i++) {

			layerCounts[i] = biomes[i].textureData.layers.Length;
			biomeTransitionDistances[i] = sqrTransitionDistance;

			for (int j = 0; j < biomes[i].textureData.layers.Length; j++) {

				TextureLayer curLayer = biomes[i].textureData.layers[j];
				baseColours[i * maxLayerCount + j] = curLayer.tint;
				baseStartHeights[i * maxLayerCount + j] = curLayer.startHeight;
				baseBlends[i * maxLayerCount + j] = curLayer.blendStrength;
				baseColourStrength[i * maxLayerCount + j] = curLayer.tintStrength;
				baseTextureScales[i * maxLayerCount + j] = curLayer.textureScale;

				if (curLayer.texture != null) {
					texturesArray.SetPixels(curLayer.texture.GetPixels(), i * maxLayerCount + j);
				}	
			}
		}
		texturesArray.Apply();

		material.SetFloatArray("layerCounts", layerCounts);
		material.SetFloatArray("biomeTransitionDistances", biomeTransitionDistances);
		material.SetColorArray("baseColours", baseColours);
		material.SetFloatArray("baseStartHeights", baseStartHeights);
		material.SetFloatArray("baseBlends", baseBlends);
		material.SetFloatArray("baseColourStrengths", baseColourStrength);
		material.SetFloatArray("baseTextureScales", baseTextureScales);
		material.SetTexture("baseTextures", texturesArray);
		material.SetFloat("minHeight", minHeight);
		material.SetFloat("maxHeight", maxHeight);
	}


	public float minHeight {
		get {
			float minHeight = float.MaxValue;
			for (int i = 0; i< biomes.Length; i++) {
				if (biomes[i].heightMapSettings.minHeight < minHeight) {
					minHeight = biomes[i].heightMapSettings.minHeight;
				}
			}
			return minHeight;
		}
	}

	public float maxHeight {
		get {
			float maxHeight = float.MinValue;
			for (int i = 0; i< biomes.Length; i++) {
				if (biomes[i].heightMapSettings.maxHeight > maxHeight) {
					maxHeight = biomes[i].heightMapSettings.maxHeight;
				}
			}
			return maxHeight;
		}
	}

	#if UNITY_EDITOR

	public void SubscribeChildren() {
		humidityMapSettings.subscribeUpdatedValues += OnValidate;
		temperatureMapSettings.subscribeUpdatedValues += OnValidate;
	}

	public void UnsubscribeChildren() {
		humidityMapSettings.subscribeUpdatedValues -= OnValidate;
		temperatureMapSettings.subscribeUpdatedValues -= OnValidate;
	}

	protected override void OnValidate() {
		// TODO ensure no overlapping biome values
		humidityMapSettings.ValidateValues();
		temperatureMapSettings.ValidateValues();
		base.OnValidate();
	}

	#endif

}