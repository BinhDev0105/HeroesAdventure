using UnityEngine;

namespace VoxelGameEngine.ThirdPerson
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
