using UnityEngine;

namespace VoxelGameEngine
{
    public class MainGameObjectCamera : MonoBehaviour
    {
        public static Camera Instance;

        void Awake()
        {
            Instance = GetComponent<UnityEngine.Camera>();
        }
    }
}
