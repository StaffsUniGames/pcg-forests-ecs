using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[AddComponentMenu("ECS Forests/Authoring/Forest Authoring")]
public class ForestAuthoring : MonoBehaviour
{
    [Header("RNG Parameters"), Tooltip("The seed for random generation")]
    public uint m_seed;

    [Range(1, 150)]
    public uint m_initialTreeAmount;

    [Header("Assignables")]
    public GameObject[] m_treePrefabs;

    [Header("Age-related parameters")]
    public Range<uint> m_matureAge;
    public Range<uint> m_deathAge;

    [Header("Spread parameters")]
    public Range<float> m_spreadChance;
    public Range<float> m_spreadDistance;

    [Header("Cull region parameters")]
    public Range<float> m_cullRegionX;
    public Range<float> m_cullRegionY;

	[Header("Spatial hashing parameters")]
	public bool m_collectFullDataSet;
	[Range(1, 256)]
	public int m_gridSubdivisions;

	[Header("Visualisation parameters")]
	public bool m_alignAlongXZ;

	public bool m_randomiseScale;
}

public struct TreePrefabItem : IBufferElementData
{
	public Entity prefab;
}

public class ForestBaker : Baker<ForestAuthoring>
{
    public override void Bake(ForestAuthoring authoring)
    {
        //Make an entity for the thing we will attach a forest component to
        var entity = GetEntity(TransformUsageFlags.Dynamic);

		//TODO: Fix the implication that this is from (0, 0)
        float2 worldSize;
        worldSize.x = authoring.m_cullRegionX.max;
        worldSize.y = authoring.m_cullRegionY.max;

		//This is done as unmanaged strings are a nightmare
		BlobBuilder builder = new BlobBuilder(Allocator.Temp);
		ref FilePath filePath = ref builder.ConstructRoot<FilePath>();
		builder.AllocateString(ref filePath.path, Application.dataPath + "/Data/Results_" + System.DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".csv");
		BlobAssetReference<FilePath> filePathBlob = builder.CreateBlobAssetReference<FilePath>(Allocator.Persistent);
		builder.Dispose();
		AddBlobAsset<FilePath>(ref filePathBlob, out var hash);

		//Get all the prefabs for this forest
		NativeArray<Entity> prefabs = new NativeArray<Entity>(authoring.m_treePrefabs.Length, Allocator.Persistent);

		//Run throguh each, convert to entity
		for(int i = 0; i < authoring.m_treePrefabs.Length; i++)
			prefabs[i] = GetEntity(authoring.m_treePrefabs[i], TransformUsageFlags.Dynamic);

		//Add a buffer to this forest and...
		DynamicBuffer<TreePrefabItem> buffer = this.AddBuffer<TreePrefabItem>(entity);

		//... fill it with the prefab entities
		foreach (var prefab in authoring.m_treePrefabs)
			buffer.Add(new TreePrefabItem { prefab = GetEntity(prefab, TransformUsageFlags.Dynamic) });

		AddComponent(entity, new ForestComponent
		{
			m_cullRegionX = authoring.m_cullRegionX,
			m_cullRegionY = authoring.m_cullRegionY,

			m_deathAge = authoring.m_deathAge,
			m_matureAge = authoring.m_matureAge,

			m_spreadChance = authoring.m_spreadChance,
			m_spreadDistance = authoring.m_spreadDistance,

			m_initialTreeAmount = authoring.m_initialTreeAmount,
			m_spatialHasher = new SpatialHasher(worldSize, (authoring.m_collectFullDataSet ? 1 : authoring.m_gridSubdivisions)),

			m_windDirection = 0,

			m_filePath = filePathBlob,
			m_InitialSeed = true,

			m_alignTreesAlongXZ = authoring.m_alignAlongXZ,
			m_randomiseScale = authoring.m_randomiseScale,

			//Initialise RNG
			m_rng = new Unity.Mathematics.Random(authoring.m_seed),

			//Assign index and update number of forests
			m_forestIndex = ForestComponent.m_forestCount++,
		});

		if (authoring.m_collectFullDataSet)
		{
			DynamicBuffer<DataCollectionBuffer> buff = AddBuffer<DataCollectionBuffer>(entity);
			buff.Add(new DataCollectionBuffer
			{
				gridSubdivisions = 1,
				deltaTime = 0f,
				treeCount = 0
			});
		}
	}
}