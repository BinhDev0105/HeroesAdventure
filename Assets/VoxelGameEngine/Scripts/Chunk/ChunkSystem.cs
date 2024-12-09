using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Debug = UnityEngine.Debug;
using Unity.Collections;
using Unity.Transforms;
using static VoxelGameEngine.Chunk.ChunkSystem;
using World = VoxelGameEngine.WorldECS.World;
using ChunkData = VoxelGameEngine.WorldECS.WorldHelper.ChunkData;
using WorldHelper = VoxelGameEngine.WorldECS.WorldHelper;
using VoxelGameEngine.Ticks;
using Unity.Burst;
using VoxelGameEngine.Player;
using UnityEngine.Jobs;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace VoxelGameEngine.Chunk
{
    [BurstCompile]
    public partial struct ChunkSystem : ISystem
    {
        private EntityManager entityManager;
        private Random random;
        private Entity chunkParentEntity;
        private EntityCommandBuffer ecb;

        //public static readonly SharedStatic<int> NumberOfChunk = SharedStatic<int>.GetOrCreate<ChunkSystem>();

        public struct LastCenterPosition : IComponentData
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
            state.RequireForUpdate<Chunk>();
            entityManager = state.EntityManager;

            Entity lastPositionEntity = entityManager.CreateEntity();
            entityManager.SetName(lastPositionEntity, $"LastCenterPosition");
            entityManager.AddComponentData(lastPositionEntity, new LastCenterPosition());

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

            ref LastCenterPosition lastPosition = ref SystemAPI.GetSingletonRW<LastCenterPosition>().ValueRW;
            ref World world = ref SystemAPI.GetSingletonRW<World>().ValueRW;
            ref Chunk chunk = ref SystemAPI.GetSingletonRW<Chunk>().ValueRW;
            ref DateTimeTicks dateTimeTicks = ref SystemAPI.GetSingletonRW<DateTimeTicks>().ValueRW;
            
            //NumberOfChunk.Data = world.NumberOfChunk;
            //Debug.Log($"{NumberOfChunk.Data}");

            random.InitState((uint)dateTimeTicks.Value);
            dateTimeTicks.Active = false;

            lastPosition.Value = random.NextInt3(chunk.MinimumPosition, chunk.MaximumPosition) * world.ChunkSize;

            ChunkData chunkData = WorldHelper.SetupChunkData(world, lastPosition.Value);

            NativeArray<int3> positionArray = CollectionHelper.CreateNativeArray<int3>(chunkData.Length, Allocator.TempJob);

            var chunkPositionJob = new ChunkPositionJob
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

            NativeArray<Entity> chunkArray = CollectionHelper.CreateNativeArray<Entity>(positionArray.Length, Allocator.TempJob);

            entityManager.Instantiate(chunk.ChunkPrefab, chunkArray);

            var chunkParallelJob = new ChunkJob
            {
                Ecb = ecb.AsParallelWriter(),
                Parent = chunkParentEntity,
                ChunkArray = chunkArray,
                PositionArray = positionArray,
            };
            var handle = chunkParallelJob.Schedule(chunkArray.Length, 64);
            handle.Complete();

            
            positionArray.Dispose();
            chunkArray.Dispose();
            state.Enabled = false;
        }
        [BurstCompile]
        public struct ChunkPositionJob : IJobParallelFor
        {
            [WriteOnly]
            public NativeArray<int3> PositionArray;
            [ReadOnly]
            public World World;
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
        private struct ChunkJob : IJobParallelFor
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly]
            public NativeArray<Entity> ChunkArray;
            [ReadOnly]
            public NativeArray<int3> PositionArray;
            [ReadOnly]
            public Entity Parent;

            [BurstCompile]
            public void Execute(int index)
            {
                Ecb.AddComponent(index, ChunkArray[index], new ChunkTag { });
                Ecb.AddComponent(index, ChunkArray[index], new Parent { Value = Parent });
                Ecb.AddComponent(index, ChunkArray[index], LocalTransform.FromPosition(PositionArray[index]));
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
            state.RequireForUpdate<LastCenterPosition>();
            state.RequireForUpdate<PlayerTag>();

            entityManager = state.EntityManager;
        }

        [BurstCompile]
        void OnUpdate(ref SystemState state)
        {
            
            ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            Entity characterEntity = SystemAPI.GetSingletonEntity<PlayerTag>();

            LocalTransform characterTransform = entityManager.GetComponentData<LocalTransform>(characterEntity);
            ref World world = ref SystemAPI.GetSingletonRW<World>().ValueRW;
            ref LastCenterPosition centerPosition = ref SystemAPI.GetSingletonRW<LastCenterPosition>().ValueRW;

            bool isOnEdge = WorldHelper.IsOnEdge(world.ChunkSize, centerPosition.Value, (int3)characterTransform.Position);

            if (isOnEdge)
            {
                return;
            }

            NativeList<int3> chunkPositionList = new NativeList<int3>(Allocator.TempJob);
            state.CompleteDependency();
            var chunkPositionJob = new ChunkPositionListJob
            {
                ChunkPositionList = chunkPositionList,
            };
            var chunkPositionHandle = chunkPositionJob.Schedule(state.Dependency);
            state.Dependency = chunkPositionHandle;
            state.Dependency.Complete();

            int chunkLength = chunkPositionList.Length;

            NativeArray<float> distanceArray = CollectionHelper.CreateNativeArray<float>(chunkLength, Allocator.TempJob);
            var distanceChunkPositionJob = new DistanceChunkPositionJob
            {
                ChunkPositionArray = chunkPositionList.AsArray(),
                CharacterPosition = characterTransform.Position,
                Distance = distanceArray
            };
            var distanceHandle = distanceChunkPositionJob.Schedule(chunkLength, 64, state.Dependency);
            state.Dependency = distanceHandle;
            state.Dependency.Complete();

            NativeArray<int> nearestChunkIndex = CollectionHelper.CreateNativeArray<int>(1, Allocator.TempJob); ;
            var nearestChunkIndexJob = new NearestChunkIndexJob
            {
                Distances = distanceArray,
                NearestChunkIndex = nearestChunkIndex,

            };
            var nearestChunkIndexHandle = nearestChunkIndexJob.Schedule(state.Dependency);
            state.Dependency = nearestChunkIndexHandle;
            state.Dependency.Complete();

            WorldHelper.ChunkData chunkData = WorldHelper.SetupChunkData(world, chunkPositionList.ElementAt(nearestChunkIndex[0]));

            NativeArray<int3> newChunkPositionArray = CollectionHelper.CreateNativeArray<int3>(chunkLength, Allocator.TempJob);
            var newChunkPositionJob = new ChunkPositionJob
            {
                World = world,
                PositionArray = newChunkPositionArray,
                StartX = chunkData.StartX,
                EndX = chunkData.EndX,
                StartZ = chunkData.StartZ,
                EndZ = chunkData.EndZ,
            };
            var newChunkPositionHandle = newChunkPositionJob.Schedule(chunkLength, 64, state.Dependency);
            state.Dependency = newChunkPositionHandle;
            state.Dependency.Complete();

            NativeList<int3> neededChunkPositionList = new NativeList<int3>(Allocator.TempJob);
            neededChunkPositionList.Capacity = chunkLength;
            NativeList<int3> unneededChunkPositionList = new NativeList<int3>(Allocator.TempJob);
            unneededChunkPositionList.Capacity = chunkLength;

            var neededChunkPositionJob = new NeededChunkPositionJob
            {
                OldChunkPositionArray = chunkPositionList.AsArray(),
                NewChunkPositionArray = newChunkPositionArray,
                NeededChunkPositionList = neededChunkPositionList.AsParallelWriter(),
                UnneededChunkPositionList = unneededChunkPositionList.AsParallelWriter()
            };
            var neededChunkPositionHandle = neededChunkPositionJob.Schedule(chunkLength, 64, state.Dependency);
            state.Dependency = neededChunkPositionHandle;
            state.Dependency.Complete();

            var chunkTransformJob = new ChunkTransformJob
            {
                NeededChunkPositionArray = neededChunkPositionList.AsArray(),
                UnneededChunkPositionArray = unneededChunkPositionList.AsArray(),
            };
            var chunkTransformHandle = chunkTransformJob.Schedule(state.Dependency);
            state.Dependency = chunkTransformHandle;
            state.Dependency.Complete();

            centerPosition.Value = chunkPositionList.ElementAt(nearestChunkIndex[0]);
            
            unneededChunkPositionList.Dispose();
            neededChunkPositionList.Dispose();
            newChunkPositionArray.Dispose();
            nearestChunkIndex.Dispose();
            distanceArray.Dispose();
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
        private struct NearestChunkIndexJob : IJob
        {
            [ReadOnly]
            public NativeArray<float> Distances;
            [WriteOnly]
            public NativeArray<int> NearestChunkIndex;
            [BurstCompile]
            public void Execute()
            {
                float minDistance = float.MaxValue;
                int minIndex = -1;
                for (int i = 0; i < Distances.Length; i++)
                {
                    if (Distances[i] < minDistance)
                    {
                        minDistance = Distances[i];
                        minIndex = i;
                    }
                }
                NearestChunkIndex[0] = minIndex;
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

        [BurstCompile]
        private partial struct ChunkTransformJob : IJobEntity
        {
            [ReadOnly]
            public NativeArray<int3> NeededChunkPositionArray;
            [ReadOnly]
            public NativeArray<int3> UnneededChunkPositionArray;
            [BurstCompile]
            public void Execute([ChunkIndexInQuery] int chunkIndex, ref LocalTransform transform)
            {
                NativeArray<float3> Position = CollectionHelper.CreateNativeArray<float3>(1, Allocator.Temp);
                Position[0] = transform.Position;
                for (int i = 0; i < UnneededChunkPositionArray.Length; i++)
                {
                    if (Position[0].x == UnneededChunkPositionArray[i].x
                        && Position[0].y == UnneededChunkPositionArray[i].y
                        && Position[0].z == UnneededChunkPositionArray[i].z)
                    {
                        Position[0] = NeededChunkPositionArray[i];
                    }
                }
                transform.Position = Position[0];
                Position.Dispose();
            }
        }

    }
}