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

		const static uint maxTexturesPerBiome = 8;
		const static uint maxBiomeCount = 8; // Must be multiple of 4 >= actual biome count
		const static float epsilon = 1E-4;
		
		uint chunkWidth;
		
		float minHeight;
		float maxHeight;
		
		// Slope height texture variables
		UNITY_DECLARE_TEX2DARRAY(textures);
		int numTexturesPerBiome[maxBiomeCount];
		float startHeights[maxTexturesPerBiome * maxBiomeCount];
		float endHeights[maxTexturesPerBiome * maxBiomeCount];
		float startSlopes[maxTexturesPerBiome * maxBiomeCount];
		float endSlopes[maxTexturesPerBiome * maxBiomeCount];
		float3 tints[maxTexturesPerBiome * maxBiomeCount];
		float tintStrengths[maxTexturesPerBiome * maxBiomeCount];
		float blendStrength[maxTexturesPerBiome * maxBiomeCount];
		float textureScales[maxTexturesPerBiome * maxBiomeCount];
		
		// Road texture variables
		// UNITY_DECLARE_TEX2DARRAY(roadTextures);
		// int roadLayerCount;
		// float3 roadColours[maxLayerCount];
		// float roadStartHeights[maxLayerCount];
		// float roadBlends[maxLayerCount];
		// float roadColourStrengths[maxLayerCount];
		// float roadTextureScales[maxLayerCount];

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
		// y: Slope
		float4 sampleBiomeData(float3 worldPos) {
			float3 scaledWorldPos = ((worldPos + float3(chunkWidth, 0, chunkWidth)) / chunkWidth);
			return UNITY_SAMPLE_TEX2D(biomeMapTex, float2(scaledWorldPos.x, scaledWorldPos.z));
		}

		float4 sampleBiomeStrength(float3 worldPos, int textureIndex) {
			float3 scaledWorldPos = ((worldPos + float3(chunkWidth, 0, chunkWidth)) / chunkWidth);
			return UNITY_SAMPLE_TEX2DARRAY(biomeStrengthMap, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex));
		}

		float3 triplanar(float3 worldPos, float3 blendAxes, int texIndex) {
			float3 texturePos = worldPos / textureScales[texIndex];
			
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(textures, float3(texturePos.y, texturePos.z, texIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(textures, float3(texturePos.x, texturePos.z, texIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(textures, float3(texturePos.x, texturePos.y, texIndex)) * blendAxes.z;

			return xProjection + yProjection + zProjection;
		}

		float3 getBiomeTexture(int biomeIndex, float3 worldPos, float3 blendAxes, float roadStrength, float slope) {			
			
			float heightPercent = inverseLerp(minHeight, maxHeight, worldPos.y);

			// Calculate height slope index index
			float3 heightSlopeTexture = float3(0, 0, 0);
			for (int i = 0; i < maxTexturesPerBiome; i++) {
				int idx = maxTexturesPerBiome * biomeIndex + i;

				if (heightPercent > startHeights[idx] - blendStrength[idx] && heightPercent < endHeights[idx] + blendStrength[idx] 
				&& slope > startSlopes[idx] - blendStrength[idx] && slope < endSlopes[idx] + blendStrength[idx]) {
					float drawStrengthHeight = inverseLerp(-blendStrength[idx] / 2, blendStrength[idx] / 2, heightPercent - startHeights[idx]);
					float drawStrengthSlope = inverseLerp(-blendStrength[idx] / 2, blendStrength[idx] / 2, slope - startSlopes[idx]);

					float drawStrength = min(drawStrengthHeight, drawStrengthSlope);

					float3 baseColour = tints[idx] * tintStrengths[idx];
					float3 textureColour = triplanar(worldPos, blendAxes, idx) * (1 - tintStrengths[idx]);

					heightSlopeTexture = heightSlopeTexture * (1 - drawStrength) + (textureColour + baseColour) * drawStrength;
				}
			}
			
			return heightSlopeTexture; 
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
				finalTex += biomeStrengthData.x * getBiomeTexture(i, IN.worldPos, blendAxes, roadStrength, slope);
				finalTex += biomeStrengthData.y * getBiomeTexture(i + 1, IN.worldPos, blendAxes, roadStrength, slope);
				finalTex += biomeStrengthData.z * getBiomeTexture(i + 2, IN.worldPos, blendAxes, roadStrength, slope);
				finalTex += biomeStrengthData.w * getBiomeTexture(i + 3, IN.worldPos, blendAxes, roadStrength, slope);
			}

			o.Albedo = finalTex;
			
		}

		ENDCG
	}
	FallBack "Diffuse"
}
