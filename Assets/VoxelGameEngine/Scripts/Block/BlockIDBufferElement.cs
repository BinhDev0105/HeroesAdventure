using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine
{
    public struct BlockIDBufferElement : IBufferElementData
    {
        public uint Id;
    }
}
