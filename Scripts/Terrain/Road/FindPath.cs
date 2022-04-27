using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile(Debug = true)]
struct FindPathJob : IJob
{   
    [ReadOnly] public NativeArray<float> heightMap;
    [WriteOnly] public NativeList<int2> path;

    public Vector3 roadStart;
    public Vector3 roadEnd;

    public int width;
    public int stepSize;
    public float maxRoadWidth;

    public void Execute()
    {
        NativeArray<Node> nodes = new NativeArray<Node>(width * width, Allocator.Temp);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {
                Node node = new Node();
                node.x = x;
                node.y = y;

                node.index = CalculateIndex(x, y, width);
                node.hCost = 0;
                node.gCost = float.MaxValue;
                node.CalculateFCost();

                node.prevIndex = -1;

                nodes[node.index] = node;
            }
        }

        NativeArray<int2> offsets = new NativeArray<int2>(16, Allocator.Temp);
        offsets[0] = new int2(1, 0);
        offsets[1] = new int2(0, 1);
        offsets[2] = new int2(-1, 0);
        offsets[3] = new int2(0, -1);
        offsets[4] = new int2(1, 1);
        offsets[5] = new int2(1, -1);
        offsets[6] = new int2(-1, 1);
        offsets[7] = new int2(-1, -1);
        offsets[8] = new int2(2, 1);
        offsets[9] = new int2(2, -1);
        offsets[10] = new int2(-2, 1);
        offsets[11] = new int2(-2, -1);
        offsets[12] = new int2(1, 2);
        offsets[13] = new int2(1, -2);
        offsets[14] = new int2(-1, 2);
        offsets[15] = new int2(-1, -2);
        
        Node startNode = nodes[CalculateIndex((int)roadStart.x, (int)roadStart.z, width)];
        startNode.gCost = 0;
        startNode.CalculateFCost();
        nodes[startNode.index] = startNode;

        Node endNode = nodes[CalculateIndex((int)roadEnd.x, (int)roadEnd.z, width)];
        float3 endNodePos = new float3(endNode.x, heightMap[endNode.index], endNode.y);

        NativeList<int> openList = new NativeList<int>(Allocator.Temp);
        NativeList<int> closedList = new NativeList<int>(Allocator.Temp);
        
        openList.Add(startNode.index);

        while (openList.Length > 0)
        {
            int currentNodeIndex = GetLowestCostFNodeIndex(openList, nodes);
            Node curNode = nodes[currentNodeIndex];
            float3 curNodePos = new float3(curNode.x, heightMap[currentNodeIndex], curNode.y);

            float distanceSqrToEndNode = math.distancesq(curNodePos, endNodePos);
            if (distanceSqrToEndNode <= stepSize * stepSize)
            {
                endNode.prevIndex = curNode.index;
                break;
            }

            // Remove current node from openlist
            for (int i = 0; i < openList.Length; i++)
            {
                if (openList[i] == currentNodeIndex)
                {
                    openList.RemoveAtSwapBack(i);
                    break;
                }
            }

            closedList.Add(currentNodeIndex);

            for (int i = 0; i < offsets.Length; i++)
            {
                int2 offset = offsets[i] * stepSize;
                int2 neighbourPos  = new int2(curNode.x + offset.x, curNode.y + offset.y);

                if (!IsPositionInsideGrid(neighbourPos, width))
                {
                    continue;
                }

                int neighbourIndex = CalculateIndex(neighbourPos.x , neighbourPos.y, width);

                if (closedList.Contains(neighbourIndex))
                {
                    continue;
                }

                Node neighbourNode = nodes[neighbourIndex];

                float neighbourHeight = heightMap[CalculateIndex(neighbourPos.x, neighbourPos.y, width)];
                float tentativeGCost = curNode.gCost + CalculateDistanceCost(curNodePos, new float3(neighbourPos.x, neighbourHeight, neighbourPos.y));
                if (tentativeGCost < neighbourNode.gCost)
                {
                    neighbourNode.prevIndex = currentNodeIndex;
                    neighbourNode.gCost = tentativeGCost;
                    neighbourNode.CalculateFCost();
                    nodes[neighbourIndex] = neighbourNode;

                    if (!openList.Contains(neighbourNode.index)) {
                        openList.Add(neighbourNode.index);
                    }
                }
            }
        }


        CalculatePath(nodes, endNode);

        offsets.Dispose();
        openList.Dispose();
        closedList.Dispose();
        nodes.Dispose();
    }

    private void CalculatePath(NativeArray<Node> nodes, Node endNode)
    {
        path.Add(new int2(endNode.x, endNode.y));

        Node curNode = endNode;
        while (curNode.prevIndex != -1)
        {
            Node prevNode = nodes[curNode.prevIndex];
            path.Add(new int2(prevNode.x, prevNode.y));
            curNode = prevNode;
        }
    }
    
    private bool IsPositionInsideGrid(int2 pos, int width)
    {
        return 
            pos.x >= 0 && pos.x < width &&
            pos.y >= 0 && pos.y < width;
    }

    private static int CalculateIndex(int x, int y, int width)
    {
        return x * width + y;
    }

    private float CalculateDistanceCost(float3 a, float3 b)
    {
        float deltaX = a.x - b.x;
        float deltaZ = a.z - b.z;
        float flatDist = Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);

        float heightDiff = Mathf.Abs(a.y - b.y);

        float slope = heightDiff / flatDist;
        float slopeCost = slope;

        float halfRoadWidth = maxRoadWidth / 2f;

        // Penalize being close to edge of chunk
        float edgeCost = 0f;
        if (b.x < halfRoadWidth || b.x > width - 1 - halfRoadWidth
            || b.y < halfRoadWidth || b.y > width - 1 - halfRoadWidth)
        {
            edgeCost = 100000f;
        }

        return slopeCost + edgeCost;
    }

    private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<Node> nodes)
    {
        Node lowestCostNode = nodes[openList[0]];
        for (int i = 1; i < openList.Length; i++)
        {
            Node testNode = nodes[openList[i]];
            if (testNode.fCost < lowestCostNode.fCost)
            {
                lowestCostNode = testNode;
            }
        }
        return lowestCostNode.index;
    }
    
    private struct Node {
        public int x;
        public int y;
        
        public int index;

        public float gCost; // Move cost
        public float hCost; // Estimated node from this node to end node
        public float fCost; // gCost + hCost

        public int prevIndex;

        public void CalculateFCost()
        {
            this.fCost = hCost + gCost;
        }
    }   
}
