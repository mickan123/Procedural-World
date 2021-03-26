using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;

public static class HydraulicErosion
{
    private static readonly bool cpuErosion = true;

    public class BrushValues
    {
        public int[][] erosionBrushIndices;
        public float[][] erosionBrushWeights;

        public List<int> brushIndexOffsets;
        public List<float> brushWeights;

        ErosionSettings settings;

        public BrushValues(ErosionSettings settings, int mapSize)
        {
            this.settings = settings;
            mapSize += 2 * settings.maxLifetime;
            erosionBrushIndices = new int[mapSize * mapSize][];
            erosionBrushWeights = new float[mapSize * mapSize][];

            int radius = settings.erosionBrushRadius;

            brushIndexOffsets = new List<int>();
            brushWeights = new List<float>();

            float weightSum = 0;
            for (int brushY = -radius; brushY <= radius; brushY++)
            {
                for (int brushX = -radius; brushX <= radius; brushX++)
                {
                    float sqrDst = brushX * brushX + brushY * brushY;
                    if (sqrDst < radius * radius)
                    {
                        brushIndexOffsets.Add(brushY * mapSize + brushX);
                        float brushWeight = 1 - Mathf.Sqrt(sqrDst) / radius;
                        weightSum += brushWeight;
                        brushWeights.Add(brushWeight);
                    }
                }
            }
            for (int i = 0; i < brushWeights.Count; i++)
            {
                brushWeights[i] /= weightSum;
            }
        }
    }

    public static void Init(TerrainSettings settings)
    {
        ErosionSettings.erosionShader = Resources.Load<ComputeShader>("Shaders/Erosion");
    }

    public static float[,] Erode(
        float[,] values, 
        TerrainSettings terrainSettings, 
        ErosionSettings erosionSettings,
        BiomeInfo info, 
        Vector2 chunkCentre
    )
    {

#if (PROFILE && UNITY_EDITOR)
        float erosionStartTime = 0f;
        if (terrainSettings.IsMainThread()) {
            erosionStartTime = Time.realtimeSinceStartup;
        }
#endif

        int mapSize = values.GetLength(0);
        int numBiomes = terrainSettings.biomeSettings.Count;

        float[] map = new float[mapSize * mapSize];
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                map[i * mapSize + j] = values[i, j];
            }
        }

        // Generate random indices to use
        int[] randomIndices = new int[erosionSettings.numHydraulicErosionIterations];
        System.Random prng = new System.Random(erosionSettings.seed);
        for (int i = 0; i < erosionSettings.numHydraulicErosionIterations; i++)
        {
            int randomX = prng.Next(erosionSettings.erosionBrushRadius, mapSize - erosionSettings.erosionBrushRadius);
            int randomY = prng.Next(erosionSettings.erosionBrushRadius, mapSize - erosionSettings.erosionBrushRadius);
            randomIndices[i] = randomY * mapSize + randomX;
        }

        bool gpuDone = false;

        if (cpuErosion)
        {            
            float[,] cpuMap = Common.CopyArray(values);
            CPUErosion(erosionSettings, mapSize, cpuMap);
            return cpuMap;
        }
        else
        {
            if (terrainSettings.IsMainThread())
            {
                GPUErosion(erosionSettings, mapSize, map, randomIndices, ref gpuDone);
            }
            else
            {
                Dispatcher.RunOnMainThread(() => GPUErosion(erosionSettings, mapSize, map, randomIndices, ref gpuDone));
            }
            while (!gpuDone)
            {
                Thread.Sleep(1);
            }
        }

        // Fade away erosion at edge
        float blendDistance = 10f;
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                float nearDist = Mathf.Min(i, j);
                float farDist = mapSize - 1 - Mathf.Max(i, j);
                float distFromEdge = Mathf.Min(nearDist, farDist);
                distFromEdge = Mathf.Max(distFromEdge - 3f, 0f);
                float edgeMultiplier = Mathf.Min(distFromEdge / blendDistance, 1f);
                values[i, j] = edgeMultiplier * map[i * mapSize + j]  + (1f - edgeMultiplier) * values[i, j];
            }
        }

#if (PROFILE && UNITY_EDITOR)
        if (terrainSettings.IsMainThread()) {
            float erosionEndTime = Time.realtimeSinceStartup;
            float erosionTimeTaken = erosionEndTime - erosionStartTime;
            Debug.Log("Erosion time taken: " + erosionTimeTaken + "s");
        }
