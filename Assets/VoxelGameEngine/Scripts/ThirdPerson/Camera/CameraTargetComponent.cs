using System;
using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine.ThirdPerson
{
    [Serializable]
    public struct CameraTargetComponent : IComponentData
    {
        public Entity TargetEntity;
    }
}
