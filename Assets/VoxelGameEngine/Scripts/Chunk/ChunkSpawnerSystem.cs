using System.Diagnostics;
using Unity.Entities;
using Unity.Mathematics;
using VoxelGameEngine.World;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Debug = UnityEngine.Debug;
using Unity.Collections;

namespace VoxelGameEngine.Chunk
{
    public partial struct ChunkSpawnerSystem : ISystem
    {
        private Random random;
        private EntityCommandBuffer ecb;
        private int randNum;
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ChunkComponent>();
            random = Random.CreateFromIndex(0);
            randNum = 50;
        }


        void OnUpdate(ref SystemState state)
        {
            ref WorldComponent worldComponent = ref SystemAPI.GetSingletonRW<WorldComponent>().ValueRW;
            ref ChunkComponent chunkComponent = ref SystemAPI.GetSingletonRW<ChunkComponent>().ValueRW;
            //ref ChunkListComponent chunkListComponent = ref SystemAPI.GetSingletonRW<ChunkListComponent>().ValueRW;

            float3 randPosition = random.NextInt3(-randNum, randNum) * worldComponent.ChunkSize;
            Debug.Log(randPosition);

            NativeArray<float3> count = WorldHelper.GetChunkPositionAroundOriginPosition(worldComponent, randPosition);
            //foreach (var item in count)
            //{
            //    Debug.Log(item);
            //}
            state.Enabled = false;
        }
    }
}
