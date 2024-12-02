using Unity.Entities;
using Unity.Burst;

namespace VoxelGameEngine.Ticks
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct TicksSystem : ISystem
    {
        private uint dayValue;

        [BurstCompile]
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TicksComponent>();
            dayValue = 28800;
        }

        [BurstCompile]
        void OnUpdate(ref SystemState state) 
        {
            ref TicksComponent ticksComponent = ref SystemAPI.GetSingletonRW<TicksComponent>().ValueRW;
            if (ticksComponent.Value >= dayValue)
            {
                ticksComponent.Value = 0;
            }
            ticksComponent.Value++;
        }
    }
}
