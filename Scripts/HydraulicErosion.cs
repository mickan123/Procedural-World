using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class HydraulicErosion {
    public static BrushValues brushValues;

    public class BrushValues {
        public int[][] erosionBrushIndices;
        public float[][] erosionBrushWeights;

        ErosionSettings  settings;

        public BrushValues(ErosionSettings settings, int mapSize) {
            this.settings = settings;
            mapSize += 2 * settings.maxLifetime;
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

    public static void Init(WorldSettings settings) {
        brushValues = new BrushValues(settings.erosionSettings, settings.meshSettings.numVerticesPerLine);
    }

    public static float[,] Erode(float[,] values, float[,] mask, WorldSettings worldSettings, BiomeInfo info, WorldGenerator worldGenerator) {

        int mapSize = values.GetLength(0);
        int numBiomes = worldSettings.biomes.Length;

        // Check if we actually perform any erosion
        bool performErosion = false;    
        for (int i = 0; i < mapSize; i++) {
            for (int j = 0; j < mapSize; j++) {
                for (int w = 0; w < numBiomes; w++) {
                    if (info.biomeStrengths[i, j, w] != 0f && worldSettings.biomes[w].hydraulicErosion) {
                        performErosion = true;
                    }
                } 
            }
        }

        if (performErosion) {
            float[,] map = new float[mapSize, mapSize];
            for (int i = 0; i < mapSize; i++) {
                for (int j = 0; j < mapSize; j++) {
                    map[i, j] = values[i, j];
                }
            }

            ErosionSettings settings = worldSettings.erosionSettings;
            System.Random prng = new System.Random(settings.seed);

            for (int iteration = 0; iteration < settings.numHydraulicErosionIterations; iteration++) {

                float initPosX = prng.Next(0, mapSize - 1);
                float initPosY = prng.Next(0, mapSize - 1);

                Drop drop = new Drop(settings, 0, initPosX, initPosY);

                ErodeDrop(drop, worldSettings, map, mapSize, worldGenerator);
            }
            
            // Weight erosion by biome strengths and whether erosion is enabled
            for (int i = 0; i < mapSize; i++) {
                for (int j = 0; j < mapSize; j++) {
                    float val = 0;
                    for (int w = 0; w < numBiomes; w++) {
                        if (worldSettings.biomes[w].hydraulicErosion) {
                            val += info.biomeStrengths[i, j, w] * map[i, j];
                        } else {
                            val += info.biomeStrengths[i, j, w] * values[i, j];
                        }
                    }

                    // Blend values according to mask
                    values[i, j] = val * mask[i, j] + (1f - mask[i, j]) * values[i, j];
                }
            }
        }

        return values;
    }

    public static void ErodeDrop(Drop drop, WorldSettings worldSettings, float[,] map, int mapSize, WorldGenerator worldGenerator) {
        ErosionSettings settings = worldSettings.erosionSettings;

        for (int lifetime = drop.lifetime; lifetime < settings.maxLifetime; lifetime ++) {
            int nodeX = (int) drop.posX;
            int nodeY = (int) drop.posY;
            int dropletIndex = nodeY * mapSize + nodeX;

            // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
            float cellOffsetX = drop.posX - nodeX;
            float cellOffsetY = drop.posY - nodeY;

            // Calculate droplet's height and direction of flow with bilinear interpolation of surrounding heights
            HeightAndGradient heightAndGradient = CalculateHeightAndGradient(map, mapSize, drop.posX, drop.posY);

            // Update the droplet's direction and position (move position 1 unit regardless of speed)
            drop.dirX = (drop.dirX * settings.inertia - heightAndGradient.gradientX * (1 - settings.inertia));
            drop.dirY = (drop.dirY * settings.inertia - heightAndGradient.gradientY * (1 - settings.inertia));

            // Normalize direction
            float len = Mathf.Sqrt(drop.dirX * drop.dirX + drop.dirY * drop.dirY);
            if (len != 0) {
                drop.dirX /= len;
                drop.dirY /= len;
            }
            drop.posX += drop.dirX;
            drop.posY += drop.dirY;
            
            // Out of map check
            if (drop.posX < 0 || drop.posX >= mapSize - 1 || drop.posY < 0 || drop.posY >= mapSize - 1) {
                break;
            }

            // Stopped moving check
            if (drop.dirX == 0 && drop.dirY == 0) {
                break;
            }

            // Find the droplet's new height and calculate the deltaHeight
            float newHeight = CalculateHeightAndGradient(map, mapSize, drop.posX, drop.posY).height;
            float deltaHeight = newHeight - heightAndGradient.height;

            // Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
            float sedimentCapacity = Mathf.Max(-deltaHeight * drop.speed * drop.water * settings.sedimentCapacityFactor, settings.minSedimentCapacity);
            
            // If carrying more sediment than capacity, or if flowing uphill:
            if (drop.sediment > sedimentCapacity || deltaHeight > 0) {
                // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, drop.sediment) : (drop.sediment - sedimentCapacity) * settings.depositSpeed;
                drop.sediment -= amountToDeposit;

                // Add the sediment to the four nodes of the current cell using bilinear interpolation
                // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                map[nodeY, nodeX] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                map[nodeY, nodeX + 1] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                map[nodeY + 1, nodeX] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                map[nodeY + 1, nodeX + 1] += amountToDeposit * cellOffsetX * cellOffsetY;
            }
            else {
                // Erode a fraction of the droplet's current carry capacity.
                // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                float amountToErode = Mathf.Min((sedimentCapacity - drop.sediment) * settings.erodeSpeed, -deltaHeight);
                
                for (int brushIndex = 0; brushIndex < brushValues.erosionBrushIndices[dropletIndex].Length; brushIndex++) {
                    int nodeIndex = brushValues.erosionBrushIndices[dropletIndex][brushIndex];
                    int brushX = nodeIndex % mapSize;
                    int brushY = nodeIndex / mapSize;

                    float weightedErodeAmount = amountToErode * brushValues.erosionBrushWeights[dropletIndex][brushIndex];
                    float deltaSediment = (map[brushY, brushX] < weightedErodeAmount) ? map[brushY, brushX] : weightedErodeAmount;
                    map[brushY, brushX] -= deltaSediment;
                    drop.sediment += deltaSediment;
                }
            }

            // Update droplet's speed and water content
            drop.speed = Mathf.Sqrt(drop.speed * drop.speed + deltaHeight * settings.gravity);
            drop.water *= (1 - settings.evaporateSpeed);
        }
    }

    struct HeightAndGradient {
        public float height;
        public float gradientX;
        public float gradientY;
    }

    private static HeightAndGradient CalculateHeightAndGradient(float[,] map, int mapSize, float posX, float posY) {
        int coordX = (int) posX;
        int coordY = (int) posY;

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = posX - coordX;
        float y = posY - coordY;

        // Calculate heights of the four nodes of the droplet's cell'
        int nodeIndexNW = coordY * mapSize + coordX;
        float heightNW = map[coordY, coordX];
        float heightNE = map[coordY, coordX + 1];
        float heightSW = map[coordY + 1, coordX];
        float heightSE = map[coordY + 1, coordX + 1];

        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;

        return new HeightAndGradient () { height = height, gradientX = gradientX, gradientY = gradientY };
    }
}

public struct Drop {
    public float dirX;
    public float dirY;
    public float posX;
    public float posY;
    public float speed;
    public float water;
    public float sediment;
    public int lifetime;

    public Drop(ErosionSettings settings, int lifetime, float posX, float posY) {
        dirX = 0;
        dirY = 0;

        speed = settings.startSpeed;
        water = settings.startWater;
        sediment = 0;

        this.lifetime = lifetime;
        this.posX = posX;
        this.posY = posY;
    }

    public Drop(Drop otherDrop) {
        this.dirX = otherDrop.dirX;
        this.dirY = otherDrop.dirY;

        this.speed = otherDrop.speed;
        this.water = otherDrop.water;
        this.sediment = otherDrop.sediment;

        this.lifetime = otherDrop.lifetime;
        this.posX = otherDrop.posX;
        this.posY = otherDrop.posY;
    }
}
