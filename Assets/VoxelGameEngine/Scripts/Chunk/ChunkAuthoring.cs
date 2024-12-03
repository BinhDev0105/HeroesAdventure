using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelGameEngine.Chunk
{
    public class ChunkAuthoring : MonoBehaviour
    {
        public GameObject ChunkPrefab;
        public int3 MinimumPosition = new int3(-50, 0, -50);
        public int3 MaximumPosition = new int3(50, 0, 50);

        class Baker : Baker<ChunkAuthoring>
        {
            public override void Bake(ChunkAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ChunkComponent
                {
                    ChunkPrefab = GetEntity(authoring.ChunkPrefab,TransformUsageFlags.None),
                    MinimumPosition = authoring.MinimumPosition,
                    MaximumPosition = authoring.MaximumPosition,
                });
            }
        }
    }
}
