using UnityEngine;

namespace DustlineArena.Runtime.Config
{
    [CreateAssetMenu(menuName = "Dustline Arena/Config/Player Movement", fileName = "PlayerMovementConfig")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float moveSpeed = 8f;
        [SerializeField, Min(0f)] private float acceleration = 45f;
        [SerializeField, Min(0f)] private float rotationSpeed = 720f;

        public float MoveSpeed => moveSpeed;
        public float Acceleration => acceleration;
        public float RotationSpeed => rotationSpeed;
    }
}
