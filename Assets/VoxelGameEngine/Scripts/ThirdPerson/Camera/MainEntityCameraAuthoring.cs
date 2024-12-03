using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine.ThirdPerson
{
    [DisallowMultipleComponent]
    public class MainEntityCameraAuthoring : MonoBehaviour
    {
        class Baker : Baker<MainEntityCameraAuthoring>
        {
            public override void Bake(MainEntityCameraAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MainEntityCameraComponent { });
            }
        }
    }
}
