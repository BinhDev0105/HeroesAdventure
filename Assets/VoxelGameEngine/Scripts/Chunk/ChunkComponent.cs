using Unity.Entities;

namespace VoxelGameEngine.Chunk
{
    public struct ChunkComponent : IComponentData
    {
        public Entity ChunkPrefab;
    }
}
