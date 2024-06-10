using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct ForestComponent : IComponentData
{
    /// <summary>
    /// The prefab to spawn for trees
    /// </summary>
    public Entity m_treePrefab;

    /// <summary>
    /// The hash map of trees, for spatial hashing purposes
    /// </summary>
    public NativeParallelMultiHashMap<int, NativeArray<TreeComponent>> m_hashMap;

    /// <summary>
    /// Represents a spatial hasher for hashing 2D coordinates to 1D indices
    /// </summary>
    public SpatialHasher m_spatialHasher;

    /// <summary>
    /// The generator for providing RNG
    /// </summary>
    public Unity.Mathematics.Random m_rng;

    //--

    /// <summary>
    /// The wind direction within the forest
    /// </summary>
    public float2 m_windDirection;

    /// <summary>
    /// The min/max mature age of trees in the forest
    /// </summary>
    public Range<uint> m_matureAge;

    /// <summary>
    /// The min/max death age of trees in the forest
    /// </summary>
    public Range<uint> m_deathAge;

    //--

    /// <summary>
    /// The global spread chance of trees in the forest
    /// </summary>
    public Range<float> m_spreadChance;

    /// <summary>
    /// The global spread distance of new trees in the forest
    /// </summary>
    public Range<float> m_spreadDistance;

    //--

    /// <summary>
    /// The min/max X extents of the cull region
    /// </summary>
    public Range<float> m_cullRegionX;

    /// <summary>
    /// The min/max Y extents of the cull region
    /// </summary>
    public Range<float> m_cullRegionY;
}

