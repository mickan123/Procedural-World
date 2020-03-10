using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MaxErosion {

	public static float[,] Erode(float[,] values, ErosionSettings settings) {

		int mapSize = values.GetLength(0);

		float[,] erodedValues = new float[mapSize, mapSize];
		
		if (settings.numMaxErosions == 0) {
			return values;
		}

		for (int i = 0; i < settings.numMaxErosions; i++) {
			
			for (int x = 0; x < mapSize; x++) {
				for (int y = 0; y < mapSize; y++) {
					int searchStartX = Mathf.Max(0, x - 1);
					int searchEndX = Mathf.Min(x + 1, mapSize - 1);
					int searchStartY = Mathf.Max(0, y - 1);
					int searchEndY = Mathf.Min(y + 1, mapSize - 1);

					float max = float.MinValue;
					for (int searchX = searchStartX; searchX <= searchEndX; searchX++) {
						for (int searchY = searchStartY; searchY <= searchEndY; searchY++) {
							if (values[searchX, searchY] > max) {
								max = values[searchX, searchY];
							}
						}
					}	
					erodedValues[x, y] = max;
				}
			}

			// Update values to be new eroded values
			for (int x = 0; x < mapSize; x++) {
				for (int y = 0; y < mapSize; y++) {
					values[x, y] = erodedValues[x, y];
				}
			}
		}

		

		return erodedValues;
	}
}
