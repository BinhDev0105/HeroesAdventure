using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelGameEngine.Chunk
{
    public struct ChunkComponent : IComponentData
    {
        public Entity ChunkPrefab;
        public int3 MinimumPosition;
        public int3 MaximumPosition;
    }
}
