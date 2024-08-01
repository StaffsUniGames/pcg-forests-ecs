using Unity.Entities;
using Unity.Mathematics;


/// <summary>
/// Represents a single tree within the simulation
/// </summary>
public struct TreeComponent : IComponentData
{
    public float2 m_position;
    public uint m_age;
    public uint m_deathAge;
    public uint m_matureAge;
    public bool m_needsCull;
    public int m_hash;
    public uint m_forestIndex;
}

/// <summary>
/// A tag component to identify trees needing cull
/// </summary>
public struct DeadTreeTagComponent : IComponentData { }

/// <summary>
/// A tag component to identify trees who are mature and can propogate
/// </summary>
public struct MatureTreeTagComponent : IComponentData { }