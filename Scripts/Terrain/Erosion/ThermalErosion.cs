using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ThermalErosion  {

	private static readonly int[,] offsets = { { 1 , 0}, { 0 , 1}, { -1, 0}, { 0 , -1} };

	public static float[,] Erode(float[,] values, TerrainSettings terrainSettings, BiomeInfo info) {
		
		int mapSize = values.GetLength(0);
		int numNeighbours = offsets.GetLength(0);
		float[,] erodedVals = new float[mapSize, mapSize];
		for (int i = 0; i < mapSize; i++) {
            for (int j = 0; j < mapSize; j++) {
                erodedVals[i, j] = values[i, j];
            }
        }

		ErosionSettings settings = terrainSettings.erosionSettings;

		for (int iter = 0; iter < settings.numThermalErosionIterations; iter++) {
			for (int x = 0; x < mapSize - 1; x++) { // Don't erode edge cells as otherwise chunks won't align correctly
				for (int y = 0; y < mapSize - 1; y++) {

					int biome = info.biomeMap[x, y];
					if (!terrainSettings.biomeSettings[biome].thermalErosion) {
						continue;
					}

					float sumHeightDifferences = 0;
					float[] heightDifferences = new float[numNeighbours];
					
					for (int i = 0; i < numNeighbours; i++) {
						int offsetX = x + offsets[i, 0];
						int offsetY = y + offsets[i, 1];
						if (offsetX >= 0 && offsetY >= 0 && offsetX < mapSize && offsetY < mapSize) {
							heightDifferences[i] = (erodedVals[offsetX, offsetY] - erodedVals[x, y]);
							sumHeightDifferences += Mathf.Max(0f, heightDifferences[i]);
						} else {
							heightDifferences[i] = 0f;
						}
					}
					
					for (int i = 0; i < numNeighbours; i++) {
						int offsetX = x + offsets[i, 0];
						int offsetY = y + offsets[i, 1];
						if (offsetX >= 0 && offsetY >= 0 && offsetX < mapSize && offsetY < mapSize 
						    && heightDifferences[i] > settings.talusAngle) {
							
							float volumeToBeMoved = (heightDifferences[i] / sumHeightDifferences) * settings.thermalErosionRate * settings.hardness * 0.5f;

							erodedVals[offsetX, offsetY] -= volumeToBeMoved;
							erodedVals[x, y] += volumeToBeMoved;
						}
					}
				}
			}
		}

		// Weight erosion by biome strengths and whether erosion is enabled
        int numBiomes = terrainSettings.biomeSettings.Length;
        for (int i = 2; i < mapSize - 3; i++) { // Don't erode border elements as otherwise chunks don't align correctly
            for (int j = 2; j < mapSize - 3; j++) {
                float val = 0;
                for (int w = 0; w < numBiomes; w++) {
                    if (terrainSettings.biomeSettings[w].thermalErosion) {
                        val += info.biomeStrengths[i, j, w] * erodedVals[i, j];
                    } else {
                        val += info.biomeStrengths[i, j, w] * values[i, j];
                    }
                }
                values[i, j] = val;
            }
        }

		return values;
	}
}