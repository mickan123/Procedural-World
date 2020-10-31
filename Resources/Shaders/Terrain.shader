Shader "Custom/Terrain" {
	Properties {

	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		const static uint maxLayerCount = 8;
		const static uint maxBiomeCount = 8; // Must be multiple of 4 >= actual biome count
		const static float epsilon = 1E-4;
		
		uint chunkWidth;
		
		float minHeight;
		float maxHeight;

		// Biome texture variables
		UNITY_DECLARE_TEX2DARRAY(baseTextures);
		int baseLayerCounts[maxBiomeCount];
		float3 baseColours[maxLayerCount * maxBiomeCount];
		float baseStartHeights[maxLayerCount * maxBiomeCount];
		float baseBlends[maxLayerCount * maxBiomeCount];
		float baseColourStrengths[maxLayerCount * maxBiomeCount];
		float baseTextureScales[maxLayerCount * maxBiomeCount];
		
		// Slope texture variables
		UNITY_DECLARE_TEX2DARRAY(slopeTextures);
		int slopeLayerCounts[maxBiomeCount];
		float slopeThresholds[maxBiomeCount];
		float slopeBlendRanges[maxBiomeCount];
		float3 slopeColours[maxLayerCount * maxBiomeCount];
		float slopeStartHeights[maxLayerCount * maxBiomeCount];
		float slopeBlends[maxLayerCount * maxBiomeCount];
		float slopeColourStrengths[maxLayerCount * maxBiomeCount];
		float slopeTextureScales[maxLayerCount * maxBiomeCount];
		

		// Road texture variables
		UNITY_DECLARE_TEX2DARRAY(roadTextures);
		int roadLayerCount;
		float3 roadColours[maxLayerCount];
		float roadStartHeights[maxLayerCount];
		float roadBlends[maxLayerCount];
		float roadColourStrengths[maxLayerCount];
		float roadTextureScales[maxLayerCount];

		// Per chunk vars
		UNITY_DECLARE_TEX2D(biomeMapTex);
		UNITY_DECLARE_TEX2DARRAY(biomeStrengthMap);

		struct Input {
			float3 worldPos;
			float3 worldNormal;
		};

		float inverseLerp(float a, float b, float value) {
			return saturate((value - a) / (b - a));
		}

		// Biome data texture is stored as follows:
		// x: Road strength
		float4 sampleBiomeData(float3 worldPos) {
			float3 scaledWorldPos = ((worldPos + float3(chunkWidth, 0, chunkWidth)) / chunkWidth);
			return UNITY_SAMPLE_TEX2D(biomeMapTex, float2(scaledWorldPos.x, scaledWorldPos.z));
		}

		float4 sampleBiomeStrength(float3 worldPos, int textureIndex) {
			float3 scaledWorldPos = ((worldPos + float3(chunkWidth, 0, chunkWidth)) / chunkWidth);
			return UNITY_SAMPLE_TEX2DARRAY(biomeStrengthMap, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex));
		}

		float3 triplanar(float3 worldPos, float3 blendAxes, int texIndex) {
			float3 texturePos = worldPos / baseTextureScales[texIndex];
			
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(texturePos.y, texturePos.z, texIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(texturePos.x, texturePos.z, texIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(texturePos.x, texturePos.y, texIndex)) * blendAxes.z;

			return xProjection + yProjection + zProjection;
		}

		float3 triplanarSlope(float3 worldPos, float3 blendAxes, int texIndex) {
			float3 texturePos = worldPos / slopeTextureScales[texIndex];
			
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(slopeTextures, float3(texturePos.y, texturePos.z, texIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(slopeTextures, float3(texturePos.x, texturePos.z, texIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(slopeTextures, float3(texturePos.x, texturePos.y, texIndex)) * blendAxes.z;

			return xProjection + yProjection + zProjection;
		}

		float3 triplanarRoad(float3 worldPos, float3 blendAxes, int texIndex) {
			float3 texturePos = worldPos / roadTextureScales[texIndex];
			
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(roadTextures, float3(texturePos.y, texturePos.z, texIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(roadTextures, float3(texturePos.x, texturePos.z, texIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(roadTextures, float3(texturePos.x, texturePos.y, texIndex)) * blendAxes.z;

			return xProjection + yProjection + zProjection;
		}

		float3 getBiomeTexture(int biomeIndex, float3 albedo, float3 worldPos, float3 blendAxes, float roadStrength, float slope) {			
			
			float heightPercent = inverseLerp(minHeight, maxHeight, worldPos.y);

			// Calculate biome texture
			int baseLayerCount = baseLayerCounts[biomeIndex];
			float3 biomeTexture = float3(0, 0, 0);
			for (int i = 0; i < baseLayerCount; i++) {

				int idx = maxLayerCount * biomeIndex + i;

				float drawStrength = inverseLerp(-baseBlends[idx] / 2, baseBlends[idx] / 2, heightPercent - baseStartHeights[idx]);

				float3 baseColour = baseColours[idx] * baseColourStrengths[idx];
				float3 textureColour = triplanar(worldPos, blendAxes, idx) * (1 - baseColourStrengths[idx]);

				biomeTexture = biomeTexture * (1 - drawStrength) + (baseColour + textureColour) * drawStrength;
			}

			// Calculate road texture
			float3 roadTexture = float3(0, 0, 0);
			for (int i = 0; i < roadLayerCount; i++) {
				int idx = i;

				float drawStrength = inverseLerp(-roadBlends[idx] / 2, roadBlends[idx] / 2, heightPercent - roadStartHeights[idx]);

				float3 roadColour = roadColours[idx] * roadColourStrengths[idx];
				float3 textureColour = triplanarRoad(worldPos, blendAxes, idx) * (1 - roadColourStrengths[idx]);

				roadTexture = roadTexture * (1 - drawStrength) + (roadColour + textureColour) * drawStrength;
			}

			// Blend road and base textures
			float3 nonSlopeTexture = roadStrength * roadTexture + (1 - roadStrength) * biomeTexture;

			// Calculate slope texture
			int slopeLayerCount = slopeLayerCounts[biomeIndex];
			float3 slopeTexture = float3(0, 0, 0);
			for (int i = 0; i < slopeLayerCount; i++) {

				int idx = maxLayerCount * biomeIndex + i;

				float drawStrength = inverseLerp(-slopeBlends[idx] / 2, slopeBlends[idx] / 2, heightPercent - slopeStartHeights[idx]);

				float3 slopeColour = slopeColours[idx] * slopeColourStrengths[idx];
				float3 textureColour = triplanarSlope(worldPos, blendAxes, idx) * (1 - slopeColourStrengths[idx]);

				slopeTexture = slopeTexture * (1 - drawStrength) + (slopeColour + textureColour) * drawStrength;
			}

			// Blend slope and non slope textures
			float slopeThreshold = slopeThresholds[biomeIndex];
			float slopeBlendRange = slopeBlendRanges[biomeIndex];
			float slopeDrawStrength = inverseLerp(slopeThreshold - slopeBlendRange, slopeThreshold + slopeBlendRange, slope);
			albedo = slopeDrawStrength * slopeTexture + (1 - slopeDrawStrength) * nonSlopeTexture;

			return albedo;
		}

		void surf(Input IN, inout SurfaceOutputStandard o) {
			
			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= (blendAxes.x + blendAxes.y + blendAxes.z);

			float4 biomeData = sampleBiomeData(IN.worldPos);
			float roadStrength = biomeData.x;
			float slope = biomeData.y;

			float3 finalTex = o.Albedo;
			for (uint i = 0; i < maxBiomeCount; i+=4) {
				float4 biomeStrengthData = sampleBiomeStrength(IN.worldPos, i / 4);
				finalTex += biomeStrengthData.x * getBiomeTexture(i, o.Albedo, IN.worldPos, blendAxes, roadStrength, slope);
				finalTex += biomeStrengthData.y * getBiomeTexture(i + 1, o.Albedo, IN.worldPos, blendAxes, roadStrength, slope);
				finalTex += biomeStrengthData.z * getBiomeTexture(i + 2, o.Albedo, IN.worldPos, blendAxes, roadStrength, slope);
				finalTex += biomeStrengthData.w * getBiomeTexture(i + 3, o.Albedo, IN.worldPos, blendAxes, roadStrength, slope);
			}
			o.Albedo = finalTex;
		}

		ENDCG
	}
	FallBack "Diffuse"
}
