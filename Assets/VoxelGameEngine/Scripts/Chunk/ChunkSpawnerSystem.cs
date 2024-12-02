using Unity.Entities;
using Unity.Mathematics;
using VoxelGameEngine.World;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Debug = UnityEngine.Debug;
using Unity.Collections;
using Unity.Transforms;
using VoxelGameEngine.Ticks;

namespace VoxelGameEngine.Chunk
{
    public partial struct ChunkSpawnerSystem : ISystem
    {
        private int3 minPosition;
        private int3 maxPosition;
        private Random random;
        private EntityCommandBuffer ecb;
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ChunkComponent>();
            minPosition = new int3(-50, 0, -50);
            maxPosition = new int3(50, 0, 50);
        }


        void OnUpdate(ref SystemState state)
        {
            ref WorldComponent worldComponent = ref SystemAPI.GetSingletonRW<WorldComponent>().ValueRW;
            ref ChunkComponent chunkComponent = ref SystemAPI.GetSingletonRW<ChunkComponent>().ValueRW;
            ref DateTimeTicksComponent ticksComponent = ref SystemAPI.GetSingletonRW<DateTimeTicksComponent>().ValueRW;

            ecb = SystemAPI.GetSingletonRW<EndSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            random.InitState((uint)ticksComponent.Value);

            ticksComponent.Active = false;

            float3 randomPosition = random.NextInt3(minPosition, maxPosition) * worldComponent.ChunkSize;

            NativeArray<float3> chunkPositionArray = WorldHelper.GetChunkPositionAroundOriginPosition(worldComponent, randomPosition);

            NativeArray<Entity> chunkEntityArray = new NativeArray<Entity>(chunkPositionArray.Length, Allocator.Temp);

            ecb.Instantiate(chunkComponent.ChunkPrefab, chunkEntityArray);

            for (int i = 0; i < chunkEntityArray.Length; i++)
            {
                ecb.SetComponent(chunkEntityArray[i], LocalTransform.FromPosition(chunkPositionArray[i]));
            }

            chunkEntityArray.Dispose();
            chunkPositionArray.Dispose();
            state.Enabled = false;
        }
    }
}
