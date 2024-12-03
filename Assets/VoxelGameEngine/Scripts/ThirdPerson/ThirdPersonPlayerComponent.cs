using System;
using Unity.Entities;
using Unity.Mathematics;

namespace VoxelGameEngine.ThirdPerson
{
    [Serializable]
    public struct ThirdPersonPlayerComponent : IComponentData
    {
        public Entity ControlledCharacter;
        public Entity ControlledCamera;
    }
    [Serializable]
    public struct ThirdPersonPlayerInputs : IComponentData
    {
        public float2 MoveInput;
        public float2 CameraLookInput;
        public float CameraZoomInput;
        public FixedInputEvent JumpPressed;
    }
}
