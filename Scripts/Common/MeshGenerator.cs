using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Stella3D;
using Unity.Mathematics;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[] heightMap, MeshSettings meshSettings, int levelOfDetail)
    {
        int skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int numVertsPerLine = meshSettings.numVerticesPerLine;
        int meshWorldSize = meshSettings.meshWorldSize;

        int numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;
        int numEdgeConnectionVertices = (skipIncrement - 1) * (numVertsPerLine - 5) / skipIncrement * 4;
        int numMainVerticesPerLine = (numVertsPerLine - 5) / skipIncrement + 1;
        int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;

        int numMeshEdgeTriangles = 8 * (numVertsPerLine - 4);
        int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;

        SharedArray<Vector3, float3> verticesShared = new SharedArray<Vector3, float3>(numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices);
        SharedArray<Vector2, float2> uvsShared = new SharedArray<Vector2, float2>(verticesShared.Length);
        SharedArray<int> trianglesShared = new SharedArray<int>((numMeshEdgeTriangles + numMainTriangles) * 3);

        SharedArray<Vector3, float3> outOfMeshVerticesShared = new SharedArray<Vector3, float3>(numVertsPerLine * 4 - 4);
        NativeArray<int> outOfMeshTrianglesNat = new NativeArray<int>(24 * (numVertsPerLine - 2), Allocator.TempJob);

        SharedArray<Vector3, float3> bakedNormalsShared = new SharedArray<Vector3, float3>(verticesShared.Length);

        NativeArray<float> heightMapNative = new NativeArray<float>(heightMap, Allocator.TempJob);

        NativeArray<float3> verticesNatFloat3 = verticesShared;
        NativeArray<float2> uvsNatFloat2 = uvsShared;
        NativeArray<float3> outOfMeshVerticesNatFloat3 = outOfMeshVerticesShared;
        NativeArray<float3> bakedNormalsNatFloat3 = bakedNormalsShared;

        CalculateMeshDataJob burstJob = new CalculateMeshDataJob
        {
            vertices = verticesNatFloat3,
            uvs = uvsNatFloat2,
            triangles = trianglesShared,
            outOfMeshVertices = outOfMeshVerticesNatFloat3,
            outOfMeshTriangles = outOfMeshTrianglesNat,
            bakedNormals = bakedNormalsNatFloat3,
            heightMap = heightMapNative,
            numVertsPerLine = numVertsPerLine,
            skipIncrement = skipIncrement,
            meshWorldSize = meshWorldSize
        };
        burstJob.Schedule().Complete();


        MeshData meshData = new MeshData(
            verticesShared,
            trianglesShared,
            uvsShared,
            bakedNormalsShared
        );

        verticesShared.Dispose();
        uvsShared.Dispose();
        trianglesShared.Dispose();
        outOfMeshVerticesShared.Dispose();
        outOfMeshTrianglesNat.Dispose();
        bakedNormalsShared.Dispose();
        heightMapNative.Dispose();

        return meshData;
    }

    [BurstCompile]
    struct CalculateMeshDataJob : IJob
    {
        public NativeArray<float3> vertices;
        public NativeArray<float2> uvs;
        public NativeArray<int> triangles;

        public NativeArray<float3> outOfMeshVertices;
        public NativeArray<int> outOfMeshTriangles;

        public NativeArray<float3> bakedNormals;
        
        [ReadOnly]
        public NativeArray<float> heightMap;

        public int numVertsPerLine;
        public int skipIncrement;
        public int meshWorldSize;

        private int triangleIndex;
        private int outOfMeshTriangleIndex;

        public void Execute()
        {
            triangleIndex = 0;
            outOfMeshTriangleIndex = 0;

            CalculateTrianglesAndVertices();
            CalculateNormals();
        }

        public void CalculateTrianglesAndVertices()
        {
            NativeArray<int> vertexIndicesMap = new NativeArray<int>(numVertsPerLine * numVertsPerLine, Allocator.Temp);
            int meshVertexIndex = 0;
            int outOfMeshVertexIndex = -1;

            for (int x = 0; x < numVertsPerLine; x++)
            {
                for (int y = 0; y < numVertsPerLine; y++)
                {
                    bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                    bool isSkippedVertex = x > 2
                                        && x < numVertsPerLine - 3
                                        && y > 2
                                        && y < numVertsPerLine - 3
                                        && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                    if (isOutOfMeshVertex)
                    {
                        vertexIndicesMap[x * numVertsPerLine + y] = outOfMeshVertexIndex;
                        outOfMeshVertexIndex--;
                    }
                    else if (!isSkippedVertex)
                    {
                        vertexIndicesMap[x * numVertsPerLine + y] = meshVertexIndex;
                        meshVertexIndex++;
                    }
                }
            }

            /* Vertices are represented as follows:
            *
            * R = out of mesh vertices (not drawn but needed for correct edge normals)
            * O = Mesh edge vertices (used for high resolution edges) 
            * P = Main vertices (used for main terrain)
            * G = edge connection vertices (normally skipped but needed for high resolution edges)
            * Z = skipped vertices
            *
            * R R R R R R R R R R R R R
            * R O O O O O O O O O O O R
            * R O P G G G P G G G P O R
            * R O G Z Z Z Z Z Z Z G O R
            * R O G Z Z Z Z Z Z Z G O R
            * R O G Z Z Z Z Z Z Z G O R
            * R O P Z Z Z P Z Z Z P O R
            * R O G Z Z Z Z Z Z Z G O R
            * R O G Z Z Z Z Z Z Z G O R
            * R O G Z Z Z Z Z Z Z G O R
            * R O P G G G P G G G P O R
            * R O O O O O O O O O O O R
            * R R R R R R R R R R R R R
            *
            */
            float2 vertexPosition2D = new float2();
            float2 percent = new float2();
            for (int x = 0; x < numVertsPerLine; x++)
            {
                for (int y = 0; y < numVertsPerLine; y++)
                {
                    bool isSkippedVertex = x > 2
                                        && x < numVertsPerLine - 3
                                        && y > 2
                                        && y < numVertsPerLine - 3
                                        && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

                    if (!isSkippedVertex)
                    {
                        bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                        bool isMeshEdgeVertex = y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2 && !isOutOfMeshVertex;
                        bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
                        bool isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3)
                                                    && !isOutOfMeshVertex
                                                    && !isMeshEdgeVertex
                                                    && !isMainVertex;

                        int vertexIndex = vertexIndicesMap[x * numVertsPerLine + y];

                        percent.x = (float)(x - 1) / (numVertsPerLine - 3);
                        percent.y = (float)(y - 1) / (numVertsPerLine - 3);
                        vertexPosition2D.x = percent.x * meshWorldSize;
                        vertexPosition2D.y = percent.y * meshWorldSize;

                        float height = heightMap[x * numVertsPerLine + y];

                        if (isEdgeConnectionVertex)
                        {
                            bool isVertical = x == 2 || x == numVertsPerLine - 3;
                            int dstToMainVertexA = (isVertical ? y - 2 : x - 2) % skipIncrement;
                            int dstToMainVertexB = skipIncrement - dstToMainVertexA;
                            float dstPercentFromAToB = dstToMainVertexA / (float)skipIncrement;

                            int ax = (isVertical) ? x : x - dstToMainVertexA;
                            int ay = (isVertical) ? y - dstToMainVertexA : y;
                            float heightMainVertexA = heightMap[ax * numVertsPerLine + ay];
                            int bx = (isVertical) ? x : x + dstToMainVertexB;
                            int by = (isVertical) ? y + dstToMainVertexB : y;
                            float heightMainVertexB = heightMap[bx * numVertsPerLine + by];

                            height = (1 - dstPercentFromAToB) * heightMainVertexA + dstPercentFromAToB * heightMainVertexB;
                        }

                        AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);

                        bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));

                        if (createTriangle)
                        {
                            int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3) ? skipIncrement : 1;

                            int a = vertexIndicesMap[x * numVertsPerLine + y];
                            int b = vertexIndicesMap[(x + currentIncrement) * numVertsPerLine + y];
                            int c = vertexIndicesMap[x * numVertsPerLine + y + currentIncrement];
                            int d = vertexIndicesMap[(x + currentIncrement) * numVertsPerLine + y + currentIncrement];
                            AddTriangle(a, c, d);
                            AddTriangle(d, b, a);
                        }
                    }
                }
            }
            vertexIndicesMap.Dispose();
        }

        public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
        {
            if (vertexIndex < 0)
            { // Border index
                outOfMeshVertices[-vertexIndex - 1] = vertexPosition;
            }
            else
            { // Mesh index
                vertices[vertexIndex] = vertexPosition;
                uvs[vertexIndex] = uv;
            }
        }

        public void AddTriangle(int a, int b, int c)
        {
            if (a < 0 || b < 0 || c < 0)
            {
                outOfMeshTriangles[outOfMeshTriangleIndex] = a;
                outOfMeshTriangles[outOfMeshTriangleIndex + 1] = b;
                outOfMeshTriangles[outOfMeshTriangleIndex + 2] = c;
                outOfMeshTriangleIndex += 3;
            }
            else
            {
                triangles[triangleIndex] = a;
                triangles[triangleIndex + 1] = b;
                triangles[triangleIndex + 2] = c;
                triangleIndex += 3;
            }
        }

        public void CalculateNormals()
        {
            for (int i = 0; i < triangles.Length / 3; i++)
            {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = triangles[normalTriangleIndex];
                int vertexIndexB = triangles[normalTriangleIndex + 1];
                int vertexIndexC = triangles[normalTriangleIndex + 2];

                float3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                bakedNormals[vertexIndexA] += triangleNormal;
                bakedNormals[vertexIndexB] += triangleNormal;
                bakedNormals[vertexIndexC] += triangleNormal;
            }

            for (int i = 0; i < outOfMeshTriangles.Length / 3; i++)
            {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
                int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
                int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];

                float3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                if (vertexIndexA >= 0)
                {
                    bakedNormals[vertexIndexA] += triangleNormal;
                }
                if (vertexIndexB >= 0)
                {
                    bakedNormals[vertexIndexB] += triangleNormal;
                }
                if (vertexIndexC >= 0)
                {
                    bakedNormals[vertexIndexC] += triangleNormal;
                }
            }
            int length = bakedNormals.Length;
            for (int i = 0; i < length; i++)
            {
                math.normalize(bakedNormals[i]);
            }
        }

        float3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
        {
            float3 pointA = (indexA < 0) ? outOfMeshVertices[-indexA - 1] : vertices[indexA];
            float3 pointB = (indexB < 0) ? outOfMeshVertices[-indexB - 1] : vertices[indexB];
            float3 pointC = (indexC < 0) ? outOfMeshVertices[-indexC - 1] : vertices[indexC];

            float3 sideAB = pointB - pointA;
            float3 sideAC = pointC - pointA;

            return math.normalize(math.cross(sideAB, sideAC));
        }
    }
}

public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    public MeshData(
        SharedArray<Vector3, float3> verticesNat,
        SharedArray<int> trianglesNat,
        SharedArray<Vector2, float2> uvsNat,
        SharedArray<Vector3, float3> bakedNormalsNat
    )
    {
        vertices = verticesNat;
        triangles = trianglesNat;
        uvs = uvsNat;
        bakedNormals = bakedNormalsNat;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = bakedNormals;

        return mesh;
    }
}