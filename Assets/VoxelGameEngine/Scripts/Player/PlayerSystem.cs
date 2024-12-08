using Unity.Entities;
using UnityEngine;
using VoxelGameEngine.Chunk;
using static VoxelGameEngine.Chunk.ChunkSystem;
using VoxelGameEngine;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;

namespace VoxelGameEngine.Player
{
    [UpdateAfter(typeof(ChunkSystem))]
    [BurstCompile]
    public partial struct PlayerSystem : ISystem
    {
        private EntityManager entityManager;
        [BurstCompile]
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Player>();
            entityManager = state.EntityManager;
        }

        [BurstCompile]
        void OnUpdate(ref SystemState state)
        {
            ref LastCenterPosition lastPosition = ref SystemAPI.GetSingletonRW<LastCenterPosition>().ValueRW;
            ref Player player = ref SystemAPI.GetSingletonRW<Player>().ValueRW;

            EntityCommandBuffer ecb = SystemAPI.GetSingletonRW<EndSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);
            Entity characterEntity = ecb.Instantiate(player.CharacterPrefab);
            Entity playerEntity = ecb.Instantiate(player.PlayerPrefab);
            Entity cameraEntity = ecb.Instantiate(player.CameraPrefab);

            ThirdPersonPlayer thirdPlayer = SystemAPI.GetComponent<ThirdPersonPlayer>(player.PlayerPrefab);
            thirdPlayer.ControlledCharacter = characterEntity;
            thirdPlayer.ControlledCamera = cameraEntity;
            ecb.SetComponent(playerEntity, thirdPlayer);
            ecb.SetComponent(characterEntity, LocalTransform.FromPosition(lastPosition.Value));
            state.Enabled = false;
        }
    }
}
