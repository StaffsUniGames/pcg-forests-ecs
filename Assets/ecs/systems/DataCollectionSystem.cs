//using UnityEngine;
//using Unity.Burst;
//using Unity.Jobs;
//using Unity.Entities;
//using UnityEditor;
//using System.IO;

//[UpdateBefore(typeof(ForestUpdateSystem))]
//[BurstCompile]
//public partial struct DataCollectionSystem : ISystem
//{
//	[BurstCompile]
//	public void OnCreate(ref SystemState state)
//	{
//		state.RequireForUpdate<DataCollectionBuffer>();
//		state.RequireForUpdate<ForestComponent>();

//#if UNITY_EDITOR
//		if (!EditorApplication.isPlaying) { return; }
//#endif
//		foreach (RefRO<ForestComponent> forest in SystemAPI.Query<RefRO<ForestComponent>>())
//		{
//			StreamWriter writer = File.CreateText(forest.ValueRO.m_filePath.Value.path.ToString());
//			writer.WriteLine("GridSubdivisions,EntityCount,deltaTime");
//			writer.Flush();
//			writer.Close();
//		}
//	}

//	private void SaveOutData(ForestComponent forest, ref DynamicBuffer<DataCollectionBuffer> buff)
//	{
//		//save out the date to csv
//		StreamWriter writer = new StreamWriter(forest.m_filePath.Value.path.ToString(), append: true);
//		for (int i = 0; i < buff.Length; i++)
//		{
//			writer.WriteLine($"{buff[i].gridSubdivisions},{buff[i].treeCount},{buff[i].deltaTime}");
//		}
//		writer.Flush();
//		writer.Close();
//	}

//	[BurstCompile]
//	public void OnUpdate(ref SystemState state)
//	{
//		DynamicBuffer<DataCollectionBuffer> buff = SystemAPI.GetSingletonBuffer<DataCollectionBuffer>();

//		EntityQuery treeQuery = SystemAPI.QueryBuilder().WithAll<TreeComponent>().Build();

//		bool finished = false;

//		foreach (RefRW<ForestComponent> forest in SystemAPI.Query<RefRW<ForestComponent>>())
//		{
//			buff.Add(new DataCollectionBuffer
//			{
//				gridSubdivisions = forest.ValueRO.m_spatialHasher.m_gridSize,
//				deltaTime = SystemAPI.Time.DeltaTime,
//				treeCount = treeQuery.CalculateEntityCount()
//			});

//			if(buff.Length >= 1000)
//			{
//				SaveOutData(forest.ValueRO, ref buff);

//				if (forest.ValueRO.m_spatialHasher.m_gridSize >= 256)
//				{
//					finished = true;
//					break;
//				}

//				buff.Clear();

//				forest.ValueRW.m_spatialHasher.m_gridSize++;
//				forest.ValueRW.m_InitialSeed = true;

//				buff.Add(new DataCollectionBuffer
//				{
//					gridSubdivisions = forest.ValueRO.m_spatialHasher.m_gridSize,
//					deltaTime = 0f,
//					treeCount = 0
//				});

//				new ResetForestJob
//				{
//					ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
//				}.ScheduleParallel();
//			}
//		}

//		if(finished)
//		{
//			EditorApplication.ExitPlaymode();
//		}
//	}
//}
