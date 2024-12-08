using Unity.Entities;

namespace VoxelGameEngine.WorldECS
{
    public struct World : IComponentData
    {
        public int ChunkRange;
        public int ChunkSize;
        public int ChunkHeight;
        public int NumberOfChunk;
    }
}
