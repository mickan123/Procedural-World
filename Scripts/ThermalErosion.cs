using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ThermalErosion  {

	private static bool Inside(int x, int y, int mapSize) {
		return (x >= 0 && y >= 0 && x < mapSize && y < mapSize);
	}

	public static float[,] Erode(float[,] values, ErosionSettings settings) {
		
		int mapSize = values.GetLength(0);
		
		int[,] offsets = { { 1 , 0}, { 0 , 1}, { -1, 0},{ 0 , -1} };
		int numNeighbours = offsets.GetLength(0);

		for (int iter = 0; iter < settings.numThermalErosionIterations; iter++) {
			for (int x = 0; x < mapSize; x++) {
				for (int y = 0; y < mapSize; y++) {

					float sumHeightDifferences = 0;
					float[] heightDifferences = new float[numNeighbours];
					
					for (int i = 0; i < numNeighbours; i++) {
						int offsetX = x + offsets[i, 0];
						int offsetY = y + offsets[i, 1];

						if (Inside(offsetX, offsetY, mapSize)) {
							heightDifferences[i] = (values[offsetX, offsetY] - values[x, y]);
							sumHeightDifferences += Mathf.Max(0f, heightDifferences[i]);
						} else {
							heightDifferences[i] = 0f;
						}
					}
					

					for (int i = 0; i < numNeighbours; i++) {
						int offsetX = x + offsets[i, 0];
						int offsetY = y + offsets[i, 1];

						if (Inside(offsetX, offsetY, mapSize) && heightDifferences[i] > settings.talusAngle) {
							
							float volumeToBeMoved = (heightDifferences[i] / sumHeightDifferences) * settings.thermalErosionRate * settings.hardness * 0.5f;

							values[offsetX, offsetY] -= volumeToBeMoved;
							values[x, y] += volumeToBeMoved;
						}
					}
				}
			}
		}

		return values;
	}
}

// void ThermalErosion(uint3 id : SV_DispatchThreadID)
// {	
// 	float4 state = CURRENT_SAMPLE(HeightMap);	

// 	// Neighbors
// 	float4 neighborHeights = float4(
// 		TERRAIN_HEIGHT(LEFT_SAMPLE(HeightMap)),
// 		TERRAIN_HEIGHT(RIGHT_SAMPLE(HeightMap)),
// 		TERRAIN_HEIGHT(TOP_SAMPLE(HeightMap)),
// 		TERRAIN_HEIGHT(BOTTOM_SAMPLE(HeightMap))
// 	);

// 	// Overall height difference in each direction
// 	float4 heightDifference = max(0, TERRAIN_HEIGHT(state) - neighborHeights);
// 	float maxHeightDifference = max(max(heightDifference.x, heightDifference.y), max(heightDifference.z, heightDifference.w));

// 	// First, we need to compute the amount of terrain to be moved from the current cell
// 	// It is capped at [Area * MaxHeightDifference / 2] because it will oscillate if we will allow 
// 	// more mass to flow per update
// 	// ErosionRate and Hardness are just control variables to reduce the erosion where and when needed
// 	float volumeToBeMoved = _CellSize.x * _CellSize.y * maxHeightDifference * 0.5 
// 		* _ThermalErosionRate * HARDNESS(state);
	
// 	// Compute angles for every neighbor
// 	// Actually a tan(angle)
// 	// NOTE: If Cellsize.X and _Cellsize.y are different 
// 	// you need to use .x for first 2 components and .y for last 2
// 	float4 tanAngle = heightDifference / _CellSize.x;
	
// 	// We need to define a threshold for the angle to identify in which direction the mass is falling
// 	// It based on hardness of the material and some more control variables
// 	float treshold = HARDNESS(state) * _TalusAngleTangentCoeff + _TalusAngleTangentBias;
	
// 	// Next we need to set proportions that defines how much mass is transfered in each direction
// 	// Some directions will not contribute because of not enough steep angles
// 	// We are 
// 	float4 k = 0;
	
// 	if (tanAngle.x > treshold)
// 		k.x = heightDifference.x;

// 	if (tanAngle.y > treshold)
// 		k.y = heightDifference.y;

// 	if (tanAngle.z > treshold)
// 		k.z = heightDifference.z;

// 	if (tanAngle.w > treshold)
// 		k.w = heightDifference.w;	

// 	// Output flux
// 	float sumProportions = SUM_COMPS(k);
// 	float4 outputFlux = 0;

// 	if (sumProportions > 0)
// 		outputFlux = volumeToBeMoved * k / sumProportions;
		
// 	// Boundaries (uncomment thisif you want water to bounce of boundaries)						
// 	if (id.x == 0) LDIR(outputFlux) = 0;
// 	if (id.y == 0) BDIR(outputFlux) = 0;
// 	if (id.x == _Width - 1) RDIR(outputFlux) = 0;
// 	if (id.y == _Height - 1) TDIR(outputFlux) = 0;	

// 	CURRENT_SAMPLE(TerrainFluxMap) = outputFlux;
// }



