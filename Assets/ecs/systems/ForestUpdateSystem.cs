
using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Rendering;
using System.Drawing;

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
		state.World.MaximumDeltaTime = 100f;
    }
    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = GetEntityCommandBuffer(ref state);

        foreach ((RefRW<ForestComponent> forest, Entity entity) in SystemAPI.Query<RefRW<ForestComponent>>().WithEntityAccess())
        {
            BufferLookup<TreePrefabItem> treePrefabLookup = state.GetBufferLookup<TreePrefabItem>(true);

            //This current gets all trees regardless of what "forest" they are in; 
            //TODO: make trees reliant on forest index
            var treeQuery = SystemAPI.QueryBuilder().WithAll<TreeComponent>().Build();
            var treeCount = treeQuery.CalculateEntityCount();

            state.Dependency = new UpdateForestJob { }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

			if (treeCount == 0 || forest.ValueRO.m_InitialSeed)
			{
				UnityEngine.Debug.Log("spawning initial trees");

                //Schedule job to spawn initial number of trees
				state.Dependency = new SpawnInitialTreesJob { ecb = ecb, prefabLookup = treePrefabLookup }.ScheduleParallel(state.Dependency);
                state.Dependency.Complete();
                
				forest.ValueRW.m_InitialSeed = false;
			}
			else
			{
                //Build lookup
				var treeLookup = SystemAPI.GetComponentLookup<TreeComponent>();

                //Make native multi hashmap
				var hashMap = new NativeParallelMultiHashMap<int, TreeComponent>(treeCount, state.WorldUnmanaged.UpdateAllocator.ToAllocator);

                //Find hashmap for all trees
				state.Dependency = new AssignIndexToTreeJob { parallelHashMap = hashMap.AsParallelWriter(), forest = forest.ValueRO }.ScheduleParallel(state.Dependency);
				state.Dependency.Complete();

                //Cull trees, then propogate, and finally calculate competition
				new CullDeadTreesJob { ecb = ecb }.ScheduleParallel();
                new SpawnTreesJob { ecb = ecb, forest = forest.ValueRO, forestEntity = entity, prefabLookup = treePrefabLookup }.ScheduleParallel();
				new FONCompetitionJob { ecb = ecb, forest = forest.ValueRO, hashMap = hashMap, treeLookup = treeLookup }.ScheduleParallel();
			}
        }
    }
}

[BurstCompile]
public partial struct UpdateForestJob : IJobEntity
{
    public void Execute([ChunkIndexInQuery] int chunkIndex, ref ForestComponent forest)
    {
        forest.m_windDirection = forest.m_rng.NextFloat(math.PI * 2);
    }
}

[BurstCompile]
public partial struct FONCompetitionJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;
    public ForestComponent forest;
    
    [ReadOnly] public NativeParallelMultiHashMap<int, TreeComponent> hashMap;

    [NativeDisableParallelForRestriction]
    [NativeDisableContainerSafetyRestriction]
    public ComponentLookup<TreeComponent> treeLookup;

    public void Execute([EntityIndexInQuery] int entityIndex, ref TreeComponent tree, ref URPMaterialPropertyBaseColor treeColour,  in Entity entity)
    {
        //Already culled? No point considering it
        if (tree.m_needsCull)
            return;

        float delta = 0;
        if(tree.m_age < tree.m_matureAge)
        {
            delta = (float)tree.m_age / (float)tree.m_matureAge;
            treeColour.Value = new float4(0, math.lerp(0.5f, 1f, delta), 0, 1);
        }
        else 
        {
            delta = ((float)tree.m_age) / ((float)tree.m_deathAge);
            treeColour.Value = new float4(0, math.lerp(1f, 0.25f, delta), 0, 1);

        }
 
        var entities = hashMap.GetValuesForKey(tree.m_hash);

        foreach(var other in entities)
        {
            var dist = math.distance(other.m_position, tree.m_position);

            //They're the same
            if (dist <= math.EPSILON)
                continue;

            //Not in competition? skip
            if (math.distance(other.m_position, tree.m_position) > 1.0f)
                continue;

            //Otherwise..
            //float maturityA = (tree.m_age / tree.m_deathAge) * forest.m_spreadDistance.min;
            //float maturityB = (other.m_age / other.m_deathAge) * forest.m_spreadDistance.min;

            //Other larger than this one, set to be culled
            if (tree.m_age > other.m_age)
                tree.m_needsCull = true;
        }
    }
}

[BurstCompile]
public partial struct AssignIndexToTreeJob : IJobEntity
{
    public NativeParallelMultiHashMap<int, TreeComponent>.ParallelWriter parallelHashMap;
    public ForestComponent forest;

    public void Execute([EntityIndexInQuery] int entityIndex, ref TreeComponent tree)
    {
        var hash = forest.m_spatialHasher.Hash(tree.m_position);
        tree.m_hash = hash;
        parallelHashMap.Add(hash, tree);
    }
}

[BurstCompile]
public partial struct CullDeadTreesJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute([ChunkIndexInQuery] int chunkIndex, in TreeComponent tree, in Entity entity)
    {
        //This culls only entities with a DeadTreeTagComponent attached to them,
        //look at the query given by the params above. Dead tree must be ref'd with
        //in rather than ref
        if(tree.m_needsCull)
            ecb.DestroyEntity(chunkIndex, entity);
    }
}

