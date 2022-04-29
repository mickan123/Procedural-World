using UnityEngine;
using System.Threading;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections;

public static class HydraulicErosion
{
    private static readonly bool gpuErosion = true;

    private static readonly object locker = new object();

    private static Dictionary<Thread, bool> gpuDone;

    public static void Init(TerrainSettings settings)
    {
        ErosionSettings.erosionShader = Resources.Load<ComputeShader>("Shaders/Erosion");
        gpuDone = new Dictionary<Thread, bool>();
    }

    public static float[] Erode(
        float[] originalHeightMap,
        TerrainSettings terrainSettings,
        ErosionSettings erosionSettings,
        BiomeInfo info
    )
    {
        int width = info.width;
        int numBiomes = terrainSettings.biomeSettings.Length;

        float[] erodedHeightMap = new float[width * width];
        for (int i = 0; i < width * width; i++)
        {
            erodedHeightMap[i] = originalHeightMap[i];
        }

        if (gpuErosion)
        {
            UnityEngine.Rendering.AsyncGPUReadbackRequest request = new UnityEngine.Rendering.AsyncGPUReadbackRequest();

            NativeArray<float> gpuData = new NativeArray<float>(width * width * 4, Allocator.Persistent);

            Thread curThread = System.Threading.Thread.CurrentThread;

            lock(locker)
            {
                gpuDone[curThread] = false;
            }
            
            if (terrainSettings.IsMainThread())
            {
                GPUErosionEditorAndNotPlaying(erosionSettings, width, erodedHeightMap, curThread, gpuData, ref request);
                request.WaitForCompletion();
            }
            else
            {
                Dispatcher.RunCoroutineOnMainThread(GPUErosion(erosionSettings, width, erodedHeightMap, curThread, gpuData));
                while (!gpuDone[curThread])
                {
                    Thread.Sleep(1);
                }
            }

            for (int i = 0; i < width * width; i++)
            {
                erodedHeightMap[i] = gpuData[i * 4];
            }
            gpuData.Dispose();
        }
        else
        {
            CPUErosion(erosionSettings, width, erodedHeightMap);
        }
        Common.FadeEdgeHeightMap(originalHeightMap, erodedHeightMap, width);
        
        return erodedHeightMap;
    }

    public static IEnumerator GPUErosion(ErosionSettings settings, int width, float[] heightMap, Thread threadId, NativeArray<float> gpuData)
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

        ComputeBuffer heightMapBuffer = new ComputeBuffer(width * width, sizeof(float) * 4);
        heightMapBuffer.SetData(computeShaderHeightMap);

        ComputeBuffer initialHeightMapBuffer = new ComputeBuffer(width * width, sizeof(float));
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

        ComputeShader shader = ComputeShader.Instantiate(ErosionSettings.erosionShader);

        // Set erosion shader parameters
        shader.SetInt("width", width);
        shader.SetFloat("timestep", settings.timestep);
        shader.SetFloat("evaporateSpeed", settings.evaporateSpeed);
        shader.SetFloat("rainRate", settings.rainRate);
        shader.SetFloat("gravity", settings.gravity);
        shader.SetFloat("sedimentCapacityFactor", settings.sedimentCapacityFactor);
        shader.SetFloat("thermalErosionRate", settings.thermalErosionRate);
        shader.SetFloat("talusAngleTangentBias", settings.talusAngleTangentBias);
        shader.SetFloat("talusAngleCoeff", settings.talusAngleCoeff);
        shader.SetFloat("maxErosionDepth", settings.maxErosionDepth);
        shader.SetFloat("sedimentDisolveFactor", settings.sedimentDisolveFactor);
        shader.SetFloat("sedimentDepositFactor", settings.sedimentDepositFactor);
        shader.SetFloat("sedimentSofteningFactor", settings.sedimentSofteningFactor);

        uint threadsPerGroupX, threadsPerGroupY, threadsPerGroupZ;
        shader.GetKernelThreadGroupSizes(0, out threadsPerGroupX, out threadsPerGroupY, out threadsPerGroupZ);

        shader.SetBuffer(0, "InitialHeightMap", initialHeightMapBuffer);
        shader.SetBuffer(0, "HeightMap", heightMapBuffer);
        shader.SetBuffer(0, "FluxMap", fluxBuffer);
        shader.SetBuffer(0, "ThermalFluxMap", thermalFluxBuffer);
        shader.SetBuffer(0, "VelocityMap", velocityBuffer);
        
        int numIterationsPerDispatch = 512;
        shader.SetInt("numIterations", numIterationsPerDispatch);
        for (int i = 0; i < settings.numHydraulicErosionIterations / numIterationsPerDispatch; i++)
        {
            shader.Dispatch(
                0, 
                width / (int)threadsPerGroupX, 
                width / (int)threadsPerGroupY, 
                1
            );
            yield return null;
        }
        
        var request = UnityEngine.Rendering.AsyncGPUReadback.Request(heightMapBuffer, (req) =>
        {
            var tempData = req.GetData<float>();
            tempData.CopyTo(gpuData);
            lock(locker)
            {
                gpuDone[threadId] = true;
            }
        });

