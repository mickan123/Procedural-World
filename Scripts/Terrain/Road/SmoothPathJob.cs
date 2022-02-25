using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

[BurstCompile]
struct SmoothPathJob : IJob
{
    public NativeList<Vector3> smoothedPoints;
    public NativeArray<Vector3> path;

    public void Execute() 
    {
        int pointsLength = path.Length;
        NativeArray<Vector3> points = new NativeArray<Vector3>(path.Length, Allocator.Temp);
        int curvedLength = (pointsLength * Mathf.RoundToInt(RoadSettings.smoothness)) - 1;

        float t = 0.0f;
        for (int pointInTimeOnCurve = 0; pointInTimeOnCurve < curvedLength + 1; pointInTimeOnCurve++)
        {
            t = Mathf.InverseLerp(0, curvedLength, pointInTimeOnCurve);

            points.CopyFrom(path);

            for (int j = pointsLength - 1; j > 0; j--)
            {
                for (int i = 0; i < j; i++)
                {
                    points[i] = (1 - t) * points[i] + t * points[i + 1];
                }
            }
            smoothedPoints.Add(points[0]);
        }

        points.Dispose();
    }
}