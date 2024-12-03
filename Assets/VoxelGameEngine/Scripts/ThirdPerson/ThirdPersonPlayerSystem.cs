using Unity.Burst;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using VoxelGameEngine.Ticks;

namespace VoxelGameEngine.ThirdPerson
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class ThirdPersonPlayerInputsSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<TicksSystem.TickComponent>();
            RequireForUpdate(SystemAPI.QueryBuilder().WithAll<ThirdPersonPlayerComponent, ThirdPersonPlayerInputs>().Build());
        }

        protected override void OnUpdate()
        {
            uint tick = SystemAPI.GetSingleton<TicksSystem.TickComponent>().Value;

            foreach (var (playerInputs, player) in SystemAPI.Query<RefRW<ThirdPersonPlayerInputs>, ThirdPersonPlayerComponent>())
            {
                playerInputs.ValueRW.MoveInput = new float2
                {
                    x = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f),
                    y = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f),
                };

                playerInputs.ValueRW.CameraLookInput = new float2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                playerInputs.ValueRW.CameraZoomInput = -Input.mouseScrollDelta.y;

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    playerInputs.ValueRW.JumpPressed.Set(tick);
                }
            }
        }
    }

    /// <summary>
    /// Apply inputs that need to be read at a variable rate
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct ThirdPersonPlayerVariableStepControlSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<ThirdPersonPlayerComponent, ThirdPersonPlayerInputs>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (playerInputs, player) in SystemAPI.Query<ThirdPersonPlayerInputs, ThirdPersonPlayerComponent>().WithAll<Simulate>())
            {
                if (SystemAPI.HasComponent<OrbitCameraControlComponent>(player.ControlledCamera))
                {
                    OrbitCameraControlComponent cameraControl = SystemAPI.GetComponent<OrbitCameraControlComponent>(player.ControlledCamera);

                    cameraControl.FollowedCharacterEntity = player.ControlledCharacter;
                    cameraControl.LookDegreesDelta = playerInputs.CameraLookInput;
                    cameraControl.ZoomDelta = playerInputs.CameraZoomInput;

                    SystemAPI.SetComponent(player.ControlledCamera, cameraControl);
                }
            }
        }
    }

    /// <summary>
    /// Apply inputs that need to be read at a fixed rate.
    /// It is necessary to handle this as part of the fixed step group, in case your framerate is lower than the fixed step rate.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct ThirdPersonPlayerFixedStepControlSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TicksSystem.TickComponent>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<ThirdPersonPlayerComponent, ThirdPersonPlayerInputs>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            uint tick = SystemAPI.GetSingleton<TicksSystem.TickComponent>().Value;

            foreach (var (playerInputs, player) in SystemAPI.Query<ThirdPersonPlayerInputs, ThirdPersonPlayerComponent>().WithAll<Simulate>())
            {
                if (SystemAPI.HasComponent<ThirdPersonCharacterControlComponent>(player.ControlledCharacter))
                {
                    ThirdPersonCharacterControlComponent characterControl = SystemAPI.GetComponent<ThirdPersonCharacterControlComponent>(player.ControlledCharacter);

                    float3 characterUp = MathUtilities.GetUpFromRotation(SystemAPI.GetComponent<LocalTransform>(player.ControlledCharacter).Rotation);

                    // Get camera rotation, since our movement is relative to it.
                    quaternion cameraRotation = quaternion.identity;
                    if (SystemAPI.HasComponent<OrbitCameraComponent>(player.ControlledCamera))
                    {
                        // Camera rotation is calculated rather than gotten from transform, because this allows us to 
                        // reduce the size of the camera ghost state in a netcode prediction context.
                        // If not using netcode prediction, we could simply get rotation from transform here instead.
                        OrbitCameraComponent orbitCamera = SystemAPI.GetComponent<OrbitCameraComponent>(player.ControlledCamera);
                        cameraRotation = OrbitCameraUtilities.CalculateCameraRotation(characterUp, orbitCamera.PlanarForward, orbitCamera.PitchAngle);
                    }
                    float3 cameraForwardOnUpPlane = math.normalizesafe(MathUtilities.ProjectOnPlane(MathUtilities.GetForwardFromRotation(cameraRotation), characterUp));
                    float3 cameraRight = MathUtilities.GetRightFromRotation(cameraRotation);

                    // Move
                    characterControl.MoveVector = (playerInputs.MoveInput.y * cameraForwardOnUpPlane) + (playerInputs.MoveInput.x * cameraRight);
                    characterControl.MoveVector = MathUtilities.ClampToMaxLength(characterControl.MoveVector, 1f);

                    // Jump
                    characterControl.Jump = playerInputs.JumpPressed.IsSet(tick);

                    SystemAPI.SetComponent(player.ControlledCharacter, characterControl);
                }
            }
        }
    }
}