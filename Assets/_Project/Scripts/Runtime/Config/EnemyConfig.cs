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

        public float MoveSpeed => moveSpeed;
        public float Acceleration => acceleration;
        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public float AttackCooldown => attackCooldown;
    }
}
