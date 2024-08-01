using Unity.Entities;
using Unity.Mathematics;

public struct SpatialHasher : IComponentData
{
    /// The world size, in coordinate space
    private float2 m_worldSize;

    /// The number of col/rows the space is divided into
    public int m_gridSize;

    /// <summary>
    /// A spatial hasher
    /// </summary>
    public SpatialHasher(float2 worldSize, int gridSize)
    {
        this.m_gridSize = gridSize;
        this.m_worldSize = worldSize;
    }

    /// <summary>
    /// Checks if a position is outside the world boundaries
    /// </summary>
    private bool PositionOutsideWorldBounds(float2 position)
    {
        bool outsideX = position.x < 0 || position.x > m_worldSize.x;
        bool outsideY = position.y < 0 || position.y > m_worldSize.y;

        return outsideX || outsideY;
    }

    /// <summary>
    /// Hashes a position to a given 1D index using a uniform grid
    /// </summary>
    public int Hash(float2 position)
    {
        //Return invalid index if outside of world boundaries
        if(PositionOutsideWorldBounds(position))
            return -1;

        //find dx & dy
        float gridDeltaX = m_worldSize.x / (float)m_gridSize;
        float gridDeltaY = m_worldSize.y / (float)m_gridSize;

        //quantize to 2D index
        int dX = (int) math.floor(position.x / gridDeltaX);
        int dY = (int) math.floor(position.y / gridDeltaY);

        //hash into 1D index from 2D
        return dX + dY * m_gridSize;
    }
}