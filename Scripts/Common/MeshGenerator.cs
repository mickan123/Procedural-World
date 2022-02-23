﻿using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[][] heightMap, MeshSettings meshSettings, int levelOfDetail)
    {
        int skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int numVertsPerLine = meshSettings.numVerticesPerLine;
        int meshWorldSize = meshSettings.meshWorldSize;

        MeshData meshData = new MeshData(numVertsPerLine, skipIncrement);

        int[][] vertexIndicesMap = new int[numVertsPerLine][];
        for (int i = 0; i < numVertsPerLine; i++)
        {
            vertexIndicesMap[i] = new int[numVertsPerLine];
        }
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
                    vertexIndicesMap[x][y] = outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                }
                else if (!isSkippedVertex)
                {
                    vertexIndicesMap[x][y] = meshVertexIndex;
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
        Vector2 vertexPosition2D = new Vector2();
        Vector2 percent = new Vector2();
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

                    int vertexIndex = vertexIndicesMap[x][y];
                    percent.Set((float)(x - 1) / (numVertsPerLine - 3), (float)(y - 1) / (numVertsPerLine - 3));
                    vertexPosition2D.Set(percent.x * meshWorldSize, percent.y * meshWorldSize);

                    float height = heightMap[x][y];

                    if (isEdgeConnectionVertex)
                    {
                        bool isVertical = x == 2 || x == numVertsPerLine - 3;
                        int dstToMainVertexA = (isVertical ? y - 2 : x - 2) % skipIncrement;
                        int dstToMainVertexB = skipIncrement - dstToMainVertexA;
                        float dstPercentFromAToB = dstToMainVertexA / (float)skipIncrement;

                        float heightMainVertexA = heightMap[(isVertical) ? x : x - dstToMainVertexA][(isVertical) ? y - dstToMainVertexA : y];
                        float heightMainVertexB = heightMap[(isVertical) ? x : x + dstToMainVertexB][(isVertical) ? y + dstToMainVertexB : y];

                        height = (1 - dstPercentFromAToB) * heightMainVertexA + dstPercentFromAToB * heightMainVertexB;
                    }

                    meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);

                    bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));

                    if (createTriangle)
                    {
                        int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3) ? skipIncrement : 1;

                        int a = vertexIndicesMap[x][y];
                        int b = vertexIndicesMap[x + currentIncrement][y];
                        int c = vertexIndicesMap[x][y + currentIncrement];
                        int d = vertexIndicesMap[x + currentIncrement][y + currentIncrement];
                        meshData.AddTriangle(a, c, d);
                        meshData.AddTriangle(d, b, a);
                    }
                }
            }
        }

        meshData.CalculateNormals();

        return meshData;
    }
}

public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    Vector3[] outOfMeshVertices;
    int[] outOfMeshTriangles;

    int triangleIndex;
    int outOfMeshTriangleIndex;

    public MeshData(int numVertsPerLine, int skipIncrement)
    {
        int numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;
        int numEdgeConnectionVertices = (skipIncrement - 1) * (numVertsPerLine - 5) / skipIncrement * 4;
        int numMainVerticesPerLine = (numVertsPerLine - 5) / skipIncrement + 1;
        int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;

        vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];
        uvs = new Vector2[vertices.Length];

        int numMeshEdgeTriangles = 8 * (numVertsPerLine - 4);
        int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;
        triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];

        outOfMeshVertices = new Vector3[numVertsPerLine * 4 - 4];
        outOfMeshTriangles = new int[24 * (numVertsPerLine - 2)];
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
        bakedNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            bakedNormals[vertexIndexA] += triangleNormal;
            bakedNormals[vertexIndexB] += triangleNormal;
            bakedNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = outOfMeshTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
            int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
            int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
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
            bakedNormals[i].Normalize();
        }
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? outOfMeshVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? outOfMeshVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? outOfMeshVertices[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
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