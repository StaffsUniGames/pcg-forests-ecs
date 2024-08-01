using UnityEngine;
using Unity.Entities;

public struct DataCollectionBuffer : IBufferElementData
{
	/// <summary>
	/// The number of grid subdivisions for this frame
	/// </summary>
	public int gridSubdivisions;

	/// <summary>
	/// The latency for this frame
	/// </summary>
	public float deltaTime;

	/// <summary>
	/// The number of trees in this frame
	/// </summary>
	public int treeCount;
}
