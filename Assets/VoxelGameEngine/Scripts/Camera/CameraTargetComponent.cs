using System;
using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine.Camera
{
    [Serializable]
    public struct CameraTargetComponent : IComponentData
    {
        public Entity TargetEntity;
    }
}
