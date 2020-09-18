using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoadGenerator
{
    public static Road GenerateRoads(TerrainSettings terrainSettings, Vector2 chunkCentre, float[,] heightMap, BiomeInfo info) {
        RoadSettings roadSettings = terrainSettings.roadSettings;

        float mapSize = heightMap.GetLength(0);
        
        List<RoadRoute> destinations = GetRoadDestinations(mapSize, chunkCentre);
        
        return new Road(terrainSettings, 
                        heightMap,
                        info,
                        destinations,
                        chunkCentre);
    }

    public static List<RoadRoute> GetRoadDestinations(float mapSize, Vector2 chunkCentre) {
        List<RoadRoute> destinations = new List<RoadRoute>(); 

        destinations.Add(new RoadRoute(new Vector2(mapSize / 2, 0), 
                                       new Vector2(mapSize / 2, mapSize - 1)));

        destinations.Add(new RoadRoute(new Vector2(0, mapSize / 2), 
                                       new Vector2(mapSize - 1, mapSize / 2)));

        return destinations;
    }
}

public class RoadRoute {
    public Vector2 roadStart;
    public Vector2 roadEnd;

    public RoadRoute(Vector2 roadStart, Vector2 roadEnd) {
        this.roadStart = roadStart;
        this.roadEnd = roadEnd;
    }
}