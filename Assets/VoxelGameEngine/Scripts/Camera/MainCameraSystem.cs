using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace VoxelGameEngine.Camera
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class MainCameraSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (MainGameObjectCamera.Instance != null && SystemAPI.HasSingleton<MainEntityCameraComponent>())
            {
                Entity mainCameraEntity = SystemAPI.GetSingletonEntity<MainEntityCameraComponent>();
                LocalToWorld target = SystemAPI.GetComponent<LocalToWorld>(mainCameraEntity);
                MainGameObjectCamera.Instance.transform.SetPositionAndRotation(target.Position, target.Rotation);
            }
        }
    }
}
