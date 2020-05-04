using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoadGenerator
{
    public static Road GenerateRoads(WorldSettings worldSettings, Vector2 chunkCentre, float[,] heightMap) {
        RoadSettings roadSettings = worldSettings.roadSettings;

        float mapSize = heightMap.GetLength(0);
        
        return new Road(worldSettings, 
                        heightMap,
                        new Vector2(0, 0), 
                        new Vector2(mapSize - 1, mapSize - 1),
                        chunkCentre);
    }
}
