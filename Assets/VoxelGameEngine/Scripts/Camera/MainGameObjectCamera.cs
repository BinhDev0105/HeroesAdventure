using UnityEngine;

namespace VoxelGameEngine.Camera
{
    public class MainGameObjectCamera : MonoBehaviour
    {
        public static UnityEngine.Camera Instance;

        private void Awake()
        {
            Instance = GetComponent<UnityEngine.Camera>();
        }
    }
}
