using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine.World
{
    public class WorldAuthoring : MonoBehaviour
    {
        public int ChunkRange;
        public int ChunkSize;
        public int ChunkHeight;

        class Baker : Baker<WorldAuthoring>
        {
            public override void Bake(WorldAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new WorldComponent
                {
                    ChunkRange = authoring.ChunkRange,
                    ChunkSize = authoring.ChunkSize,
                    ChunkHeight = authoring.ChunkHeight
                });
            }
        }
    }
}
