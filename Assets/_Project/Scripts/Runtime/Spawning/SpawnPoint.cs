using UnityEngine;

namespace DustlineArena.Runtime.Spawning
{
    public sealed class SpawnPoint : MonoBehaviour
    {
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
    }
}
