using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine.Chunk
{
    public class ChunkAuthoring : MonoBehaviour
    {
        public GameObject ChunkPrefab;

        class Baker : Baker<ChunkAuthoring>
        {
            public override void Bake(ChunkAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ChunkComponent
                {
                    ChunkPrefab = GetEntity(authoring.ChunkPrefab,TransformUsageFlags.None),
                });
                AddComponent(entity, new ChunkListComponent { });
            }
        }
    }
}
