using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine.ThirdPerson
{
    [DisallowMultipleComponent]
    public class ThirdPersonPlayerAuthoring : MonoBehaviour
    {
        public GameObject ControlledCharacter;
        public GameObject ControlledCamera;

        public class Baker : Baker<ThirdPersonPlayerAuthoring>
        {
            public override void Bake(ThirdPersonPlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ThirdPersonPlayerComponent
                {
                    ControlledCharacter = GetEntity(authoring.ControlledCharacter, TransformUsageFlags.Dynamic),
                    ControlledCamera = GetEntity(authoring.ControlledCamera, TransformUsageFlags.Dynamic),
                });
                AddComponent<ThirdPersonPlayerInputs>(entity);
            }
        }
    }
}
