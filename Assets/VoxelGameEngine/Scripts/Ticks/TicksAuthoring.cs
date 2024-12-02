using UnityEngine;
using Unity.Entities;

namespace VoxelGameEngine.Ticks
{
    public class TicksAuthoring : MonoBehaviour
    {
        class Baker : Baker<TicksAuthoring>
        {
            public override void Bake(TicksAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new TicksComponent {});
                AddComponent(entity, new DateTimeTicksComponent { Active = true});
            }
        }
    }
}
