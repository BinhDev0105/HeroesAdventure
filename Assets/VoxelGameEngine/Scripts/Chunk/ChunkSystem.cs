using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Debug = UnityEngine.Debug;
using Unity.Collections;
using Unity.Transforms;
using static VoxelGameEngine.Chunk.ChunkSystem;
using VoxelGameEngine.World;
using VoxelGameEngine.Ticks;
using Unity.Burst;
using VoxelGameEngine.Player;
using UnityEngine.Jobs;
using Unity.Jobs;
using UnityEngine.UIElements;

namespace VoxelGameEngine.Chunk
{
    [BurstCompile]
    public partial struct ChunkSystem : ISystem
    {
        private EntityManager entityManager;
        private Random random;
        private Entity chunkParentEntity;
        private EntityCommandBuffer ecb;

        public struct LastCenterPositionComponent : IComponentData
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
            entityManager.SetName(lastPositionEntity, $"LastCenterPosition");
            entityManager.AddComponentData(lastPositionEntity, new LastCenterPositionComponent());

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

            ref LastCenterPositionComponent lastPosition = ref SystemAPI.GetSingletonRW<LastCenterPositionComponent>().ValueRW;
            ref WorldComponent world = ref SystemAPI.GetSingletonRW<WorldComponent>().ValueRW;
            ref ChunkComponent chunk = ref SystemAPI.GetSingletonRW<ChunkComponent>().ValueRW;
            ref DateTimeTicksComponent dateTimeTicks = ref SystemAPI.GetSingletonRW<DateTimeTicksComponent>().ValueRW;

            random.InitState((uint)dateTimeTicks.Value);
            dateTimeTicks.Active = false;

            lastPosition.Value = random.NextInt3(chunk.MinimumPosition, chunk.MaximumPosition) * world.ChunkSize;

            WorldHelper.ChunkData chunkData = WorldHelper.SetupChunkData(world, lastPosition.Value);

            NativeArray<int3> positionArray = CollectionHelper.CreateNativeArray<int3>(chunkData.Length, state.WorldUpdateAllocator);

            var chunkPositionJob = new ChunkPositionParallelJob
            {
                PositionArray = positionArray,
                StartX =chunkData.StartX,
                EndX = chunkData.EndX,
                StartZ = chunkData.StartZ,
                EndZ = chunkData.EndZ,
                World = world,
            };
            var chunkPositionHandle = chunkPositionJob.Schedule(chunkData.Length, 64);
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
            [WriteOnly]
            public NativeArray<int3> PositionArray;
            [ReadOnly]
            public WorldComponent World;
            [ReadOnly]
            public int StartX;
            [ReadOnly]
            public int EndX;
            [ReadOnly]
            public int StartZ;
            [ReadOnly]
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
            [ReadOnly]
            public NativeArray<Entity> chunkArray;
            [ReadOnly]
            public NativeArray<int3> positionArray;
            [ReadOnly]
            public Entity Parent;

