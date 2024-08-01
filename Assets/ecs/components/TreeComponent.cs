using System;
using Unity.Entities;
using Unity.Mathematics;


/// <summary>
/// Represents a single tree within the simulation
/// </summary>
public struct TreeComponent : IComponentData
{
    /// <summary>
    /// The position of the tree
    /// </summary>
    public float2 m_position;

    /// <summary>
    /// The current age of the tree
    /// </summary>
    public uint m_age;

    /// <summary>
    /// The selected age at which this tree will be culled
    /// </summary>
    public uint m_deathAge;

    /// <summary>
    /// The selected age at which the tree will begin propogation
    /// </summary>
    public uint m_matureAge;

    /// <summary>
    /// Whether or not this tree needs to be culled
    /// </summary>
    public bool m_needsCull;

    /// <summary>
    /// The hash for this particular tree for lookups
    /// </summary>
    public int m_hash;

    /// <summary>
    /// The forest this tree is a part of
    /// </summary>
    public uint m_forestIndex;
}

/// <summary>
/// A tag component to identify trees needing cull
/// </summary>
[Obsolete("Tagged components are no longer used in this solution and should not be used.")]
public struct DeadTreeTagComponent : IComponentData { }

/// <summary>
/// A tag component to identify trees who are mature and can propogate
/// </summary>
[Obsolete("Tagged components are no longer used in this solution and should not be used.")]
public struct MatureTreeTagComponent : IComponentData { }