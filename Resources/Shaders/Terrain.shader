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
		UNITY_DECLARE_TEX2DARRAY(roadTextures);
		int numRoadTexturesPerBiome[maxBiomeCount];
		float roadStartHeights[maxTexturesPerBiome * maxBiomeCount];
		float roadEndHeights[maxTexturesPerBiome * maxBiomeCount];
		float roadStartSlopes[maxTexturesPerBiome * maxBiomeCount];
		float roadEndSlopes[maxTexturesPerBiome * maxBiomeCount];
		float3 roadTints[maxTexturesPerBiome * maxBiomeCount]; 
		float roadTintStrengths[maxTexturesPerBiome * maxBiomeCount];
		float roadBlendStrength[maxTexturesPerBiome * maxBiomeCount];
		float roadTextureScales[maxTexturesPerBiome * maxBiomeCount];

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

		float3 triplanarRoad(float3 worldPos, float3 blendAxes, int texIndex) {
			float3 texturePos = worldPos / roadTextureScales[texIndex];
			
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(roadTextures, float3(texturePos.y, texturePos.z, texIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(roadTextures, float3(texturePos.x, texturePos.z, texIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(roadTextures, float3(texturePos.x, texturePos.y, texIndex)) * blendAxes.z;

			return xProjection + yProjection + zProjection;
		}

		float3 getBiomeTexture(int biomeIndex, float3 worldPos, float3 blendAxes, float roadStrength, float slope) {			
			
			float heightPercent = inverseLerp(minHeight, maxHeight, worldPos.y);

			// Calculate height slope index index
			float3 heightSlopeTexture = float3(0, 0, 0);
			for (int i = 0; i < maxTexturesPerBiome; i++) {
				int idx = maxTexturesPerBiome * biomeIndex + i;

				if (heightPercent > startHeights[idx] - blendStrength[idx] / 2 && heightPercent < endHeights[idx] + blendStrength[idx] / 2
				&& slope > startSlopes[idx] - blendStrength[idx] / 2 && slope < endSlopes[idx] + blendStrength[idx] / 2) {
					float drawStrengthHeightStart = inverseLerp(
						-blendStrength[idx] / 2, 
						blendStrength[idx] / 2, 
						max(heightPercent, blendStrength[idx] / 2) - startHeights[idx] // Take max since otherwise values at max height would blend to nothing
					);
					float drawStrengthHeightEnd = inverseLerp(
						-blendStrength[idx] / 2, 
						blendStrength[idx] / 2, 
						endHeights[idx] - min(1 - blendStrength[idx] / 2, heightPercent) // Take min since otherwise values at max height would blend to nothing
					);
					float drawStrengthHeight = min(drawStrengthHeightStart, drawStrengthHeightEnd);

					float drawStrengthSlopeStart = inverseLerp(
						-blendStrength[idx] / 2, 
						blendStrength[idx] / 2, 
						max(slope, blendStrength[idx] / 2) - startSlopes[idx]
					);
					float drawStrengthSlopeEnd = inverseLerp(
						-blendStrength[idx] / 2, 
						blendStrength[idx] / 2, 
						endSlopes[idx] - min(1 - blendStrength[idx] / 2, slope)
					);
					float drawStrengthSlope = min(drawStrengthSlopeStart, drawStrengthSlopeEnd);

					float drawStrength = min(drawStrengthSlope, drawStrengthHeight);


					float3 baseColour = tints[idx] * tintStrengths[idx];
					float3 textureColour = triplanar(worldPos, blendAxes, idx) * (1 - tintStrengths[idx]);

					heightSlopeTexture = heightSlopeTexture * (1 - drawStrength) + (textureColour + baseColour) * drawStrength;
				}
			}

			// Calculate road texture
			float3 roadTexture = float3(0, 0, 0);
			for (int i = 0; i < maxTexturesPerBiome; i++) {
				int idx = maxTexturesPerBiome * biomeIndex + i;

				if (heightPercent > roadStartHeights[idx] - roadBlendStrength[idx] / 2 && heightPercent < roadEndHeights[idx] + roadBlendStrength[idx] / 2
				&& slope > roadStartSlopes[idx] - roadBlendStrength[idx] / 2 && slope < roadEndSlopes[idx] + roadBlendStrength[idx] / 2) {
					float drawStrengthHeightStart = inverseLerp(
						-roadBlendStrength[idx] / 2, 
						roadBlendStrength[idx] / 2, 
						max(heightPercent, roadBlendStrength[idx] / 2) - roadStartHeights[idx] // Take max since otherwise values at max height would blend to nothing
					);
					float drawStrengthHeightEnd = inverseLerp(
						-roadBlendStrength[idx] / 2, 
						roadBlendStrength[idx] / 2, 
						roadEndHeights[idx] - min(1 - roadBlendStrength[idx] / 2, heightPercent) // Take min since otherwise values at max height would blend to nothing
					);
					float drawStrengthHeight = min(drawStrengthHeightStart, drawStrengthHeightEnd);

					float drawStrengthSlopeStart = inverseLerp(
						-roadBlendStrength[idx] / 2, 
						roadBlendStrength[idx] / 2, 
						max(slope, roadBlendStrength[idx] / 2) - roadStartSlopes[idx]
					);
					float drawStrengthSlopeEnd = inverseLerp(
						-roadBlendStrength[idx] / 2, 
						roadBlendStrength[idx] / 2, 
						roadEndSlopes[idx] - min(1 - roadBlendStrength[idx] / 2, slope)
					);
					float drawStrengthSlope = min(drawStrengthSlopeStart, drawStrengthSlopeEnd);

					float drawStrength = min(drawStrengthSlope, drawStrengthHeight);

					float3 baseColour = roadTints[idx] * roadTintStrengths[idx];
					float3 textureColour = triplanarRoad(worldPos, blendAxes, idx) * (1 - roadTintStrengths[idx]);

					roadTexture = roadTexture * (1 - drawStrength) + (textureColour + baseColour) * drawStrength;
				}
			}

			return roadStrength * roadTexture + (1 - roadStrength) * heightSlopeTexture; 
		}

		void surf(Input IN, inout SurfaceOutputStandard o) {
			
			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= (blendAxes.x + blendAxes.y + blendAxes.z);

			float4 biomeData = sampleBiomeData(IN.worldPos);
			float roadStrength = biomeData.x;
			float slope = biomeData.y; 

			float3 finalTex = o.Albedo;
			for (uint i = 0; i < maxBiomeCount; i += 4) {
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
