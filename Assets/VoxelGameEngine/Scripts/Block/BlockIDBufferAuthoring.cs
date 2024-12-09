using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine
{
    public class BlockIDBufferAuthoring : MonoBehaviour
    {
        class Baker : Baker<BlockIDBufferAuthoring>
        {
            public override void Bake(BlockIDBufferAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddBuffer<BlockIDBufferElement>(entity);
            }
        }
    }
}
