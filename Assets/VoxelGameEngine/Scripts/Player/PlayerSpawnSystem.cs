using Unity.Entities;
using UnityEngine;
using VoxelGameEngine.Chunk;
using static VoxelGameEngine.Chunk.ChunkSpawnerSystem;
using VoxelGameEngine;
using Unity.Mathematics;
using Unity.Transforms;

namespace VoxelGameEngine.Player
{
    [UpdateAfter(typeof(ChunkSpawnerSystem))]
    public partial struct PlayerSpawnSystem : ISystem
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ChunkSpawnerSystem.RandomPosition>();
            state.RequireForUpdate<PlayerComponent>();
        }

        // Update is called once per frame
        void OnUpdate(ref SystemState state)
        {
            ref RandomPosition randomPosition = ref SystemAPI.GetSingletonRW<RandomPosition>().ValueRW;
            ref PlayerComponent player = ref SystemAPI.GetSingletonRW<PlayerComponent>().ValueRW;

            EntityCommandBuffer ecb = SystemAPI.GetSingletonRW<EndSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);
            Entity characterEntity = ecb.Instantiate(player.CharacterPrefab);
            Entity playerEntity = ecb.Instantiate(player.PlayerPrefab);
            Entity cameraEntity = ecb.Instantiate(player.CameraPrefab);

            ThirdPersonPlayer thirdPlayer = SystemAPI.GetComponent<ThirdPersonPlayer>(player.PlayerPrefab);
            thirdPlayer.ControlledCharacter = characterEntity;
            thirdPlayer.ControlledCamera = cameraEntity;
            ecb.SetComponent(playerEntity, thirdPlayer);
            ecb.SetComponent(characterEntity, LocalTransform.FromPosition(randomPosition.Value));
            state.Enabled = false;
        }
    }
}
