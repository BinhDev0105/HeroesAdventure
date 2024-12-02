using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine.Ticks
{
    public struct DateTimeTicksComponent : IComponentData
    {
        public bool Active;
        public long Value;
    }
}
