using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelGameEngine.World
{
    public struct WorldHelper
    {
        public static float3 GetChunkPositionFromCoordinate(WorldComponent world, float3 position)
        {
            return new float3(
                Mathf.FloorToInt(position.x / world.ChunkSize) * world.ChunkSize,
                Mathf.FloorToInt(position.y / world.ChunkHeight) * world.ChunkHeight,
                Mathf.FloorToInt(position.z / world.ChunkSize) * world.ChunkSize
            );
        }
        public static NativeArray<float3> GetChunkPositionAroundOriginPosition(WorldComponent world, float3 originPosition)
        {
            int startX = (int)originPosition.x - world.ChunkRange * world.ChunkSize;
            int endX = (int)originPosition.x + world.ChunkRange * world.ChunkSize;

            int startZ = (int)originPosition.z - world.ChunkRange * world.ChunkSize;
            int endZ = (int)originPosition.z + world.ChunkRange * world.ChunkSize;

            int length = ((endX - startX) / world.ChunkSize + 1) * ((endZ - startZ) / world.ChunkSize + 1);
            int count = 0;

            NativeArray<float3> chunkToCreate = new NativeArray<float3>(length, Allocator.Temp);

            for (int x = startX; x <= endX; x+= world.ChunkSize)
            {
                for (int z = startZ; z <= endZ; z+= world.ChunkSize)
                {
                    float3 chunkPosition = GetChunkPositionFromCoordinate(world, new float3(x, 0, z));
                    chunkToCreate[count] = chunkPosition;
                    count++;
                }
            }
            return chunkToCreate;
        }
    }

}
