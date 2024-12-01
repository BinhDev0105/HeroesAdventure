using UnityEngine;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace VoxelGameEngine.Ticks
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct TicksSystem : ISystem
    {
        private uint timeCycle;
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TicksComponent>();
            timeCycle = 28800;
        }

        void OnUpdate(ref SystemState state) 
        {
            ref TicksComponent ticksComponent = ref SystemAPI.GetSingletonRW<TicksComponent>().ValueRW;
            if (ticksComponent.Value >= timeCycle)
            {
                ticksComponent.Value = 0;
            }
            ticksComponent.Value++;
        }
    }
}
