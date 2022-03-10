using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


[BurstCompile(CompileSynchronously = true)]
struct HydraulicErosionJob : IJob
{
    public NativeArray<float> heightMap;
    public NativeArray<float> waterMap;
    public NativeArray<float> sedimentMap;
    public NativeArray<float> hardnessMap;

    public NativeArray<float4> waterFluxMap;
    public NativeArray<float4> thermalFluxMap;
    public NativeArray<float2> velocityMap;

    
    [ReadOnly] public NativeArray<float> initialHeightMap;

    [ReadOnly] public int width;
    [ReadOnly] public int height;
    [ReadOnly] public float timestep;
    [ReadOnly] public float evaporateSpeed;
    [ReadOnly] public float rainRate;
    [ReadOnly] public float maxErosionDepth;
    [ReadOnly] public float gravity;
    [ReadOnly] public float sedimentCapacityFactor;
    [ReadOnly] public float sedimentDisolveFactor;
    [ReadOnly] public float sedimentDepositFactor;
    [ReadOnly] public float sedimentSofteningFactor;
    [ReadOnly] public float thermalErosionRate;
    [ReadOnly] public float talusAngleTangentBias;
    [ReadOnly]public float talusAngleCoeff;

    [ReadOnly] public int numIterations;
    
    public void Execute()
    {
        for (int i = 0; i < numIterations; i++) 
        {
            WaterIncrease();
            FluxComputation();
            VelocityComputation();
            ErosionDeposition();
            SedimentTransportation();
            ThermalFluxComputation();
            ApplyThermalErosion();
        }
        // WaterIncrease();
    }

    public void WaterIncrease()
    {
        for (int i = 0; i < width * height; i++)
        {
            waterMap[i] += rainRate;
        }
    }

    public void FluxComputation()
    {
        const float pipeArea = 60f;
        for (int i = width; i < width * height - width; i++)
        {
            float heightLeft = heightMap[i - 1];
            float waterLeft = waterMap[i - 1];

            float heightRight = heightMap[i + 1];
            float waterRight = waterMap[i + 1];

            float heightTop = heightMap[i - width];
            float waterTop = waterMap[i - width];

            float heightBottom = heightMap[i + width];
            float waterBottom = waterMap[i + width];

            float height = heightMap[i];
            float water = waterMap[i];
            float totalHeight = height + water;

            float4 deltaHeight = new float4(
                totalHeight - heightLeft - waterLeft,
                totalHeight - heightRight - waterRight,
                totalHeight - heightTop - waterTop,
                totalHeight - heightBottom - waterBottom
            );

            float4 currentFlux = waterFluxMap[i];
            
            float4 outputFlux = new float4(
                math.max(0, currentFlux.x + timestep * gravity * deltaHeight.x * pipeArea),
                math.max(0, currentFlux.y + timestep * gravity * deltaHeight.y * pipeArea),
                math.max(0, currentFlux.z + timestep * gravity * deltaHeight.z * pipeArea),
                math.max(0, currentFlux.w + timestep * gravity * deltaHeight.w * pipeArea)
            );

            float sumOutputFlux = outputFlux.x + outputFlux.y + outputFlux.z + outputFlux.w;

            if (sumOutputFlux != 0)
            {
                outputFlux.x *= math.min(1, water / (sumOutputFlux * timestep));
                outputFlux.y *= math.min(1, water / (sumOutputFlux * timestep));
                outputFlux.w *= math.min(1, water / (sumOutputFlux * timestep));
                outputFlux.z *= math.min(1, water / (sumOutputFlux * timestep));
            }
            
            waterFluxMap[i] = outputFlux;
        }
    }

    public void VelocityComputation()
    {
        for (int i = width; i < width * height - width; i++)
        {
            float4 outputFlux = waterFluxMap[i];
            float4 leftFlux = waterFluxMap[i - 1];
            float4 rightFlux = waterFluxMap[i + 1];
            float4 topFlux = waterFluxMap[i - width];
            float4 bottomFlux = waterFluxMap[i + width];

            float4 inputFlux = new float4(
                RightDir(leftFlux),
                LeftDir(rightFlux),
                BottomDir(topFlux),
                TopDir(bottomFlux)
            );

            float deltaVolume = timestep * (SumComponents(inputFlux) - SumComponents(outputFlux));

            waterMap[i] += deltaVolume;

            velocityMap[i] = new float2(
                0.5f * (LeftDir(inputFlux) - LeftDir(outputFlux) + RightDir(outputFlux) + RightDir(inputFlux)),
                0.5f * (BottomDir(inputFlux) - BottomDir(outputFlux) + TopDir(outputFlux) + TopDir(inputFlux))
            );
        }
    }