public static class DynamicBufferExtensions
{
    /// <summary>
    /// Selects a random element of the buffer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="buf"></param>
    /// <param name="rng"></param>
    /// <returns></returns>
    public static T SelectRandom<T>(this in DynamicBuffer<T> buf, in Unity.Mathematics.Random rng) where T : unmanaged
        => buf[rng.NextInt(0, buf.Length)];

    /// <summary>
    /// Selects the first element of the buffer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="buf"></param>
    /// <returns></returns>
    public static T SelectFirst<T>(this in DynamicBuffer<T> buf) where T : unmanaged => buf[0];
}

[BurstCompile]
public partial struct SpawnInitialTreesJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;
    [ReadOnly] public BufferLookup<TreePrefabItem> prefabLookup;

    public void Execute([ChunkIndexInQuery] int chunkIndex, ref ForestComponent forest, in Entity entity)
    {
        DynamicBuffer<TreePrefabItem> prefabBuf = prefabLookup[entity];

        for (int i = 0; i < forest.m_initialTreeAmount; i++)
        {
            Entity newEntity = ecb.Instantiate(chunkIndex, prefabBuf.SelectRandom(forest.m_rng).prefab);

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
                m_position = randPos2,
                m_needsCull = false
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
    public Entity forestEntity;
    [ReadOnly] public BufferLookup<TreePrefabItem> prefabLookup;

    public void Execute([ChunkIndexInQuery] int chunkIndex, ref TreeComponent tree, in Entity entity)
    {
        //TreeComponent modifiedTree = tree;
        tree.m_age++;

        //Set to needing cull
        if (tree.m_age > tree.m_deathAge)
            tree.m_needsCull = true;

        //Check if mature or not
        if (tree.m_age < tree.m_matureAge)
            return;

        //forest.m_rng.InitState((uint)chunkIndex);
        float randChance = forest.m_rng.NextFloat(forest.m_spreadChance.min, forest.m_spreadChance.max);

        if (forest.m_rng.NextFloat() > randChance)
            return;

        //----

        DynamicBuffer<TreePrefabItem> prefabs = prefabLookup[forestEntity];
        Entity randomPrefab = prefabs.SelectRandom(forest.m_rng).prefab;

        //Use NextFloat because NextFloat2 doesn't do what you'd expect
        float a = forest.m_windDirection;
        float d = forest.m_rng.NextFloat(forest.m_spreadDistance.min, forest.m_spreadDistance.max);

        float randX = tree.m_position.x + math.sin(a) * d;
        float randY = tree.m_position.y + math.cos(a) * d;

        //Wrap around if x/y limits exceeded
        if (randX > forest.m_cullRegionX.max) randX %= forest.m_cullRegionX.max;
        if (randY > forest.m_cullRegionY.max) randY %= forest.m_cullRegionY.max;
        //--
        if (randX < forest.m_cullRegionX.min) randX = forest.m_cullRegionX.max - math.abs(randX);
        if (randY < forest.m_cullRegionY.min) randY = forest.m_cullRegionY.max - math.abs(randY);

        //No swizzling, they are spawned on XY plane
        float3 randPos3 = new float3(randX, randY, 0);
        float2 randPos2 = new float2(randX, randY);

        //Instantiate the new tree
        Entity newEntity = ecb.Instantiate(chunkIndex, randomPrefab);

        //Build local transform
        float scale = 1;

        //Scale needs randomising?
        if (forest.m_randomiseScale)
            scale = forest.m_rng.NextFloat(0.85f, 1.0f);

        if (forest.m_alignTreesAlongXZ)
            randPos3 = new float3(randPos3.x, 0, randPos3.y);

        //Build T*R*S from what we have so far
        LocalTransform trans = LocalTransform.FromPositionRotationScale(randPos3, quaternion.identity, scale);

        if (forest.m_alignTreesAlongXZ)
        {
            //Looks along z-axis with fwd = vec3.up
            //quaternion rot = quaternion.LookRotationSafe(new float3(0, 0, 1), new float3(0, 0, 1));
            //trans = LocalTransform.FromPositionRotationScale(randPos3, rot, scale);
        }

        //Set the position of the entity
        ecb.SetComponent(chunkIndex, newEntity, trans);

        //Add a tree component: make this entity a tree
        ecb.AddComponent(chunkIndex, newEntity, new TreeComponent
        {
            m_age = 0,
            m_deathAge = forest.m_rng.NextUInt(forest.m_deathAge.min, forest.m_deathAge.max),
            m_matureAge = forest.m_rng.NextUInt(forest.m_matureAge.min, forest.m_matureAge.max),
            m_position = randPos2,
            m_needsCull = false
        });
    }
}

[BurstCompile]
public partial struct ResetForestJob : IJobEntity
{
	public EntityCommandBuffer.ParallelWriter ecb;

	public void Execute([ChunkIndexInQuery] int chunkIndex, in TreeComponent tree, in Entity entity)
	{
		ecb.DestroyEntity(chunkIndex, entity);
	}
}