using Unity.Entities;

namespace VoxelGameEngine.Player
{
    public struct Player : IComponentData
    {
        public Entity PlayerPrefab;
        public Entity CharacterPrefab;
        public Entity CameraPrefab;
    }
}
