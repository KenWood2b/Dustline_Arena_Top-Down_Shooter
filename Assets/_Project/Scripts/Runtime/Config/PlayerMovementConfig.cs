using UnityEngine;

namespace DustlineArena.Runtime.Config
{
    [CreateAssetMenu(menuName = "Dustline Arena/Config/Player Movement", fileName = "PlayerMovementConfig")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float moveSpeed = 8f;
        [SerializeField, Min(0f)] private float acceleration = 45f;
        [SerializeField, Min(0f)] private float rotationSpeed = 720f;
        [SerializeField, Min(0f)] private float dodgeSpeed = 16f;
        [SerializeField, Min(0.01f)] private float dodgeDuration = 0.28f;
        [SerializeField, Min(0f)] private float dodgeCooldown = 1.35f;
        [SerializeField, Min(0f)] private float dodgeInvulnerabilityDuration = 0.22f;

        public float MoveSpeed => moveSpeed;
        public float Acceleration => acceleration;
        public float RotationSpeed => rotationSpeed;
        public float DodgeSpeed => dodgeSpeed;
        public float DodgeDuration => dodgeDuration;
        public float DodgeCooldown => dodgeCooldown;
        public float DodgeInvulnerabilityDuration => dodgeInvulnerabilityDuration;
    }
}
