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
    public GameObject m_treePrefab;

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
}

public class ForestBaker : Baker<ForestAuthoring>
{
    public override void Bake(ForestAuthoring authoring)
    {
        //Make an entity for the thing we will attach a forest component to
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        float2 worldSize;
        worldSize.x = authoring.m_cullRegionX.max;
        worldSize.y = authoring.m_cullRegionY.max;

		BlobBuilder builder = new BlobBuilder(Allocator.Temp);
		ref FilePath filePath = ref builder.ConstructRoot<FilePath>();
		builder.AllocateString(ref filePath.path, Application.dataPath + "/Data/Results_" + System.DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".csv");
		BlobAssetReference<FilePath> filePathBlob = builder.CreateBlobAssetReference<FilePath>(Allocator.Persistent);
		builder.Dispose();
		AddBlobAsset<FilePath>(ref filePathBlob, out var hash);

		AddComponent(entity, new ForestComponent
		{
			//Set prefab from one in authoring
			m_treePrefab = GetEntity(authoring.m_treePrefab, TransformUsageFlags.Dynamic),

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

			//Initialise RNG
			m_rng = new Unity.Mathematics.Random(authoring.m_seed)
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