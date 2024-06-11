﻿using Unity.Collections;
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
    /// The generator for providing RNG
    /// </summary>
    public Unity.Mathematics.Random m_rng;

    //--

    /// <summary>
    /// A spatial hasher for the forest
    /// </summary>
    public SpatialHasher m_spatialHasher;
    
    /// <summary>
    /// The amount of initial trees to spawn
    /// </summary>
    public uint m_initialTreeAmount;

    /// <summary>
    /// The wind direction within the forest
    /// </summary>
    public float m_windDirection;

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

