using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public static class HydraulicErosion
{
    private static readonly bool cpuErosion = false;

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

        if (cpuErosion)
        {
            float[,] cpuMap = Common.CopyArray(values);
            CPUErosion(erosionSettings, cpuMap);
            return cpuMap;
        }
        else
        {
            // Generate random indices to use
            int[] rainfallIndices = new int[erosionSettings.numHydraulicErosionIterations];
            System.Random prng = new System.Random(erosionSettings.seed);
            for (int i = 0; i < erosionSettings.numHydraulicErosionIterations; i++)
            {
                rainfallIndices[i] = prng.Next(0, 1024);
            }

            bool gpuDone = false;

            if (terrainSettings.IsMainThread())
            {
                GPUErosion(erosionSettings, mapSize, map, rainfallIndices, ref gpuDone);
            }
            else
            {
                Dispatcher.RunOnMainThread(() => GPUErosion(erosionSettings, mapSize, map, rainfallIndices, ref gpuDone));
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
                values[i, j] = edgeMultiplier * map[i * mapSize + j] + (1f - edgeMultiplier) * values[i, j];


                // TODO: Remove this once done testing erosion
                values[i, j] = map[i * mapSize + j];
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

    public static void GPUErosion(ErosionSettings settings, int mapSize, float[] heightMap, int[] rainfallIndices, ref bool gpuDone)
    {
        // Heightmap buffer
        float[] computeShaderHeightMap = new float[heightMap.Length * 4];
        for (int i = 0; i < heightMap.Length; i++)
        {
            computeShaderHeightMap[i * 4] = heightMap[i]; // Height
            computeShaderHeightMap[i * 4 + 1] = 0; // Water height
            computeShaderHeightMap[i * 4 + 2] = 0; // Sediment
            computeShaderHeightMap[i * 4 + 3] = 1; // Hardness
        }

        ComputeBuffer heightMapBuffer = new ComputeBuffer(mapSize * mapSize, sizeof(float) * 4);
        heightMapBuffer.SetData(computeShaderHeightMap);
        ErosionSettings.erosionShader.SetBuffer(0, "HeightMap", heightMapBuffer);

        // Send random indices to compute shader
        ComputeBuffer rainfallIndexBuffer = new ComputeBuffer(rainfallIndices.Length, sizeof(int));
        rainfallIndexBuffer.SetData(rainfallIndices);
        ErosionSettings.erosionShader.SetBuffer(0, "RainfallIndices", rainfallIndexBuffer);

        // Set initial flux and velocity to zeros
        float[] flux = new float[heightMap.Length * 4];
        float[] thermalFlux = new float[heightMap.Length * 4];
        float[] velocity = new float[heightMap.Length * 2];

        ComputeBuffer fluxBuffer = new ComputeBuffer(flux.Length, sizeof(float) * 4);
        ComputeBuffer thermalFluxBuffer = new ComputeBuffer(flux.Length, sizeof(float) * 4);
        ComputeBuffer velocityBuffer = new ComputeBuffer(velocity.Length, sizeof(float) * 2);

        fluxBuffer.SetData(flux);
        thermalFluxBuffer.SetData(thermalFlux);
        velocityBuffer.SetData(velocity);

        ErosionSettings.erosionShader.SetBuffer(0, "FluxMap", fluxBuffer);
        ErosionSettings.erosionShader.SetBuffer(0, "ThermalFluxMap", thermalFluxBuffer);
        ErosionSettings.erosionShader.SetBuffer(0, "VelocityMap", velocityBuffer);

        // Set erosion shader parameters
        ErosionSettings.erosionShader.SetInt("mapSize", mapSize);
        ErosionSettings.erosionShader.SetFloat("timestep", settings.timestep);
        ErosionSettings.erosionShader.SetFloat("evaporateSpeed", settings.evaporateSpeed);
        ErosionSettings.erosionShader.SetFloat("rainRate", settings.rainRate);
        ErosionSettings.erosionShader.SetFloat("gravity", settings.gravity);
        ErosionSettings.erosionShader.SetFloat("sedimentCapacityFactor", settings.sedimentCapacityFactor);
        ErosionSettings.erosionShader.SetFloat("sedimentDisolveFactor", settings.sedimentDisolveFactor);
        ErosionSettings.erosionShader.SetFloat("sedimentDepositFactor", settings.sedimentDepositFactor);
        ErosionSettings.erosionShader.SetFloat("thermalErosionRate", settings.thermalErosionRate);
        ErosionSettings.erosionShader.SetFloat("talusAngleTangentBias", settings.talusAngleTangentBias);
        ErosionSettings.erosionShader.SetFloat("talusAngleCoeff", settings.talusAngleCoeff);
        ErosionSettings.erosionShader.SetFloat("sedimentSofteningFactor", settings.sedimentSofteningFactor);

        ErosionSettings.erosionShader.SetFloat("stepSize", settings.stepSize);
        ErosionSettings.erosionShader.SetFloat("pipeArea", settings.pipeArea);

        int numThreads = (mapSize * mapSize / 1024) + 1;
        int numThreadsX = (mapSize / 256) + 1;
        int numThreadsY = (mapSize / 256) + 1;

        uint threadsPerGroupX, threadsPerGroupY, threadsPerGroupZ;
        ErosionSettings.erosionShader.GetKernelThreadGroupSizes(0, out threadsPerGroupX, out threadsPerGroupY, out threadsPerGroupZ);

        float[] waterHeight = new float[heightMap.Length];
        float[] sediment = new float[heightMap.Length];
        float[] hardness = new float[heightMap.Length];
        float[] deltaHeight = new float[heightMap.Length];

        for (int i = 0; i < settings.numHydraulicErosionIterations; i++)
        {
            ErosionSettings.erosionShader.SetInt("iteration", i);
            ErosionSettings.erosionShader.Dispatch(
                0, 
                mapSize * mapSize / (int)threadsPerGroupX + 1, 
                1,
                1
            );
        }

        heightMapBuffer.GetData(computeShaderHeightMap);
        fluxBuffer.GetData(flux);
        velocityBuffer.GetData(velocity);

        for (int j = 0; j < heightMap.Length; j++)
        {
            deltaHeight[j] = computeShaderHeightMap[j * 4] - heightMap[j];
            heightMap[j] = computeShaderHeightMap[j * 4];
            waterHeight[j] = computeShaderHeightMap[j * 4 + 1];
            sediment[j] = computeShaderHeightMap[j * 4 + 2];
            hardness[j] = computeShaderHeightMap[j * 4 + 3];
        }

        gpuDone = true;

        fluxBuffer.Release();
        thermalFluxBuffer.Release();
        velocityBuffer.Release();
        heightMapBuffer.Release();
        rainfallIndexBuffer.Release();
    }
















    public static void CPUErosion(ErosionSettings settings, float[,] map)
    {
        // Generate random indices
        int mapSize = map.GetLength(0);
        System.Random prng = new System.Random(settings.seed);

        Vector4[,] flux = new Vector4[mapSize, mapSize];
        Vector4[,] thermalErosionFlux = new Vector4[mapSize, mapSize];
        Vector2[,] velocity = new Vector2[mapSize, mapSize];

        float[,] waterHeight = new float[mapSize, mapSize];
        float[,] sediment = new float[mapSize, mapSize];

        float[,] hardness = new float[mapSize, mapSize];
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                hardness[x, y] = 1f;
            }
        }

        for (int i = 0; i < settings.numHydraulicErosionIterations; i++)
        {
            CPUWaterSimulationErosion(
                settings,
                map,
                flux,
                thermalErosionFlux,
                velocity,
                waterHeight,
                sediment,
                hardness,
                prng
            );
        }
    }

    public static void CPUWaterSimulationErosion(
        ErosionSettings settings,
        float[,] map,
        Vector4[,] flux,
        Vector4[,] thermalErosionFlux,
        Vector2[,] velocity,
        float[,] waterHeight,
        float[,] sediment,
        float[,] hardness,
        System.Random prng
    )
    {
        int mapSize = map.GetLength(0);

        WaterSources(settings, waterHeight, prng);
        FluxComputation(settings, map, waterHeight, flux);
        VelocityComputation(settings, map, waterHeight, flux, velocity);
        ErosionDeposition(settings, map, waterHeight, sediment, velocity, hardness);
        SedimentTransportation(settings, map, sediment, velocity);
        CalculateThermalFlux(settings, map, hardness, thermalErosionFlux);
        ApplyThermalErosion(settings, map, thermalErosionFlux);
    }

    public static void WaterSources(ErosionSettings settings, float[,] waterHeight, System.Random prng) 
    {   
        int mapSize = waterHeight.GetLength(0);
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                waterHeight[x, y] += settings.rainRate * settings.timestep;
            }
        }
    }

    public static void FluxComputation(ErosionSettings settings, float[,] map, float[,] waterHeight, Vector4[,] flux)
    {
        int mapSize = map.GetLength(0);
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                float curTerrainHeight = map[x, y];
                float curWaterHeight = waterHeight[x, y];
                float curFullHeight = curTerrainHeight + curWaterHeight;
                Vector4 curOutputFlux = flux[x, y];

                float fullHeightLeft = (x == 0) ? curFullHeight : map[x - 1, y] + waterHeight[x - 1, y];
                float fullHeightRight = (x == mapSize - 1) ? curFullHeight : map[x + 1, y] + waterHeight[x + 1, y];
                float fullHeightTop = (y == 0) ? curFullHeight : map[x, y - 1] + waterHeight[x, y - 1];
                float fullHeightBottom = (y == mapSize - 1) ? curFullHeight : map[x, y + 1] + waterHeight[x, y + 1];

                Vector4 deltaHeight = new Vector4(curFullHeight, curFullHeight, curFullHeight, curFullHeight) - new Vector4(
                    fullHeightLeft,
                    fullHeightRight,
                    fullHeightTop,
                    fullHeightBottom
                );

                Vector4 outputFlux = curOutputFlux + settings.timestep * settings.gravity * deltaHeight * settings.pipeArea / settings.stepSize;
                outputFlux.x = Mathf.Max(0, outputFlux.x);
                outputFlux.y = Mathf.Max(0, outputFlux.y);
                outputFlux.z = Mathf.Max(0, outputFlux.z);
                outputFlux.w = Mathf.Max(0, outputFlux.w);

                float sumOutputFlux = outputFlux.x + outputFlux.y + outputFlux.z + outputFlux.w;
                if (sumOutputFlux == 0f)
                {
                    flux[x, y] = outputFlux;
                }
                else 
                {
                    float fluxRescaleConstant = Mathf.Min(1, waterHeight[x, y] * settings.stepSize * settings.stepSize / (sumOutputFlux * settings.timestep));
                    flux[x, y] = outputFlux * fluxRescaleConstant;
                }
            }
        }
    }

    public static void VelocityComputation(ErosionSettings settings, float[,] map, float[,] waterHeight, Vector4[,] flux, Vector2[,] velocity)
    {
        int mapSize = map.GetLength(0);
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                Vector4 outputFlux = flux[x, y];
                Vector4 inputFlux = new Vector4(
                    (x == 0) ? 0 : flux[x - 1, y].y,
                    (x == mapSize - 1) ? 0 : flux[x + 1, y].x,
                    (y == 0) ? 0 : flux[x, y - 1].w,
                    (y == mapSize - 1) ? 0 : flux[x, y + 1].z
                );

                float totalInputFlux = inputFlux.x + inputFlux.y + inputFlux.z + inputFlux.w;
                float totalOutpoutFlux = outputFlux.x + outputFlux.y + outputFlux.z + outputFlux.w;

                float volumeDelta = totalInputFlux - totalOutpoutFlux;

                waterHeight[x, y] += settings.timestep * volumeDelta / (settings.stepSize * settings.stepSize);

                velocity[x, y] = new Vector2(
                    0.5f * (inputFlux.x - outputFlux.x + inputFlux.y - outputFlux.y),
                    0.5f * (inputFlux.w - outputFlux.w + inputFlux.z - outputFlux.z)
                );
            }
        }
    }

    public static void ErosionDeposition(ErosionSettings settings, float[,] map, float[,] waterHeight, float[,] sediment, Vector2[,] velocity, float[,] hardness)
    {
        int mapSize = map.GetLength(0);
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                float curSediment = sediment[x, y];
                float curTerrainHeight = map[x, y];
                float heightLeft = (x == 0) ? curTerrainHeight : map[x - 1, y];
                float heightRight = (x == mapSize - 1) ? curTerrainHeight : map[x + 1, y];
                float heightTop = (y == 0) ? curTerrainHeight : map[x, y - 1];
                float heightBottom = (y == mapSize - 1) ? curTerrainHeight : map[x, y + 1];
                Vector2 curVelocity = velocity[x, y];

                // Tilt angle computation
                Vector3 dhdx = new Vector3(2 * settings.stepSize, heightRight - heightLeft, 0);
                Vector3 dhdy = new Vector3(0, heightTop - heightBottom, 2 * settings.stepSize);
                Vector3 normal = Vector3.Cross(dhdx, dhdy);
                float sinTiltAngle = Mathf.Max(Mathf.Abs(normal.y) / normal.magnitude, 0.05f);
            
                // Lmax calc
                float lmax = Mathf.Max(0, 1.0f - waterHeight[x, y] / settings.maxErosionDepth);
                
                // Sediment capacity calc
                float sedimentCapacity = settings.sedimentCapacityFactor * curVelocity.magnitude * sinTiltAngle * lmax;
                
                // Update sediment
                if (sediment[x, y] < sedimentCapacity)
                {
                    float deltaSediment = Mathf.Clamp(settings.timestep * hardness[x, y] * settings.sedimentDisolveFactor * (sedimentCapacity - sediment[x, y]), 0, waterHeight[x, y]);
                    map[x, y] -= deltaSediment;
                    sediment[x, y] += deltaSediment;
                    waterHeight[x, y] += deltaSediment;
                }
                else // Deposit sediment if we are over capacity
                {
                    float deltaSediment = Mathf.Clamp(settings.timestep * settings.sedimentDepositFactor * (sediment[x, y] - sedimentCapacity), 0, waterHeight[x, y]);
                    map[x, y] += deltaSediment;
                    sediment[x, y] -= deltaSediment;
                    waterHeight[x, y] -= deltaSediment;
                }

                // Hardness update
                hardness[x, y] = hardness[x, y] - settings.timestep * settings.sedimentDisolveFactor * settings.sedimentSofteningFactor * (curSediment - sedimentCapacity);
                hardness[x, y] = Mathf.Clamp(hardness[x, y], 0.1f, 1f);

                // Water evaporation
                waterHeight[x, y] *= (1 - settings.evaporateSpeed * settings.timestep);
            }
        }
    }

    public static void SedimentTransportation(ErosionSettings settings, float[,] map, float[,] sediment, Vector2[,] velocity)
    {
        int mapSize = map.GetLength(0);
        float[,] intermediateSed = new float[mapSize, mapSize];
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                Vector2 curVelocity = velocity[x, y];
                Vector2 curPos = new Vector2(x, y);

                Vector2 destPos = curPos - (curVelocity * settings.timestep);
                
                intermediateSed[x, y] = Common.HeightFromFloatCoord(destPos, sediment);                
            }
        }

        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                sediment[x, y] = intermediateSed[x, y];
            }
        }
    }

    public static void CalculateThermalFlux(ErosionSettings settings, float[,] map, float[,] hardness, Vector4[,] flux)
    {
        int mapSize = map.GetLength(0);
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                float curTerrainHeight = map[x, y];

                float heightLeft = (x == 0) ? curTerrainHeight : map[x - 1, y];
                float heightRight = (x == mapSize - 1) ? curTerrainHeight : map[x + 1, y];
                float heightTop = (y == 0) ? curTerrainHeight : map[x, y - 1];
                float heightBottom = (y == mapSize - 1) ? curTerrainHeight : map[x, y + 1];

                float heightDiffLeft = Mathf.Max(0, curTerrainHeight - heightLeft);
                float heightDiffRight = Mathf.Max(0, curTerrainHeight - heightRight);
                float heightDiffTop = Mathf.Max(0, curTerrainHeight - heightTop);
                float heightDiffBottom = Mathf.Max(0, curTerrainHeight - heightBottom);

                float maxHeightDiff = Mathf.Max(
                    Mathf.Max(heightDiffLeft, heightDiffRight),
                    Mathf.Max(heightDiffTop, heightDiffBottom)
                );
                
                float deltaVolume = settings.stepSize * settings.stepSize * settings.thermalErosionRate * hardness[x, y] * maxHeightDiff * 0.5f;

                float thermalThreshold = hardness[x, y] * settings.talusAngleCoeff + settings.talusAngleTangentBias;

                float fluxLeft = (heightDiffLeft / settings.stepSize > thermalThreshold) ? heightDiffLeft : 0f;
                float fluxRight = (heightDiffRight / settings.stepSize > thermalThreshold) ? heightDiffRight : 0f;
                float fluxTop = (heightDiffTop / settings.stepSize > thermalThreshold) ? heightDiffTop : 0f;
                float fluxBottom = (heightDiffBottom / settings.stepSize > thermalThreshold) ? heightDiffBottom : 0f;

                float sumFlux = fluxLeft + fluxRight + fluxTop + fluxBottom;
                sumFlux = (sumFlux == 0) ? 1f : sumFlux;

                Vector4 ouptutFlux = deltaVolume * new Vector4(fluxLeft, fluxRight, fluxTop, fluxBottom) / sumFlux;

                flux[x, y] = ouptutFlux;
            }
        }
    }

    public static void ApplyThermalErosion(ErosionSettings settings, float[,] map, Vector4[,] flux)
    {
        int mapSize = map.GetLength(0);
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                Vector4 outputFlux = flux[x, y];
                Vector4 inputFlux = new Vector4(
                    (x == 0) ? 0 : flux[x - 1, y].y,
                    (x == mapSize - 1) ? 0 : flux[x + 1, y].x,
                    (y == 0) ? 0 : flux[x, y - 1].w,
                    (y == mapSize - 1) ? 0 : flux[x, y + 1].z
                );

                float sumOutputFlux = outputFlux.x + outputFlux.y + outputFlux.z + outputFlux.w;
                float sumInputFlux = inputFlux.x + inputFlux.y + inputFlux.z + inputFlux.w;

                float volumeDelta = sumInputFlux - sumOutputFlux;

                map[x, y] += settings.timestep * volumeDelta;
            }
        }
    }















    public static void CPUDropErosion(ErosionSettings settings, float[,] map)
    {
        int mapSize = map.GetLength(0);
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
                || drop.posX < settings.erosionBrushRadius + 1
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

                for (int i = 0; i < brushValues.brushIndexOffsets.Count; i++)
                {
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
}

