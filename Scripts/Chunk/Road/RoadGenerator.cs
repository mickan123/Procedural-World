using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoadGenerator
{
    public static Road GenerateRoads(WorldSettings worldSettings, Vector2 chunkCentre, float[,] heightMap, BiomeInfo info) {
        RoadSettings roadSettings = worldSettings.roadSettings;

        float mapSize = heightMap.GetLength(0);
        
        List<RoadRoute> destinations = GetRoadDestinations(mapSize);

        return new Road(worldSettings, 
                        heightMap,
                        info,
                        destinations,
                        chunkCentre);
    }

    public static List<RoadRoute> GetRoadDestinations(float mapSize) {
        List<RoadRoute> destinations = new List<RoadRoute>(); 

        destinations.Add(new RoadRoute(new Vector2(20, 0), 
                                       new Vector2(20, mapSize - 1)));

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