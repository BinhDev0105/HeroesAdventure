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
using UnityEngine.Jobs;
using Unity.Jobs;
using System.Threading;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using Unity.Entities.UniversalDelegates;

namespace VoxelGameEngine.Chunk
{
    [BurstCompile]
    public partial struct ChunkSystem : ISystem
    {
        private EntityManager entityManager;
        private Random random;
        private Entity chunkParentEntity;
        private EntityCommandBuffer ecb;

        public struct LastPositionComponent : IComponentData
        {
            public int3 Value;
        }

        public struct ChunkParentTag : IComponentData
        {

        }

        public struct ChunkTag : IComponentData
        {

        }

        [BurstCompile]
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ChunkComponent>();
            entityManager = state.EntityManager;

            Entity lastPositionEntity = entityManager.CreateEntity();
            entityManager.SetName(lastPositionEntity, $"LastPosition");
            entityManager.AddComponentData(lastPositionEntity, new LastPositionComponent());

            chunkParentEntity = entityManager.CreateEntity();
            entityManager.SetName(chunkParentEntity, $"ChunkParent");
            entityManager.AddComponentData(chunkParentEntity, LocalTransform.FromPosition(float3.zero));
            entityManager.AddComponentData(chunkParentEntity, new LocalToWorld());
            entityManager.AddComponentData(chunkParentEntity, new ChunkParentTag());
        }

        [BurstCompile]
        void OnUpdate(ref SystemState state)
        {
            ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            ref LastPositionComponent lastPosition = ref SystemAPI.GetSingletonRW<LastPositionComponent>().ValueRW;
            ref WorldComponent world = ref SystemAPI.GetSingletonRW<WorldComponent>().ValueRW;
            ref ChunkComponent chunk = ref SystemAPI.GetSingletonRW<ChunkComponent>().ValueRW;
            ref DateTimeTicksComponent dateTimeTicks = ref SystemAPI.GetSingletonRW<DateTimeTicksComponent>().ValueRW;

            random.InitState((uint)dateTimeTicks.Value);
            dateTimeTicks.Active = false;

            lastPosition.Value = random.NextInt3(chunk.MinimumPosition, chunk.MaximumPosition) * world.ChunkSize;

            int startX = (int)lastPosition.Value.x - world.ChunkRange * world.ChunkSize;
            int endX = (int)lastPosition.Value.x + world.ChunkRange * world.ChunkSize;
            int startZ = (int)lastPosition.Value.z - world.ChunkRange * world.ChunkSize;
            int endZ = (int)lastPosition.Value.z + world.ChunkRange * world.ChunkSize;

            int countX = (endX - startX) / world.ChunkSize + 1;
            int countZ = (endZ - startZ) / world.ChunkSize + 1;

            int length = countX * countZ;

            NativeArray<int3> positionArray = CollectionHelper.CreateNativeArray<int3>(length, state.WorldUpdateAllocator);

            var chunkPositionJob = new ChunkPositionParallelJob
            {
                PositionArray = positionArray,
                StartX = startX,
                EndX = endX,
                StartZ = startZ,
                EndZ = endZ,
                World = world,
            };
            var chunkPositionHandle = chunkPositionJob.Schedule(length, 64);
            chunkPositionHandle.Complete();

            foreach (var item in positionArray)
            {
                //Debug.Log($"{item}");
            }

            NativeArray<Entity> chunkArray = CollectionHelper.CreateNativeArray<Entity>(positionArray.Length, state.WorldUpdateAllocator);

            entityManager.Instantiate(chunk.ChunkPrefab, chunkArray);

            var chunkParallelJob = new ChunkParallelJob
            {
                Ecb = ecb.AsParallelWriter(),
                Parent = chunkParentEntity,
                chunkArray = chunkArray,
                positionArray = CollectionHelper.CreateNativeArray<int3>(positionArray, state.WorldUpdateAllocator),
            };
            var handle = chunkParallelJob.Schedule(chunkArray.Length, 64);
            handle.Complete();

            chunkArray.Dispose();
            positionArray.Dispose();

            state.Enabled = false;
        }
        [BurstCompile]
        private struct ChunkPositionParallelJob : IJobParallelFor
        {
            public NativeArray<int3> PositionArray;

