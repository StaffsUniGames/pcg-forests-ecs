
using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System.Diagnostics;
using static UnityEngine.EventSystems.EventTrigger;
using Unity.Transforms;

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
    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
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

            if(treeCount == 0)
            {
                SpawnInitialTreesJob spawnJob = new SpawnInitialTreesJob
                {
                    ecb = GetEntityCommandBuffer(ref state)
                };

                spawnJob.ScheduleParallel();
            }

            //var hashMap = new NativeParallelMultiHashMap<int, int>(treeCount, state.WorldUnmanaged.UpdateAllocator.ToAllocator);

            //AssignIndexToTreeJob assignIndexJob = new AssignIndexToTreeJob
            //{
            //    parallelHashMap = hashMap.AsParallelWriter(),
            //    forest = forest
            //};

            //JobHandle hashJob = assignIndexJob.ScheduleParallel(treeQuery, state.Dependency);
            //hashJob.Complete();

            //var keys = hashMap.GetKeyArray(Allocator.Temp);

            //for (int i = 0; i < keys.Length; i++)
            //{
            //    var key = keys[i];

            //    foreach (var value in hashMap.GetValuesForKey(key))
            //        UnityEngine.Debug.Log($"key {key} has value {value}");
            //}

            //SpawnTreesJob spawnTreesJob = new SpawnTreesJob
            //{
            //    ecb = GetEntityCommandBuffer(ref state),
            //    forest = forest
            //};

            //spawnTreesJob.ScheduleParallel();
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

[BurstCompile]
public partial struct SpawnInitialTreesJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute([ChunkIndexInQuery] int chunkIndex, ref ForestComponent forest)
    {
        for (int i = 0; i < forest.m_initialTreeAmount; i++)
        {
            Entity newEntity = ecb.Instantiate(chunkIndex, forest.m_treePrefab);

            //Use NextFloat because NextFloat2 doesn't do what you'd expect
            float randX = forest.m_rng.NextFloat(forest.m_cullRegionX.min, forest.m_cullRegionX.max);
            float randY = forest.m_rng.NextFloat(forest.m_cullRegionY.min, forest.m_cullRegionY.max);

            //No swizzling, they are spawned on XY plane
            float3 randPos3 = new float3(randX, randY, 0);
            float2 randPos2 = new float2(randX, randY);

            //Add a tree component: make this entity a tree
            ecb.AddComponent(chunkIndex, newEntity, new TreeComponent
            {
                m_age = 0,
                m_deathAge = forest.m_rng.NextUInt(forest.m_deathAge.min, forest.m_deathAge.max),
                m_matureAge = forest.m_rng.NextUInt(forest.m_matureAge.min, forest.m_matureAge.max),
                m_position = randPos2
            });

            //Set the position of the entity
            ecb.SetComponent<LocalTransform>(chunkIndex, newEntity, LocalTransform.FromPosition(randPos3));
        }
    }
}

[BurstCompile]
public partial struct SpawnTreesJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;
    public ForestComponent forest;

    public void Execute([ChunkIndexInQuery] int chunkIndex, in TreeComponent tree, in Entity entity)
    {
        //TreeComponent modifiedTree = tree;
        //modifiedTree.m_age++;

        //ecb.SetComponent<TreeComponent>(chunkIndex, entity, tree);

        //if (forest.m_rng.NextFloat() > forest.m_spreadChance.max)
        //    return;

        //float2 randPos = tree.m_position + forest.m_rng.NextFloat2(new float2(forest.m_spreadDistance.max, forest.m_spreadDistance.max));

        //Entity newEntity = ecb.Instantiate(chunkIndex, forest.m_treePrefab);

        //TreeComponent newTree = new TreeComponent
        //{
        //    m_age = 0,
        //    m_deathAge = forest.m_deathAge.min,
        //    m_matureAge = forest.m_matureAge.min,
        //    m_position = randPos
        //};

        //ecb.AddComponent(chunkIndex, newEntity, newTree);
    }
}