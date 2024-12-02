using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine.Camera
{
    public struct CameraTargetComponent : IComponentData
    {
        public Entity TargetEntity;
    }
}
