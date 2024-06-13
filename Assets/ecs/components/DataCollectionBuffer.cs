using UnityEngine;
using Unity.Entities;

public struct DataCollectionBuffer : IBufferElementData
{
	public int gridSubdivisions;
	public float deltaTime;
	public int treeCount;
}