    public void ErosionDeposition()
    {
        for (int i = width; i < width * height - width; i++)
        {
            float curHeight = heightMap[i];

            float heightLeft = heightMap[i - 1];
            float heightRight = heightMap[i + 1];
            float heightTop = heightMap[i - width];
            float heightBottom = heightMap[i + width];

            float2 velocity = velocityMap[i];

            // Tilt angle computation
            float3 dhdx = new float3(2f, heightRight - heightLeft, 0f);
            float3 dhdy = new float3(0f, heightTop - heightBottom, 2f);
            float3 normal = math.cross(dhdx, dhdy);
            float sinTiltAngle = math.abs(normal.y) / math.length(normal);

            float deltaHeight = initialHeightMap[i] - curHeight;
            float maxErosionMultiplier = 1f - math.max(0, deltaHeight) / maxErosionDepth;
            maxErosionMultiplier = 1f;

            float sedimentCapacity = sedimentCapacityFactor * math.length(velocity) * math.min(sinTiltAngle, 0.05f) * maxErosionMultiplier;

            // Take sediment from soil if we haven't filled up capacity
            float curSediment = sedimentMap[i];
            if (curSediment < sedimentCapacity)
            {
                float deltaSediment = timestep * sedimentDisolveFactor * hardnessMap[i] * (sedimentCapacity - curSediment);
                heightMap[i] -= deltaSediment;
                waterMap[i] += deltaSediment;
                sedimentMap[i] += deltaSediment;
            }
            else // Deposit sediment if we are over capacity
            {
                float deltaSediment = timestep * sedimentDisolveFactor * hardnessMap[i] * (curSediment - sedimentCapacity);
                heightMap[i] += deltaSediment;
                waterMap[i] -= deltaSediment;
                sedimentMap[i] -= deltaSediment;
            }

            // Water evaporation.
            waterMap[i] *= (1 - evaporateSpeed) * timestep;

            // Hardness update
            hardnessMap[i] = hardnessMap[i] - timestep * sedimentSofteningFactor * sedimentDisolveFactor * (sedimentMap[i] - sedimentCapacity);
            hardnessMap[i] = math.clamp(hardnessMap[i], 0.1f, 1f);
        }
    }

    public void SedimentTransportation()
    {
        float maxIndex = width - 1;
        float minIndex = 0;
        for (int i = 0; i < width * height; i++)
        {
            float2 velocity = velocityMap[i];
            float2 pos = new float2(
                math.floor(i % width),
                math.floor(i / height)
            );

            float updatedX = math.clamp(pos.x + velocity.x * timestep, minIndex, maxIndex);
            float updatedY = math.clamp(pos.y + velocity.y * timestep, minIndex, maxIndex);

            float2 updatedPos = new float2(updatedX, updatedY);

            sedimentMap[i] = SampleBillinear(updatedPos);
        }
    }

    public void ThermalFluxComputation()
    {
        for (int i = width; i < width * height - width; i++)
        {
            float heightLeft = heightMap[i - 1];
            float heightRight = heightMap[i + 1];
            float heightTop = heightMap[i - width];
            float heightBottom = heightMap[i + width];

            float height = heightMap[i];

            float4 deltaHeight = new float4(
                height - heightLeft,
                height - heightRight,
                height - heightTop,
                height - heightBottom
            );  

            float maxHeightDifference = math.cmax(deltaHeight);
            float deltaVolume = thermalErosionRate * hardnessMap[i] * maxHeightDifference * 0.5f;

            // Calculate threshold angle to determine if mass falls in that direction
            float threshold = hardnessMap[i] * talusAngleCoeff + talusAngleTangentBias;

            float4 tanAngle = deltaHeight;
            float4 k = new float4();
            
            if (tanAngle.x > threshold)
                k.x = deltaHeight.x;
            if (tanAngle.y > threshold)
                k.y = deltaHeight.y;
            if (tanAngle.z > threshold)
                k.z = deltaHeight.z;
            if (tanAngle.w > threshold)
                k.w = deltaHeight.w;

            float sumProportions = SumComponents(k);
            if (sumProportions != 0)
            {
                thermalFluxMap[i] = deltaVolume * k / sumProportions;
            }
        }   
    }

    public void ApplyThermalErosion()
    {
        for (int i = width; i < width * height - width; i++)
        {
            float4 outputFlux = thermalFluxMap[i];

            float4 leftFlux = thermalFluxMap[i - 1];
            float4 rightFlux = thermalFluxMap[i + 1];
            float4 topFlux = thermalFluxMap[i - width];
            float4 bottomFlux = thermalFluxMap[i + width];

            float4 inputFlux = new float4(
                RightDir(leftFlux),
                LeftDir(rightFlux),
                BottomDir(topFlux),
                TopDir(bottomFlux)
            );

            float deltaVolume = SumComponents(inputFlux) - SumComponents(outputFlux);

            heightMap[i] += timestep * deltaVolume * thermalErosionRate;
        }
    }

    public float SampleBillinear(float2 pos)
    {
        float2 uva = math.floor(pos);
        float2 uvb = math.ceil(pos);

        int2 id00 = (int2)uva;  // 0 0
        int2 id10 = new int2((int)uvb.x, (int)uva.y); // 1 0
        int2 id01 = new int2((int)uva.x, (int)uvb.y); // 0 1	
        int2 id11 = (int2)uvb; // 1 1

        float2 d = pos - uva;

        return
            heightMap[id00.x + id00.y * width] * (1 - d.x) * (1 - d.y) +
            heightMap[id10.x + id10.y * width] * d.x * (1 - d.y) +
            heightMap[id01.x + id01.y * width] * (1 - d.x) * d.y +
            heightMap[id11.x + id11.y * width] * d.x * d.y;
    }

    public static float LeftDir(float4 v)
    {
        return v.x;
    }

    public static float RightDir(float4 v)
    {
        return v.y;
    }

    public static float TopDir(float4 v)
    {
        return v.z;
    }

    public static float BottomDir(float4 v)
    {
        return v.w;
    }

    public static float SumComponents(float4 v)
    {
        return v.x + v.y + v.z + v.w;
    }

    public static bool IsLeftBorder(int idx, int width)
    {
        return idx % width == 0;
    }

    public static bool IsRightBorder(int idx, int width)
    {
        return idx % width == width - 1;
    }

    public static bool IsTopBorder(int idx, int width)
    {
        return idx / width == 0;
    }

    public static bool IsBottomBorder(int idx, int width)
    {
        return idx / width == width - 1;
    }
}
