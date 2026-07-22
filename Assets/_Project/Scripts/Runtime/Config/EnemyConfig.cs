using UnityEngine;

namespace DustlineArena.Runtime.Config
{
    [CreateAssetMenu(menuName = "Dustline Arena/Config/Enemy", fileName = "EnemyConfig")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(0f)] private float acceleration = 28f;
        [SerializeField, Min(0f)] private float attackRange = 1.35f;
        [SerializeField, Min(0f)] private float attackDamage = 12f;
        [SerializeField, Min(0.05f)] private float attackCooldown = 0.75f;
        [SerializeField] private bool useChaseBurst;
        [SerializeField, Min(1f)] private float chaseBurstSpeedMultiplier = 1.25f;
        [SerializeField, Min(0.05f)] private float chaseBurstDuration = 0.45f;
        [SerializeField, Min(0.05f)] private float chaseBurstCooldown = 2.2f;
        [SerializeField, Min(0f)] private float chaseBurstMinDistance = 4.5f;

        public float MoveSpeed => moveSpeed;
        public float Acceleration => acceleration;
        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public float AttackCooldown => attackCooldown;
        public bool UseChaseBurst => useChaseBurst;
        public float ChaseBurstSpeedMultiplier => chaseBurstSpeedMultiplier;
        public float ChaseBurstDuration => chaseBurstDuration;
        public float ChaseBurstCooldown => chaseBurstCooldown;
        public float ChaseBurstMinDistance => chaseBurstMinDistance;
    }
}