#endif

        return values;
    }

    public static void GPUErosion(ErosionSettings settings, int mapSize, float[] map, int[] randomIndices, ref bool gpuDone)
    {
        BrushValues brushValues = new BrushValues(settings, mapSize);
        // Send brush data to compute shader
        ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushValues.brushIndexOffsets.Count, sizeof(int));
        ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushValues.brushWeights.Count, sizeof(int));
        brushIndexBuffer.SetData(brushValues.brushIndexOffsets);
        brushWeightBuffer.SetData(brushValues.brushWeights);
        ErosionSettings.erosionShader.SetBuffer(0, "brushIndices", brushIndexBuffer);
        ErosionSettings.erosionShader.SetBuffer(0, "brushWeights", brushWeightBuffer);

        // Heightmap buffer
        ComputeBuffer mapBuffer = new ComputeBuffer(mapSize * mapSize, sizeof(float));
        mapBuffer.SetData(map);
        ErosionSettings.erosionShader.SetBuffer(0, "map", mapBuffer);

        // Send random indices to compute shader
        ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
        randomIndexBuffer.SetData(randomIndices);
        ErosionSettings.erosionShader.SetBuffer(0, "randomIndices", randomIndexBuffer);

        ErosionSettings.erosionShader.SetInt("borderSize", settings.erosionBrushRadius);
        ErosionSettings.erosionShader.SetInt("mapSize", mapSize);
        ErosionSettings.erosionShader.SetInt("brushLength", brushValues.brushIndexOffsets.Count);
        ErosionSettings.erosionShader.SetInt("maxLifetime", settings.maxLifetime);
        ErosionSettings.erosionShader.SetFloat("inertia", settings.inertia);
        ErosionSettings.erosionShader.SetFloat("sedimentCapacityFactor", settings.sedimentCapacityFactor);
        ErosionSettings.erosionShader.SetFloat("minSedimentCapacity", settings.minSedimentCapacity);
        ErosionSettings.erosionShader.SetFloat("depositSpeed", settings.depositSpeed);
        ErosionSettings.erosionShader.SetFloat("erodeSpeed", settings.erodeSpeed);
        ErosionSettings.erosionShader.SetFloat("evaporateSpeed", settings.evaporateSpeed);
        ErosionSettings.erosionShader.SetFloat("gravity", settings.gravity);
        ErosionSettings.erosionShader.SetFloat("startSpeed", settings.startSpeed);
        ErosionSettings.erosionShader.SetFloat("startWater", settings.startWater);

        int numThreads = settings.numHydraulicErosionIterations / 1024;
        ErosionSettings.erosionShader.Dispatch(0, numThreads, 1, 1);
        mapBuffer.GetData(map);

        gpuDone = true;

        brushIndexBuffer.Release();
        brushWeightBuffer.Release();
        mapBuffer.Release();
        randomIndexBuffer.Release();
    }

    public static void CPUErosion(ErosionSettings settings, int mapSize, float[,] map)
    {
        System.Random prng = new System.Random(settings.seed);
        BrushValues brushValues = new BrushValues(settings, mapSize);

        for (int i = 0; i < settings.numHydraulicErosionIterations; i++)
        {
            int randomX = prng.Next(settings.erosionBrushRadius, mapSize - settings.erosionBrushRadius);
            int randomY = prng.Next(settings.erosionBrushRadius, mapSize - settings.erosionBrushRadius);
            Drop drop = new Drop(settings, 0, randomX, randomY);
            ErodeDrop(drop, settings, brushValues, map, mapSize);
        }
    }

    // CPU Erosion below
    public static void ErodeDrop(Drop drop, ErosionSettings settings, BrushValues brushValues, float[,] map, int mapSize)
    {
        for (int lifetime = drop.lifetime; lifetime < settings.maxLifetime; lifetime++)
        {
            int nodeX = (int)drop.posX;
            int nodeY = (int)drop.posY;
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
            if (len != 0)
            {
                drop.dirX /= len;
                drop.dirY /= len;
            }
            drop.posX += drop.dirX;
            drop.posY += drop.dirY;

            // Out of map check
            if ((drop.posX == 0 && drop.posY == 0) 
                || drop.posX < settings.erosionBrushRadius  + 1
                || drop.posX > mapSize - settings.erosionBrushRadius - 1 
                || drop.posY < settings.erosionBrushRadius + 1
                || drop.posY > mapSize - settings.erosionBrushRadius - 1) 
            {
                break;
            }

            // Stopped moving check
            if (drop.dirX == 0 && drop.dirY == 0)
            {
                break;
            }

            // Find the droplet's new height and calculate the deltaHeight
            float newHeight = CalculateHeightAndGradient(map, mapSize, drop.posX, drop.posY).height;
            float deltaHeight = newHeight - heightAndGradient.height;

            // Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
            float sedimentCapacity = Mathf.Max(-deltaHeight * drop.speed * drop.water * settings.sedimentCapacityFactor, settings.minSedimentCapacity);

            // If carrying more sediment than capacity, or if flowing uphill:
            if (drop.sediment > sedimentCapacity || deltaHeight > 0)
            {
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
            else
            {
                // Erode a fraction of the droplet's current carry capacity.
                // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                float amountToErode = Mathf.Min((sedimentCapacity - drop.sediment) * settings.erodeSpeed, -deltaHeight);

                for (int i = 0; i < brushValues.brushIndexOffsets.Count; i ++) {
                    int erodeIndex = dropletIndex + brushValues.brushIndexOffsets[i];

                    int brushX = erodeIndex % mapSize;
                    int brushY = erodeIndex / mapSize;

                    float weightedErodeAmount = amountToErode * brushValues.brushWeights[i];
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

    struct HeightAndGradient
    {
        public float height;
        public float gradientX;
        public float gradientY;
    }

    private static HeightAndGradient CalculateHeightAndGradient(float[,] map, int mapSize, float posX, float posY)
    {
        int coordX = (int)posX;
        int coordY = (int)posY;

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = posX - coordX;
        float y = posY - coordY;

        // Calculate heights of the four nodes of the droplet's cell
        float heightNW = map[coordY, coordX];
        float heightNE = map[coordY, coordX + 1];
        float heightSW = map[coordY + 1, coordX];
        float heightSE = map[coordY + 1, coordX + 1];

        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;

        return new HeightAndGradient() { height = height, gradientX = gradientX, gradientY = gradientY };
    }
}

public struct Drop
{
    public float dirX;
    public float dirY;
    public float posX;
    public float posY;
    public float speed;
    public float water;
    public float sediment;
    public int lifetime;

    public Drop(ErosionSettings settings, int lifetime, float posX, float posY)
    {
        dirX = 0;
        dirY = 0;

        speed = settings.startSpeed;
        water = settings.startWater;
        sediment = 0;

        this.lifetime = lifetime;
        this.posX = posX;
        this.posY = posY;
    }

    public Drop(Drop otherDrop)
    {
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
