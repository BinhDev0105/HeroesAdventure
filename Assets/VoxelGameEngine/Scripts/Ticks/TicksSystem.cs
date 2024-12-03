using Unity.Entities;
using Unity.Burst;
using System;

namespace VoxelGameEngine.Ticks
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
    [BurstCompile]
    public partial struct TicksSystem : ISystem
    {
        public struct TickComponent : IComponentData
        {
            public uint Value;
        }

        [BurstCompile]
        void OnCreate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<TickComponent>())
            {
                Entity singletonEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(singletonEntity, new TickComponent());
            }
        }

        [BurstCompile]
        void OnUpdate(ref SystemState state) 
        {
            ref TickComponent ticksComponent = ref SystemAPI.GetSingletonRW<TickComponent>().ValueRW;
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
