using UnityEngine;

namespace DustlineArena.Runtime.Config
{
    [CreateAssetMenu(menuName = "Dustline Arena/Config/Grenade", fileName = "GrenadeConfig")]
    public sealed class GrenadeConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float damage = 85f;
        [SerializeField, Min(0.1f)] private float fuseTime = 1.8f;
        [SerializeField, Min(0.1f)] private float explosionRadius = 3.2f;
        [SerializeField, Min(0f)] private float minThrowSpeed = 4.5f;
        [SerializeField, Min(0f)] private float maxThrowSpeed = 13f;
        [SerializeField, Min(0f)] private float minThrowUpwardSpeed = 3.4f;
        [SerializeField, Min(0f)] private float maxThrowUpwardSpeed = 7.5f;
        [SerializeField, Min(0.05f)] private float chargeDuration = 1.4f;
        [SerializeField, Min(0f)] private float throwCooldown = 0.4f;
        [SerializeField, Min(1)] private int maximumCount = 2;
        [SerializeField, Min(0)] private int startingCount = 2;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private GameObject thrownPrefab;

        public float Damage => damage;
        public float FuseTime => fuseTime;
        public float ExplosionRadius => explosionRadius;
        public float MinThrowSpeed => minThrowSpeed;
        public float MaxThrowSpeed => maxThrowSpeed;
        public float MinThrowUpwardSpeed => minThrowUpwardSpeed;
        public float MaxThrowUpwardSpeed => maxThrowUpwardSpeed;
        public float ChargeDuration => chargeDuration;
        public float ThrowCooldown => throwCooldown;
        public int MaximumCount => maximumCount;
        public int StartingCount => Mathf.Min(startingCount, maximumCount);
        public LayerMask HitMask => hitMask;
        public GameObject ThrownPrefab => thrownPrefab;
    }
}
