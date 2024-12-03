using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelGameEngine
{
    [Serializable]
    public struct ThirdPersonPlayer : IComponentData
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
