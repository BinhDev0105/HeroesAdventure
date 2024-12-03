using System;
using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine
{
    [Serializable]
    public struct CameraTarget : IComponentData
    {
        public Entity TargetEntity;
    }

}
