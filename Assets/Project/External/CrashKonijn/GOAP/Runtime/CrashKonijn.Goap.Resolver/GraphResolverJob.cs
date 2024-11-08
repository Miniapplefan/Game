﻿using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace CrashKonijn.Goap.Resolver
{
    [BurstCompile]
    public struct NodeData
    {
        public int Index;
        public float G;
        public float H;
        public int ParentIndex;
    
        public float F => this.G + this.H;
    }

    [BurstCompile]
    public struct RunData
    {
        public int StartIndex;
        // Index = NodeIndex
        public NativeArray<bool> IsExecutable;
        // Index = ConditionIndex
        public NativeArray<bool> ConditionsMet;
        public NativeArray<float3> Positions;
        public NativeArray<float> Costs;
        public float DistanceMultiplier;
    }

    [BurstCompile]
    public struct NodeSorter : IComparer<NodeData>
    {
        public int Compare(NodeData x, NodeData y)
        {
            return x.F.CompareTo(y.F);
        }
    }

    [BurstCompile]
    public struct GraphResolverJob : IJob
    {
        // Graph specific
#if UNITY_COLLECTIONS_2_1
        // Dictionary<ActionIndex, ConditionIndex[]>
        [ReadOnly] public NativeParallelMultiHashMap<int, int> NodeConditions;
        // Dictionary<ConditionIndex, NodeIndex[]>
        [ReadOnly] public NativeParallelMultiHashMap<int, int> ConditionConnections;
#else
        // Dictionary<ActionIndex, ConditionIndex[]>
        [ReadOnly] public NativeMultiHashMap<int, int> NodeConditions;
        // Dictionary<ConditionIndex, NodeIndex[]>
        [ReadOnly] public NativeMultiHashMap<int, int> ConditionConnections;
#endif

        // Resolve specific
        [ReadOnly] public RunData RunData;

        // Results
        public NativeList<NodeData> Result;
        
        public static readonly float3 InvalidPosition = new float3(float.MaxValue, float.MaxValue, float.MaxValue);

        [BurstCompile]
        public void Execute()
        {
            var nodeCount = this.NodeConditions.Count();
            var runData = this.RunData;
        
            var openSet = new NativeHashMap<int, NodeData>(nodeCount, Allocator.Temp);
            var closedSet = new NativeHashMap<int, NodeData>(nodeCount, Allocator.Temp);
        
            var nodeData = new NodeData
            {
                Index = runData.StartIndex,
                G = 0,
                H = int.MaxValue,
                ParentIndex = -1
            };
            openSet.Add(runData.StartIndex, nodeData);

            while (!openSet.IsEmpty)
            {
                var openList = openSet.GetValueArray(Allocator.Temp);
                openList.Sort(new NodeSorter());
            
                var currentNode = openList[0];

                if (runData.IsExecutable[currentNode.Index])
                {
                    this.RetracePath(currentNode, closedSet, this.Result);
                    break;
                }

                closedSet.TryAdd(currentNode.Index, currentNode);
                openSet.Remove(currentNode.Index);
                
                // If this node has a condition that is false and has no connections, it is unresolvable
                if (this.HasUnresolvableCondition(currentNode.Index))
                {
                    continue;
                }

                foreach (var conditionIndex in this.NodeConditions.GetValuesForKey(currentNode.Index))
                {
                    if (runData.ConditionsMet[conditionIndex])
                    {
                        continue;
                    }
                    
                    foreach (var neighborIndex in this.ConditionConnections.GetValuesForKey(conditionIndex))
                    {
                        if (closedSet.ContainsKey(neighborIndex))
                        {
                            continue;
                        }
                
                        var newG = currentNode.G + this.RunData.Costs[neighborIndex];
                        NodeData neighbor;
                
                        // Current neighbour is not in the open set
                        if (!openSet.TryGetValue(neighborIndex, out neighbor))
                        {
                            neighbor = new NodeData
                            {
                                Index = neighborIndex,
                                G = newG,
                                H = this.Heuristic(neighborIndex, currentNode.Index),
                                ParentIndex = currentNode.Index
                            };
                            openSet.Add(neighborIndex, neighbor);
                            continue;
                        }
                
                        // This neighbour has a lower cost
                        if (newG < neighbor.G)
                        {
                            neighbor.G = newG;
                            neighbor.ParentIndex = currentNode.Index;
                    
                            openSet.Remove(neighborIndex);
                            openSet.Add(neighborIndex, neighbor);
                        }
                    }
                }

                openList.Dispose();
            }

            openSet.Dispose();
            closedSet.Dispose();
        }

        private float Heuristic(int currentIndex, int previousIndex)
        {
            var previousPosition = this.RunData.Positions[previousIndex];
            var currentPosition = this.RunData.Positions[currentIndex];

            if (previousPosition.Equals(InvalidPosition) || currentPosition.Equals(InvalidPosition))
            {
                return 0f;
            }

            return math.distance(previousPosition, currentPosition) * this.RunData.DistanceMultiplier;
        }

        private void RetracePath(NodeData startNode, NativeHashMap<int, NodeData> closedSet, NativeList<NodeData> path)
        {
            var currentNode = startNode;
            while (currentNode.ParentIndex != -1)
            {
                path.Add(currentNode);
                currentNode = closedSet[currentNode.ParentIndex];
            }
        }

        private bool HasUnresolvableCondition(int currentIndex)
        {
            foreach (var conditionIndex in this.NodeConditions.GetValuesForKey(currentIndex))
            {
                if (this.RunData.ConditionsMet[conditionIndex])
                {
                    continue;
                }
                    
                if (!this.ConditionConnections.GetValuesForKey(conditionIndex).MoveNext())
                {
                    return true;
                }
            }

            return false;
        }
    }
}