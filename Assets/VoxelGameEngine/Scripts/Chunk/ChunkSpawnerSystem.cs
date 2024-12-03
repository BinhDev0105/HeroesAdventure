using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Debug = UnityEngine.Debug;
using Unity.Collections;
using Unity.Transforms;
using static VoxelGameEngine.Chunk.ChunkSpawnerSystem;
using VoxelGameEngine.World;
using VoxelGameEngine.Ticks;

namespace VoxelGameEngine.Chunk
{
    public partial struct ChunkSpawnerSystem : ISystem
    {
        private int3 minPosition;
        private int3 maxPosition;
        private Random random;
        private EntityCommandBuffer ecb;

        public struct RandomPosition : IComponentData
        {
            public float3 Value;
        }

        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ChunkComponent>();
            minPosition = new int3(-50, 0, -50);
            maxPosition = new int3(50, 0, 50);
            if (!SystemAPI.HasSingleton<RandomPosition>())
            {
                Entity entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, new RandomPosition());
            }
        }


        void OnUpdate(ref SystemState state)
        {
            ref WorldComponent worldComponent = ref SystemAPI.GetSingletonRW<WorldComponent>().ValueRW;
            ref ChunkComponent chunkComponent = ref SystemAPI.GetSingletonRW<ChunkComponent>().ValueRW;
            ref RandomPosition randomPosition = ref SystemAPI.GetSingletonRW<RandomPosition>().ValueRW;
            ref DateTimeTicksComponent ticksComponent = ref SystemAPI.GetSingletonRW<DateTimeTicksComponent>().ValueRW;

            ecb = SystemAPI.GetSingletonRW<EndSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            random.InitState((uint)ticksComponent.Value);

            ticksComponent.Active = false;

            randomPosition.Value = random.NextInt3(minPosition, maxPosition) * worldComponent.ChunkSize;

            NativeArray<float3> chunkPositionArray = WorldHelper.GetChunkPositionAroundOriginPosition(worldComponent, randomPosition.Value);

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