            [BurstCompile]
            public void Execute(int index)
            {
                Ecb.AddComponent(index, chunkArray[index], new ChunkTag { });
                Ecb.AddComponent(index, chunkArray[index], new Parent { Value = Parent });
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
        [BurstCompile]
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LastCenterPositionComponent>();
            state.RequireForUpdate<PlayerTagComponent>();
            entityManager = state.EntityManager;
        }
        [BurstCompile]
        void OnUpdate(ref SystemState state)
        {
            ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            Entity characterEntity = SystemAPI.GetSingletonEntity<PlayerTagComponent>();

            LocalTransform characterTransform = entityManager.GetComponentData<LocalTransform>(characterEntity);
            ref WorldComponent world = ref SystemAPI.GetSingletonRW<WorldComponent>().ValueRW;
            ref LastCenterPositionComponent centerPosition = ref SystemAPI.GetSingletonRW<LastCenterPositionComponent>().ValueRW;

            bool isOnEdge = WorldHelper.IsOnEdge(world.ChunkSize, centerPosition.Value, (int3)characterTransform.Position);

            if (isOnEdge)
            {
                return;
            }

            NativeList<int3> chunkPositionList = new NativeList<int3>(state.WorldUpdateAllocator);

            state.CompleteDependency();
            var chunkPositionJob = new ChunkPositionListJob
            {
                ChunkPositionList = chunkPositionList,
            };
            var chunkPositionListHandle = chunkPositionJob.Schedule(state.Dependency);
            state.Dependency = chunkPositionListHandle;
            chunkPositionListHandle.Complete();

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

            var nearestChunkPositionJob = new NearestChunkPositionJob
            {
                NearestPosition = nearest,
                MinDistance = minDistance,
                Distance = distanceChunkPositionArray,
                ChunkPositionArray = chunkPositionList.AsArray(),
            };
            var nearestChunkPositionHandle = nearestChunkPositionJob.Schedule(distanceChunkPositionArray.Length, 64);
            nearestChunkPositionHandle.Complete();
            //Debug.Log($"Generate new Center chunk at {nearest[0]}");

            WorldHelper.ChunkData chunkData = WorldHelper.SetupChunkData(world, nearest[0]);

            NativeArray<int3> newChunkPositionArray = CollectionHelper.CreateNativeArray<int3>(chunkData.Length, state.WorldUpdateAllocator);

            var newChunkPositionJob = new ChunkSystem.ChunkPositionParallelJob
            {
                World = world,
                PositionArray = newChunkPositionArray,
                StartX = chunkData.StartX,
                EndX = chunkData.EndX,
                StartZ = chunkData.StartZ,
                EndZ = chunkData.EndZ,
            };
            var newChunkPositionHandle = newChunkPositionJob.Schedule(newChunkPositionArray.Length, 64);
            newChunkPositionHandle.Complete();

            //for (int i = 0; i < newChunkPositionArray.Length; i++)
            //{
            //    Debug.Log($"{newChunkPositionArray[i]}");
            //}
            NativeList<int3> neededChunkPositionList = new NativeList<int3>(state.WorldUpdateAllocator);
            NativeList<int3> unneededChunkPositionList = new NativeList<int3>(state.WorldUpdateAllocator);
            var neededChunkPositionJob = new NeededChunkPositionJob
            {
                OldChunkPositionArray = chunkPositionList.AsArray(),
                NewChunkPositionArray = newChunkPositionArray,
                NeededChunkPositionList = neededChunkPositionList.AsParallelWriter(),
                UnneededChunkPositionList = unneededChunkPositionList.AsParallelWriter()
            };
            var neededChunkPositionHandle = neededChunkPositionJob.Schedule(newChunkPositionArray.Length, 64);
            neededChunkPositionHandle.Complete();

            foreach (var item in neededChunkPositionList)
            {
                Debug.Log($"Need {item}");
            }

            foreach (var item in unneededChunkPositionList)
            {
                Debug.Log($"Unneed {item}");
            }

            unneededChunkPositionList.Dispose();
            neededChunkPositionList.Dispose();
            newChunkPositionArray.Dispose();
            minDistance.Dispose();
            nearest.Dispose();
            distanceChunkPositionArray.Dispose();
            chunkPositionList.Dispose();
        }

        [BurstCompile]
        [WithAll(typeof(ChunkTag))]
        private partial struct ChunkPositionListJob : IJobEntity
        {
            [WriteOnly]
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
            [ReadOnly]
            public NativeArray<int3> ChunkPositionArray;
            [ReadOnly]
            public float3 CharacterPosition;
            [WriteOnly]
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
            [WriteOnly]
            public NativeArray<int3> NearestPosition;
            public NativeArray<float> MinDistance;
            [ReadOnly]
            public NativeArray<float> Distance;
            [ReadOnly]
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
        private struct NeededChunkPositionJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<int3> OldChunkPositionArray;
            [ReadOnly]
            public NativeArray<int3> NewChunkPositionArray;
            [WriteOnly]
            public NativeList<int3>.ParallelWriter NeededChunkPositionList;
            [WriteOnly]
            public NativeList<int3>.ParallelWriter UnneededChunkPositionList;
            [BurstCompile]
            public void Execute(int index)
            {
                if (!OldChunkPositionArray.Contains(NewChunkPositionArray[index]))
                {
                    NeededChunkPositionList.AddNoResize(NewChunkPositionArray[index]);
                }
                if (!NewChunkPositionArray.Contains(OldChunkPositionArray[index]))
                {
                    UnneededChunkPositionList.AddNoResize(OldChunkPositionArray[index]);
                }
            }
        }

    }
}