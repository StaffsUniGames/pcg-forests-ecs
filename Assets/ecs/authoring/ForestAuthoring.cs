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
    [Range(1, 256)]
    public int m_gridSubdivisions;
}

public class ForestBaker : Baker<ForestAuthoring>
{
    public override void Bake(ForestAuthoring authoring)
    {
        //Make an entity for the thing we will attach a forest component to
        var entity = GetEntity(TransformUsageFlags.Dynamic);

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

            //By default, wind is pointing right
            m_windDirection = new float2(1, 0),

            //Initialise RNG
            m_rng = new Unity.Mathematics.Random(authoring.m_seed)
        });
    }
}