            public WorldComponent World;
            public int StartX;
            public int EndX;
            public int StartZ;
            public int EndZ;
            
            [BurstCompile]
            public void Execute(int index)
            {
                int x = StartX + (index % ((EndX - StartX) / World.ChunkSize + 1)) * World.ChunkSize;
                int z = StartZ + (index / ((EndX - StartX) / World.ChunkSize + 1)) * World.ChunkSize;

                int3 position = WorldHelper.GetChunkPositionFromCoordinate(World, new int3(x, 0, z));
                PositionArray[index] = position;
            }
        }
        [BurstCompile]
        private struct ChunkParallelJob : IJobParallelFor
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public NativeArray<Entity> chunkArray;
            public NativeArray<int3> positionArray;
            public Entity Parent;

            [BurstCompile]
            public void Execute(int index)
            {
                Ecb.AddComponent(index, chunkArray[index], new ChunkTag{});
                Ecb.AddComponent(index, chunkArray[index], new Parent { Value = Parent});
                Ecb.AddComponent(index, chunkArray[index], LocalTransform.FromPosition(positionArray[index]));
            }
        }
    }


    [UpdateAfter(typeof(PlayerSystem))]
    [UpdateAfter(typeof(ChunkSystem))]
    [BurstCompile]
    public partial struct ChunkHandleSystem : ISystem
    {
        private EntityManager entityManager;
        private EntityCommandBuffer ecb;
        private bool isOnEdge;
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
            //ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            //ref WorldComponent world = ref SystemAPI.GetSingletonRW<WorldComponent>().ValueRW;

            //Entity chunkParentEntity = SystemAPI.GetSingletonEntity<ChunkParentTag>();

            //DynamicBuffer<Child> childBuffer = entityManager.GetBuffer<Child>(chunkParentEntity);
            //NativeArray<int3> childPositionArray = CollectionHelper.CreateNativeArray<int3>(childBuffer.Length, state.WorldUpdateAllocator);

            //state.CompleteDependency();
            //var childPositionJob = new childChunkPositionJob
            //{
            //    childPositionArray = childPositionArray,
            //};
            //var childHandle = childPositionJob.Schedule(state.Dependency);
            //state.Dependency = childHandle;
            //childHandle.Complete();

            //Entity characterEntity = SystemAPI.GetSingletonEntity<PlayerTagComponent>();

            //LocalTransform characterTransform = entityManager.GetComponentData<LocalTransform>(characterEntity);

            //var isOnEdgeJob = new IsOnEdgeParallelJob
            //{

            //};
            //var edgeHandle = isOnEdgeJob.Schedule(1,64);
            //Debug.Log($"Player is on edge: {isOnEdge}");

            
        }

        [BurstCompile]
        [WithAll(typeof(ChunkTag))]
        private partial struct childChunkPositionJob : IJobEntity
        {
            private int index;
            public NativeArray<int3> childPositionArray;
            [BurstCompile]
            public void Execute([ChunkIndexInQuery] int chunkIndex, ref LocalTransform transform)
            {
                childPositionArray[index] = (int3)transform.Position;
                index++;
            }
        }

        [BurstCompile]
        private struct GetChunkPositionParallelJob : IJobParallelFor
        {
            public NativeArray<int3> ChildPositionArray;
            public int ChunkSize;
            public int3 CharacterPosition;
            public bool IsOnEdge;
            [BurstCompile]
            public void Execute(int index)
            {
                int3 chunkPosition = WorldHelper.GetChunkPositionFromCoordinate(ChildPositionArray, CharacterPosition);
                IsOnEdge = WorldHelper.IsOnEdge(ChunkSize, chunkPosition, CharacterPosition);
            }
        }

        [BurstCompile]
        private struct IsOnEdgeParallelJob : IJobParallelFor
        {
            public void Execute(int index)
            {
                throw new NotImplementedException();
            }
        }
    }
}
