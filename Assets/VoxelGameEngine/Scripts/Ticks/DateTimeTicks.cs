using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine.Ticks
{
    public struct DateTimeTicks : IComponentData
    {
        public bool Active;
        public long Value;
    }
}