        fluxBuffer.Release();
        thermalFluxBuffer.Release();
        velocityBuffer.Release();
        heightMapBuffer.Release();
        initialHeightMapBuffer.Release();
    }

    public static void GPUErosionEditorAndNotPlaying(ErosionSettings settings, int width, float[] heightMap, Thread threadId, NativeArray<float> gpuData, ref UnityEngine.Rendering.AsyncGPUReadbackRequest request)
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

        ComputeBuffer heightMapBuffer = new ComputeBuffer(width * width, sizeof(float) * 4);
        heightMapBuffer.SetData(computeShaderHeightMap);

        ComputeBuffer initialHeightMapBuffer = new ComputeBuffer(width * width, sizeof(float));
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
        ErosionSettings.erosionShader.SetInt("width", width);
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

        uint threadsPerGroupX, threadsPerGroupY, threadsPerGroupZ;
        ErosionSettings.erosionShader.GetKernelThreadGroupSizes(0, out threadsPerGroupX, out threadsPerGroupY, out threadsPerGroupZ);

        ErosionSettings.erosionShader.SetBuffer(0, "InitialHeightMap", initialHeightMapBuffer);
        ErosionSettings.erosionShader.SetBuffer(0, "HeightMap", heightMapBuffer);
        ErosionSettings.erosionShader.SetBuffer(0, "FluxMap", fluxBuffer);
        ErosionSettings.erosionShader.SetBuffer(0, "ThermalFluxMap", thermalFluxBuffer);
        ErosionSettings.erosionShader.SetBuffer(0, "VelocityMap", velocityBuffer);
        
        int numIterationsPerDispatch = settings.numHydraulicErosionIterations;
        ErosionSettings.erosionShader.SetInt("numIterations", numIterationsPerDispatch);
        for (int i = 0; i < settings.numHydraulicErosionIterations / numIterationsPerDispatch; i++)
        {
            ErosionSettings.erosionShader.Dispatch(
                0, 
                width / (int)threadsPerGroupX, 
                width / (int)threadsPerGroupY, 
                1
            );
        }
        
        request = UnityEngine.Rendering.AsyncGPUReadback.Request(heightMapBuffer, (req) =>
        {
            var tempData = req.GetData<float>();
            tempData.CopyTo(gpuData);
            lock(locker)
            {
                gpuDone[threadId] = true;
            }
        });

        fluxBuffer.Release();
        thermalFluxBuffer.Release();
        velocityBuffer.Release();
        heightMapBuffer.Release();
        initialHeightMapBuffer.Release();
    }


    public static void CPUErosion(ErosionSettings settings, int width, float[] heightMap)
    {
        NativeArray<float> initialHeightMapNat = new NativeArray<float>(width * width, Allocator.TempJob);

        NativeArray<float> heightMapNat = new NativeArray<float>(width * width, Allocator.TempJob);
        NativeArray<float> waterMapNat = new NativeArray<float>(width * width, Allocator.TempJob);
        NativeArray<float> sedimentMapNat = new NativeArray<float>(width * width, Allocator.TempJob);
        NativeArray<float> hardnessMapNat = new NativeArray<float>(width * width, Allocator.TempJob);

        NativeArray<float4> waterFluxMapNat = new NativeArray<float4>(width * width, Allocator.TempJob);
        NativeArray<float4> thermalFluxMapNat = new NativeArray<float4>(width * width, Allocator.TempJob);
        NativeArray<float2> velocityMapNat = new NativeArray<float2>(width * width, Allocator.TempJob);

        heightMapNat.CopyFrom(heightMap);
        initialHeightMapNat.CopyFrom(heightMap);
        for (int i = 0; i < hardnessMapNat.Length; i++)
        {
            hardnessMapNat[i] = 1f;
        }

        HydraulicErosionJob burstJob = new HydraulicErosionJob{
            width = width,
            height = width,
            timestep = settings.timestep,
            evaporateSpeed = settings.evaporateSpeed,
            rainRate = settings.rainRate,
            maxErosionDepth = settings.maxErosionDepth,
            gravity = settings.gravity,
            sedimentCapacityFactor = settings.sedimentCapacityFactor,
            sedimentDisolveFactor = settings.sedimentDisolveFactor,
            sedimentDepositFactor = settings.sedimentDepositFactor,
            sedimentSofteningFactor = settings.sedimentSofteningFactor,
            thermalErosionRate = settings.thermalErosionRate,
            talusAngleTangentBias = settings.talusAngleTangentBias,
            talusAngleCoeff = settings.talusAngleCoeff,
            numIterations = settings.numHydraulicErosionIterations,
            heightMap = heightMapNat,
            waterMap = waterMapNat,
            sedimentMap = sedimentMapNat,
            hardnessMap = hardnessMapNat,
            waterFluxMap = waterFluxMapNat,
            thermalFluxMap = thermalFluxMapNat,
            velocityMap = velocityMapNat,
            initialHeightMap = initialHeightMapNat
        };
        burstJob.Schedule(width + 5, 1).Complete();

        heightMapNat.CopyTo(heightMap);

        heightMapNat.Dispose();
        waterMapNat.Dispose();
        sedimentMapNat.Dispose();
        hardnessMapNat.Dispose();
        waterFluxMapNat.Dispose();
        thermalFluxMapNat.Dispose();
        velocityMapNat.Dispose();
        initialHeightMapNat.Dispose();
    }
}