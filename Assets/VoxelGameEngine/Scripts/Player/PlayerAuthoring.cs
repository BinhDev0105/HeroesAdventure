using Unity.Entities;
using UnityEngine;

namespace VoxelGameEngine.Player
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public GameObject PlayerPrefab;
        public GameObject CharacterPrefab;
        public GameObject CameraPrefab;

        class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Player
                {
                    PlayerPrefab = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.None),
                    CharacterPrefab = GetEntity(authoring.CharacterPrefab, TransformUsageFlags.None),
                    CameraPrefab = GetEntity(authoring.CameraPrefab, TransformUsageFlags.None)
                });
            }
        }
    }
}
