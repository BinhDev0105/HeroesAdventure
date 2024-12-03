using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.CharacterController;

namespace VoxelGameEngine.ThirdPerson
{
    [Serializable]
    public struct ThirdPersonCharacterComponent : IComponentData
    {
        public float RotationSharpness;
        public float GroundMaxSpeed;
        public float GroundedMovementSharpness;
        public float AirAcceleration;
        public float AirMaxSpeed;
        public float AirDrag;
        public float JumpSpeed;
        public float3 Gravity;
        public bool PreventAirAccelerationAgainstUngroundedHits;
        public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling;
    }

    [Serializable]
    public struct ThirdPersonCharacterControlComponent : IComponentData
    {
        public float3 MoveVector;
        public bool Jump;
    }

}
