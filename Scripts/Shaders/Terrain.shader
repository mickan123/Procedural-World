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
		
		int layerCounts[maxBiomeCount];

		float3 baseColours[maxLayerCount * maxBiomeCount];
		float baseStartHeights[maxLayerCount * maxBiomeCount];
		float baseBlends[maxLayerCount * maxBiomeCount];
		float baseColourStrengths[maxLayerCount * maxBiomeCount];
		float baseTextureScales[maxLayerCount * maxBiomeCount];
		
		float minHeight;
		float maxHeight;

		UNITY_DECLARE_TEX2DARRAY(baseTextures);

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
		// x: Main biome index
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


		float3 getBiomeTexture(int biomeIndex, float3 albedo, float3 worldPos, float3 blendAxes) {
			
			int layerCount = layerCounts[biomeIndex];

			float heightPercent = inverseLerp(minHeight, maxHeight, worldPos.y);

			for (int i = 0; i < layerCount; i++) {

				int idx = maxLayerCount * biomeIndex + i;

				float drawStrength = inverseLerp(-baseBlends[idx] / 2, baseBlends[idx] / 2, heightPercent - baseStartHeights[idx]);

				float3 baseColour = baseColours[idx] * baseColourStrengths[idx];
				float3 textureColour = triplanar(worldPos, blendAxes, idx) * (1 - baseColourStrengths[idx]);

				albedo = albedo * (1 - drawStrength) + (baseColour + textureColour) * drawStrength;
			}

			return albedo;
		}

		void surf(Input IN, inout SurfaceOutputStandard o) {
			
			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= (blendAxes.x + blendAxes.y + blendAxes.z);

			float4 biomeData = sampleBiomeData(IN.worldPos);
			int mainBiome = biomeData.x;

			float3 finalTex = o.Albedo;
			for (uint i = 0; i < maxBiomeCount; i+=4) {
				float4 biomeStrengthData = sampleBiomeStrength(IN.worldPos, i / 4);
				finalTex += biomeStrengthData.x * getBiomeTexture(i, o.Albedo, IN.worldPos, blendAxes);
				finalTex += biomeStrengthData.y * getBiomeTexture(i + 1, o.Albedo, IN.worldPos, blendAxes);
				finalTex += biomeStrengthData.z * getBiomeTexture(i + 2, o.Albedo, IN.worldPos, blendAxes);
				finalTex += biomeStrengthData.w * getBiomeTexture(i + 3, o.Albedo, IN.worldPos, blendAxes);
				
			}

			o.Albedo = finalTex;
		}

		ENDCG
	}
	FallBack "Diffuse"
}
