using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public static class HydraulicErosion
{
    public static void Init(TerrainSettings settings)
    {
        ErosionSettings.erosionShader = Resources.Load<ComputeShader>("Shaders/Erosion");
    }

    public static float[][] Erode(
        float[][] originalHeightMap,
        TerrainSettings terrainSettings,
        ErosionSettings erosionSettings,
        BiomeInfo info
    )
    {
        int mapSize = originalHeightMap.Length;
        int numBiomes = terrainSettings.biomeSettings.Length;

        float[] erodedHeightMap = new float[mapSize * mapSize];
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                erodedHeightMap[i * mapSize + j] = originalHeightMap[i][j];
            }
        }

        bool gpuDone = false;

        if (terrainSettings.IsMainThread())
        {
            GPUErosion(erosionSettings, mapSize, erodedHeightMap, ref gpuDone);
        }
        else
        {
            Dispatcher.RunOnMainThread(() => GPUErosion(erosionSettings, mapSize, erodedHeightMap, ref gpuDone));
        }

        while (!gpuDone)
        {
            Thread.Sleep(1);
        }

        FadeEdgeErosion(erodedHeightMap, originalHeightMap);
        
        return originalHeightMap;
    }

    public static void FadeEdgeErosion(float[] erodedHeightMap, float[][] originalHeightMap, float blendDistance = 5f)
    {
        int mapSize = originalHeightMap.Length;
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                float nearDist = i < j ? i : j;
                float farDist = mapSize - 1 - (i > j ? i : j);
                float distFromEdge = nearDist < farDist ? nearDist : farDist;
                distFromEdge = distFromEdge - 3f < 0f ? 0f : distFromEdge - 3f ;
                float edgeMultiplier = distFromEdge / blendDistance < 1f ? distFromEdge / blendDistance :1f;
                originalHeightMap[i][j] = edgeMultiplier * erodedHeightMap[i * mapSize + j] + (1f - edgeMultiplier) * originalHeightMap[i][j];
            }
        }
    }

    public static void GPUErosion(ErosionSettings settings, int mapSize, float[] heightMap, ref bool gpuDone)
    {
        int length = heightMap.Length;

        // Heightmap buffer
        float[] computeShaderHeightMap = new float[length * 4];
        for (int i = 0; i < length; i++)
        {
            computeShaderHeightMap[i * 4] = heightMap[i]; // Height
            computeShaderHeightMap[i * 4 + 1] = 0; // Water height
            computeShaderHeightMap[i * 4 + 2] = 0; // Sediment
            computeShaderHeightMap[i * 4 + 3] = 1; // Hardness
        }

        ComputeBuffer heightMapBuffer = new ComputeBuffer(mapSize * mapSize, sizeof(float) * 4);
        heightMapBuffer.SetData(computeShaderHeightMap);

        ComputeBuffer initialHeightMapBuffer = new ComputeBuffer(mapSize * mapSize, sizeof(float));
        initialHeightMapBuffer.SetData(heightMap);

        // Set initial flux and velocity to zeros
        float[] flux = new float[length * 4];
        float[] thermalFlux = new float[length * 4];
        float[] velocity = new float[length * 2];

        ComputeBuffer fluxBuffer = new ComputeBuffer(flux.Length, sizeof(float) * 4);
        ComputeBuffer thermalFluxBuffer = new ComputeBuffer(flux.Length, sizeof(float) * 4);
        ComputeBuffer velocityBuffer = new ComputeBuffer(velocity.Length, sizeof(float) * 2);

        fluxBuffer.SetData(flux);
        thermalFluxBuffer.SetData(thermalFlux);
        velocityBuffer.SetData(velocity);

        // Set erosion shader parameters
        ErosionSettings.erosionShader.SetInt("mapSize", mapSize);
        ErosionSettings.erosionShader.SetFloat("timestep", settings.timestep);
        ErosionSettings.erosionShader.SetFloat("evaporateSpeed", settings.evaporateSpeed);
        ErosionSettings.erosionShader.SetFloat("rainRate", settings.rainRate);
        ErosionSettings.erosionShader.SetFloat("gravity", settings.gravity);
        ErosionSettings.erosionShader.SetFloat("sedimentCapacityFactor", settings.sedimentCapacityFactor);
        ErosionSettings.erosionShader.SetFloat("thermalErosionRate", settings.thermalErosionRate);
        ErosionSettings.erosionShader.SetFloat("talusAngleTangentBias", settings.talusAngleTangentBias);
        ErosionSettings.erosionShader.SetFloat("talusAngleCoeff", settings.talusAngleCoeff);
        ErosionSettings.erosionShader.SetFloat("maxErosionDepth", settings.maxErosionDepth);
        ErosionSettings.erosionShader.SetFloat("sedimentDisolveFactor", settings.sedimentDisolveFactor);
        ErosionSettings.erosionShader.SetFloat("sedimentDepositFactor", settings.sedimentDepositFactor);
        ErosionSettings.erosionShader.SetFloat("sedimentSofteningFactor", settings.sedimentSofteningFactor);

        int numThreads = (mapSize * mapSize / 1024) + 1;
        int numThreadsX = (mapSize / 256) + 1;
        int numThreadsY = (mapSize / 256) + 1;

        uint threadsPerGroupX, threadsPerGroupY, threadsPerGroupZ;
        ErosionSettings.erosionShader.GetKernelThreadGroupSizes(0, out threadsPerGroupX, out threadsPerGroupY, out threadsPerGroupZ);

        ErosionSettings.erosionShader.SetBuffer(0, "InitialHeightMap", initialHeightMapBuffer);
        ErosionSettings.erosionShader.SetBuffer(0, "HeightMap", heightMapBuffer);
        ErosionSettings.erosionShader.SetBuffer(0, "FluxMap", fluxBuffer);
        ErosionSettings.erosionShader.SetBuffer(0, "ThermalFluxMap", thermalFluxBuffer);
        ErosionSettings.erosionShader.SetBuffer(0, "VelocityMap", velocityBuffer);
        

        ErosionSettings.erosionShader.SetInt("numIterations", 100);
        for (int i = 0; i < settings.numHydraulicErosionIterations / 100; i++)
        {
            ErosionSettings.erosionShader.Dispatch(
                0, 
                mapSize * mapSize / (int)threadsPerGroupX, 
                1,
                1
            );
        }
        
        heightMapBuffer.GetData(computeShaderHeightMap);
        for (int i = 0; i < length; i++)
        {
            heightMap[i] = computeShaderHeightMap[i * 4];
        }

        gpuDone = true;

        fluxBuffer.Release();
        thermalFluxBuffer.Release();
        velocityBuffer.Release();
        heightMapBuffer.Release();
        initialHeightMapBuffer.Release();

    }
}