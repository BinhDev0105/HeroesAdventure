using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine
{
    public class PlayerTagAuthoring : MonoBehaviour
    {
        class Baker : Baker<PlayerTagAuthoring>
        {
            public override void Bake(PlayerTagAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PlayerTagComponent());
            }
        }
    }
}
