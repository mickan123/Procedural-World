﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class WaterErosionSimulator {


    
    private class BrushValues {
        public int[][] erosionBrushIndices;
        public float[][] erosionBrushWeights;

        public BrushValues(ErosionSettings settings, int mapSize) {
            erosionBrushIndices = new int[mapSize * mapSize][];
            erosionBrushWeights = new float[mapSize * mapSize][];

            int radius = settings.erosionBrushRadius;

            int[] xOffsets = new int[radius * radius * 4];
            int[] yOffsets = new int[radius * radius * 4];
            float[] weights = new float[radius * radius * 4];
            float weightSum = 0;
            int addIndex = 0;

            for (int i = 0; i < erosionBrushIndices.GetLength (0); i++) {
                int centreX = i % mapSize;
                int centreY = i / mapSize;

                if (centreY <= radius || centreY >= mapSize - radius || centreX <= radius + 1 || centreX >= mapSize - radius) {
                    weightSum = 0;
                    addIndex = 0;
                    for (int y = -radius; y <= radius; y++) {
                        for (int x = -radius; x <= radius; x++) {
                            float sqrDst = x * x + y * y;
                            if (sqrDst < radius * radius) {
                                int coordX = centreX + x;
                                int coordY = centreY + y;

                                if (coordX >= 0 && coordX < mapSize && coordY >= 0 && coordY < mapSize) {
                                    float weight = 1 - Mathf.Sqrt (sqrDst) / radius;
                                    weightSum += weight;
                                    weights[addIndex] = weight;
                                    xOffsets[addIndex] = x;
                                    yOffsets[addIndex] = y;
                                    addIndex++;
                                }
                            }
                        }
                    }
                }

                int numEntries = addIndex;
                erosionBrushIndices[i] = new int[numEntries];
                erosionBrushWeights[i] = new float[numEntries];

                for (int j = 0; j < numEntries; j++) {
                    erosionBrushIndices[i][j] = (yOffsets[j] + centreY) * mapSize + xOffsets[j] + centreX;
                    erosionBrushWeights[i][j] = weights[j] / weightSum;
                }
            }

        }   
    }

    public static float[,] Erode(float[,] values, ErosionSettings settings) {
        
        int mapSize = values.GetLength(0);

        BrushValues brushValues = new BrushValues(settings, mapSize);
        
        System.Random prng = new System.Random(settings.seed);
        
        for (int iteration = 0; iteration < settings.numErosionIterations; iteration++) {
            float posX = prng.Next(0, mapSize - 1);
            float posY = prng.Next(0, mapSize - 1);

            float dirX = 0;
            float dirY = 0;

            float speed = settings.startSpeed;
            float water = settings.startWater;
            float sediment = 0;

            for (int lifetime = 0; lifetime < settings.maxLifetime; lifetime ++) {
                int nodeX = (int) posX;
                int nodeY = (int) posY;
                int dropletIndex = nodeY * mapSize + nodeX;

                // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
                float cellOffsetX = posX - nodeX;
                float cellOffsetY = posY - nodeY;

                // Calculate droplet's height and direction of flow with bilinear interpolation of surrounding heights
                HeightAndGradient heightAndGradient = CalculateHeightAndGradient(values, posX, posY);

                // Update the droplet's direction and position (move position 1 unit regardless of speed)
                dirX = (dirX * settings.inertia - heightAndGradient.gradientX * (1 - settings.inertia));
                dirY = (dirY * settings.inertia - heightAndGradient.gradientY * (1 - settings.inertia));

                // Normalize direction
                float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                if (len != 0) {
                    dirX /= len;
                    dirY /= len;
                }
                posX += dirX;
                posY += dirY;

                // Stop simulating droplet if it's not moving or has flowed over edge of map
                if ((dirX == 0 && dirY == 0) || posX < 0 || posX >= mapSize - 1 || posY < 0 || posY >= mapSize - 1) {
                    break;
                }

                // Find the droplet's new height and calculate the deltaHeight
                float newHeight = CalculateHeightAndGradient(values, posX, posY).height;
                float deltaHeight = newHeight - heightAndGradient.height;

                // Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
                float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * settings.sedimentCapacityFactor, settings.minSedimentCapacity);
                
                // If carrying more sediment than capacity, or if flowing uphill:
                if (sediment > sedimentCapacity || deltaHeight > 0) {
                    // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                    float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, sediment) : (sediment - sedimentCapacity) * settings.depositSpeed;
                    sediment -= amountToDeposit;

                    // Add the sediment to the four nodes of the current cell using bilinear interpolation
                    // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                    values[nodeX, nodeY] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                    values[nodeX + 1, nodeY] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                    values[nodeX, nodeY + 1] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                    values[nodeX + 1, nodeY + 1] += amountToDeposit * cellOffsetX * cellOffsetY;
                }
                else {
                    // Erode a fraction of the droplet's current carry capacity.
                    // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                    float amountToErode = Mathf.Min((sedimentCapacity - sediment) * settings.erodeSpeed, -deltaHeight);
                    
                    for (int brushIndex = 0; brushIndex < brushValues.erosionBrushIndices[dropletIndex].Length; brushIndex++) {
                        int nodeIndex = brushValues.erosionBrushIndices[dropletIndex][brushIndex];
                        
                        int erodeX = (nodeIndex) % mapSize;
                        int erodeY = (nodeIndex) / mapSize;

                        float weightedErodeAmount = amountToErode * brushValues.erosionBrushWeights[dropletIndex][brushIndex];
                        float deltaSediment = (values[erodeX, erodeY] < weightedErodeAmount) ? values[erodeX, erodeY] : weightedErodeAmount;
                        values[erodeX, erodeY] -= deltaSediment;
                        sediment += deltaSediment;
                    }
                }

                // Update droplet's speed and water content
                speed = Mathf.Sqrt(speed * speed - deltaHeight * settings.gravity);
                water *= (1 - settings.evaporateSpeed);
            }
        }
        
        return values;
    }

    private static HeightAndGradient CalculateHeightAndGradient(float[,] map, float posX, float posY) {
        int coordX = (int) posX;
        int coordY = (int) posY;

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = posX - coordX;
        float y = posY - coordY;

        // Calculate heights of the four nodes of the droplet's cell'
        float heightNW = map[coordX, coordY];
        float heightNE = map[coordX + 1, coordY];
        float heightSW = map[coordX, coordY + 1];
        float heightSE = map[coordX + 1, coordY + 1];

        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;

        return new HeightAndGradient () { height = height, gradientX = gradientX, gradientY = gradientY };
    }


    struct HeightAndGradient {
        public float height;
        public float gradientX;
        public float gradientY;
    }
}
