using Unity.Entities;

namespace VoxelGameEngine.World
{
    public struct WorldComponent : IComponentData
    {
        public int ChunkRange;
        public int ChunkSize;
        public int ChunkHeight;
    }
}
