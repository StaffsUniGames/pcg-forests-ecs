
using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System.Diagnostics;

public partial struct ForestUpdateSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        //Build a query for any forests
        EntityQuery forestQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<ForestComponent>()
                .Build(ref state);

        //Require at least one for update
        state.RequireAnyForUpdate(forestQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var forest in SystemAPI.Query<ForestComponent>())
        {
            //This current gets all trees regardless of what "forest" they are in; 
            //TODO: make trees reliant on forest index
            var treeQuery = SystemAPI.QueryBuilder().WithAll<TreeComponent>().Build();
            var treeCount = treeQuery.CalculateEntityCount();

            var hashMap = new NativeParallelMultiHashMap<int, int>(treeCount, state.WorldUnmanaged.UpdateAllocator.ToAllocator);

            AssignIndexToTreeJob assignIndexJob = new AssignIndexToTreeJob
            {
                parallelHashMap = hashMap.AsParallelWriter(),
                forest = forest
            };

            JobHandle hashJob = assignIndexJob.ScheduleParallel(treeQuery, state.Dependency);
            hashJob.Complete();

            var keys = hashMap.GetKeyArray(Allocator.Temp);

            for(int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];

                foreach (var value in hashMap.GetValuesForKey(key))
                    UnityEngine.Debug.Log($"key {key} has value {value}");
            }
        }
    }
}

[BurstCompile]
public partial struct AssignIndexToTreeJob : IJobEntity
{
    public NativeParallelMultiHashMap<int, int>.ParallelWriter parallelHashMap;
    public ForestComponent forest;

    public void Execute([EntityIndexInQuery] int entityIndex, in TreeComponent tree)
    {
        var hash = forest.m_spatialHasher.Hash(tree.m_position);
        parallelHashMap.Add(hash, entityIndex);
    }
}