using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Debug = UnityEngine.Debug;
using Unity.Collections;
using Unity.Transforms;
using static VoxelGameEngine.Chunk.ChunkSystem;
using VoxelGameEngine.World;
using VoxelGameEngine.Ticks;
using System;
using Unity.Burst;
using VoxelGameEngine.Player;

namespace VoxelGameEngine.Chunk
{
    [BurstCompile]
    public partial struct ChunkSystem : ISystem
    {
        private EntityManager entityManager;
        private Random random;
        public struct LastPositionComponent : IComponentData
        {
            public int3 Value;
        }

        public struct ChunkArrayComponent : IComponentData
        {
            public NativeArray<Entity> Values;
        }

        [BurstCompile]
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ChunkComponent>();
            entityManager = state.EntityManager;
        }

        [BurstCompile]
        void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<LastPositionComponent>())
            {
                Entity lastPositionEntity = entityManager.CreateEntity();
                entityManager.SetName(lastPositionEntity,$"LastPosition");
                entityManager.AddComponentData(lastPositionEntity, new LastPositionComponent());
            }

            if (!SystemAPI.HasSingleton<ChunkArrayComponent>())
            {
                Entity chunkArrayEntity = entityManager.CreateEntity();
                entityManager.SetName(chunkArrayEntity, $"ChunkArray");
                entityManager.AddComponentData(chunkArrayEntity, new ChunkArrayComponent());
            }

            ref LastPositionComponent lastPosition = ref SystemAPI.GetSingletonRW<LastPositionComponent>().ValueRW;
            ref ChunkArrayComponent chunkArray = ref SystemAPI.GetSingletonRW<ChunkArrayComponent>().ValueRW;
            ref WorldComponent world = ref SystemAPI.GetSingletonRW<WorldComponent>().ValueRW;
            ref ChunkComponent chunk = ref SystemAPI.GetSingletonRW<ChunkComponent>().ValueRW;
            ref DateTimeTicksComponent dateTimeTicks = ref SystemAPI.GetSingletonRW<DateTimeTicksComponent>().ValueRW;

            random.InitState((uint)dateTimeTicks.Value);
            dateTimeTicks.Active = false;

            lastPosition.Value = random.NextInt3(chunk.MinimumPosition, chunk.MaximumPosition) * world.ChunkSize;

            NativeArray<int3> positionArray = WorldHelper.GetChunkPositionAroundOriginPosition(world, lastPosition.Value);

            chunkArray.Values = new NativeArray<Entity>(positionArray.Length, Allocator.Temp);

            entityManager.Instantiate(chunk.ChunkPrefab, chunkArray.Values);

            Entity chunkParent = entityManager.CreateEntity();
            entityManager.SetName(chunkParent, $"ChunkParent");
            entityManager.AddComponentData(chunkParent, LocalTransform.FromPosition(float3.zero));
            entityManager.AddComponentData(chunkParent, new LocalToWorld());

            for (int index = 0; index < chunkArray.Values.Length; index++)
            {
                entityManager.AddComponentData(chunkArray.Values[index], new Parent { Value = chunkParent});
                entityManager.AddComponentData(chunkArray.Values[index], LocalTransform.FromPosition(positionArray[index]));
            }

            chunkArray.Values.Dispose();
            positionArray.Dispose();

            state.Enabled = false;
        }
    }

    [UpdateAfter(typeof(PlayerSystem))]
    [BurstCompile]
    public partial struct ChunkHandleSystem : ISystem
    {
        private EntityManager entityManager;
        [BurstCompile]
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ChunkSystem.LastPositionComponent>();
            state.RequireForUpdate<PlayerTagComponent>();
            entityManager = state.EntityManager;
        }
        [BurstCompile]
        void OnUpdate(ref SystemState state)
        {
            ref WorldComponent world = ref SystemAPI.GetSingletonRW<WorldComponent>().ValueRW;

            Entity characterEntity = SystemAPI.GetSingletonEntity<PlayerTagComponent>();

            LocalTransform characterTransform = entityManager.GetComponentData<LocalTransform>(characterEntity);

            int3 chunkPosition = WorldHelper.GetChunkPositionFromCoordinate(world, (int3)characterTransform.Position);
            Debug.Log($"{chunkPosition}");
        }
    }
}
