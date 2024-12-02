using Unity.Entities;

namespace VoxelGameEngine.Player
{
    public struct PlayerComponent : IComponentData
    {
        public Entity PlayerPrefab;
        public Entity CharacterPrefab;
        public Entity CameraPrefab;
    }
}
