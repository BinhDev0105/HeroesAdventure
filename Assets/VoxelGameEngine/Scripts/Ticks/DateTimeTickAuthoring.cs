using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine.Ticks
{
    public class DateTimeTickAuthoring : MonoBehaviour
    {
        class Baker : Baker<DateTimeTickAuthoring>
        {
            public override void Bake(DateTimeTickAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new DateTimeTicks { Active = true});
            }
        }
    }
}
