using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelGameEngine.WorldECS
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
                AddComponent(entity, new World
                {
                    ChunkRange = authoring.ChunkRange,
                    ChunkSize = authoring.ChunkSize,
                    ChunkHeight = authoring.ChunkHeight,
                });
            }
        }
    }
}
