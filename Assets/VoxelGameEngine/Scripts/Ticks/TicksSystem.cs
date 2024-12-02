using Unity.Entities;
using Unity.Burst;
using System;

namespace VoxelGameEngine.Ticks
{
    [BurstCompile]
    public partial struct TicksSystem : ISystem
    {
        private long maxValue;

        [BurstCompile]
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TicksComponent>();
            maxValue = 28800;
        }

        [BurstCompile]
        void OnUpdate(ref SystemState state) 
        {
            ref TicksComponent ticksComponent = ref SystemAPI.GetSingletonRW<TicksComponent>().ValueRW;
            if (ticksComponent.Value >= maxValue)
            {
                ticksComponent.Value = 0;
            }
            ticksComponent.Value++;
        }
    }

    public partial struct DateTimeTicksSystem : ISystem
    {
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DateTimeTicksComponent>();
        }

        void OnUpdate(ref SystemState state)
        {
            ref DateTimeTicksComponent ticks = ref SystemAPI.GetSingletonRW<DateTimeTicksComponent>().ValueRW;
            if (ticks.Active == false)
            {
                return;
            }
            ticks.Value = DateTime.Now.Ticks;
        }
    }
}
