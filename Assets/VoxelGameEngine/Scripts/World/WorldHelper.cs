using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;
using VoxelGameEngine.Chunk;

namespace VoxelGameEngine.World
{
    public struct WorldHelper
    {
        public static int3 GetChunkPositionFromCoordinate(WorldComponent world, int3 position)
        {
            return new int3(
                Mathf.FloorToInt(position.x / world.ChunkSize) * world.ChunkSize,
                Mathf.FloorToInt(position.y / world.ChunkHeight) * world.ChunkHeight,
                Mathf.FloorToInt(position.z / world.ChunkSize) * world.ChunkSize
            );
        }

        public static int3 GetChunkPositionFromCoordinate(NativeArray<int3> chunkPositions, int3 position)
        {
            int3 nearest = chunkPositions[0];
            float minDistance = math.distance(chunkPositions[0], position);

            for (int i = 0; i < chunkPositions.Length; i++)
            {
                float distance = math.distance(chunkPositions[i], position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = chunkPositions[i];
                }
            }
            return nearest;
        }

        public static int3 GetBlockPositionInChunkCoordinate(int3 chunkPosition, int3 position)
        {
            return new int3(
                position.x - chunkPosition.x,
                position.y - chunkPosition.y,
                position.z - chunkPosition.z
                );
        }

        public static bool IsOnEdge(int chunkSize, int3 chunkPosition, int3 position)
        {
            int3 blockPosition = GetBlockPositionInChunkCoordinate(chunkPosition, position);
            if (Mathf.Abs(blockPosition.x) < chunkSize - 1 && Mathf.Abs(blockPosition.z) < chunkSize - 1)
            {
                return true;
            }
            return false;
        }

        public struct ChunkData
        {
            public int StartX;
            public int EndX;
            public int StartZ;
            public int EndZ;
            public int Length;
        }

        public static ChunkData SetupChunkData(WorldComponent world, int3 originPosition)
        {
            int startX = (int)originPosition.x - world.ChunkRange * world.ChunkSize;
            int endX = (int)originPosition.x + world.ChunkRange * world.ChunkSize;

            int startZ = (int)originPosition.z - world.ChunkRange * world.ChunkSize;
            int endZ = (int)originPosition.z + world.ChunkRange * world.ChunkSize;

            int length = ((endX - startX) / world.ChunkSize + 1) * ((endZ - startZ) / world.ChunkSize + 1);

            ChunkData data = new ChunkData();
            data.StartX = startX;
            data.EndX = endX;
            data.StartZ = startZ;
            data.EndZ = endZ;
            data.Length = length;
            return data;
        }


    }

}
