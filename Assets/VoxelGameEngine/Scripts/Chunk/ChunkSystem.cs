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
        public struct ChunkPositionParallelJob : IJobParallelFor
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
        private int3 nearest;
        private float minDistance;
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
            ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            ref WorldComponent world = ref SystemAPI.GetSingletonRW<WorldComponent>().ValueRW;

            NativeList<int3> chunkPositionList = new NativeList<int3>(state.WorldUpdateAllocator);

            state.CompleteDependency();
            var chunkPositionJob = new ChunkPositionListJob
            {
                ChunkPositionList = chunkPositionList,
            };
            var chunkPositionListHandle = chunkPositionJob.Schedule(state.Dependency);
            state.Dependency = chunkPositionListHandle;
            chunkPositionListHandle.Complete();

            Entity characterEntity = SystemAPI.GetSingletonEntity<PlayerTagComponent>();

            LocalTransform characterTransform = entityManager.GetComponentData<LocalTransform>(characterEntity);

            NativeArray<float> distanceChunkPositionArray = CollectionHelper.CreateNativeArray<float>(chunkPositionList.Length, state.WorldUpdateAllocator);

            var distanceChunkPositionJob = new DistanceChunkPositionJob
            {
                ChunkPositionArray = chunkPositionList.AsArray(),
                CharacterPosition = characterTransform.Position,
                Distance = distanceChunkPositionArray
            };
            var distanceChunkPositionHandle = distanceChunkPositionJob.Schedule(chunkPositionList.Length, 64);
            distanceChunkPositionHandle.Complete();

            NativeArray<int3> nearest = CollectionHelper.CreateNativeArray<int3>(1, state.WorldUpdateAllocator);
            nearest[0] = chunkPositionList[0];
            NativeArray<float> minDistance = CollectionHelper.CreateNativeArray<float>(1, state.WorldUpdateAllocator);
            minDistance[0] = distanceChunkPositionArray[0];

            //for (int i = 1; i < distanceChunkPositionArray.Length; i++)
            //{
            //    if (distanceChunkPositionArray[i] < minDistance)
            //    {
            //        minDistance = distanceChunkPositionArray[i];
            //        nearest = chunkPositionList[i];
            //    }
            //}
            
            var nearestChunkPositionJob = new NearestChunkPositionJob
            {
                NearestPosition = nearest,
                MinDistance = minDistance,
                Distance = distanceChunkPositionArray,
                ChunkPositionArray = chunkPositionList.AsArray(),
            };
            var nearestChunkPositionHandle = nearestChunkPositionJob.Schedule(distanceChunkPositionArray.Length, 64);
            nearestChunkPositionHandle.Complete();
            
            minDistance.Dispose();
            nearest.Dispose();
            distanceChunkPositionArray.Dispose();
            chunkPositionList.Dispose();
        }

        [BurstCompile]
        [WithAll(typeof(ChunkTag))]
        private partial struct ChunkPositionListJob : IJobEntity
        {
            public NativeList<int3> ChunkPositionList;
            [BurstCompile]
            public void Execute([ChunkIndexInQuery] int chunkIndex, ref LocalTransform transform)
            {
                ChunkPositionList.Add((int3)transform.Position);
            }
        }

        [BurstCompile]
        private struct DistanceChunkPositionJob : IJobParallelFor
        {
            public NativeArray<int3> ChunkPositionArray;
            public float3 CharacterPosition;
            public NativeArray<float> Distance;
            [BurstCompile]
            public void Execute(int index)
            {
                Distance[index] = math.distance(ChunkPositionArray[index], CharacterPosition);
            }
        }
        [BurstCompile]
        private struct NearestChunkPositionJob : IJobParallelFor
        {
            public NativeArray<int3> NearestPosition;
            public NativeArray<float> MinDistance;
            public NativeArray<float> Distance;
            public NativeArray<int3> ChunkPositionArray;
            [BurstCompile]
            public void Execute(int index)
            {
                if (Distance[index] < MinDistance[0])
                {
                    MinDistance[0] = Distance[index];
                    NearestPosition[0] = ChunkPositionArray[index];
                }
            }
        }

        [BurstCompile]
        private struct IsOnEdgeParallelJob : IJob
        {
            public NativeArray<int3> ChildPositionArray;
            public int ChunkSize;
            public int3 CharacterPosition;
            public bool IsOnEdge;
            [BurstCompile]
            public void Execute()
            {
                int3 chunkPosition = WorldHelper.GetChunkPositionFromCoordinate(ChildPositionArray, CharacterPosition);
                IsOnEdge = WorldHelper.IsOnEdge(ChunkSize, chunkPosition, CharacterPosition);
            }
        }
    }
}
