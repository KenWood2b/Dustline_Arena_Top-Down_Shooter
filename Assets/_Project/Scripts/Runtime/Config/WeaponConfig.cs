using UnityEngine;

namespace DustlineArena.Runtime.Config
{
    public enum WeaponVisualId
    {
        AK,
        SMG,
        Pistol,
        Shotgun,
        Grenade
    }

    [CreateAssetMenu(menuName = "Dustline Arena/Config/Weapon", fileName = "WeaponConfig")]
    public sealed class WeaponConfig : ScriptableObject
    {
        [SerializeField] private WeaponVisualId visual = WeaponVisualId.AK;
        [SerializeField, Min(0f)] private float damage = 18f;
        [SerializeField, Min(0.01f)] private float shotsPerSecond = 6f;
        [SerializeField, Min(0f)] private float projectileSpeed = 34f;
        [SerializeField, Min(0f)] private float projectileLifetime = 2f;
        [SerializeField, Range(0f, 20f)] private float spreadAngle = 1.5f;
        [SerializeField, Min(1)] private int projectilesPerShot = 1;
        [SerializeField, Min(0f)] private float explosionRadius;
        [SerializeField, Min(0f)] private float throwUpwardSpeed = 5.5f;
        [SerializeField, Min(0f)] private float minThrowSpeed = 4f;
        [SerializeField, Min(0f)] private float maxThrowSpeed = 12f;
        [SerializeField, Min(0f)] private float minThrowUpwardSpeed = 3.5f;
        [SerializeField, Min(0f)] private float maxThrowUpwardSpeed = 7f;
        [SerializeField, Min(0.05f)] private float throwChargeDuration = 1.25f;
        [Header("Ammo")]
        [SerializeField, Min(1)] private int magazineSize = 30;
        [SerializeField, Min(0)] private int startingReserveAmmo = 90;
        [SerializeField, Min(0)] private int maxReserveAmmo = 120;
        [SerializeField, Min(0.05f)] private float reloadDuration = 1.6f;
        [SerializeField, Min(1)] private int ammoPerPickup = 24;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject thrownPrefab;

        public WeaponVisualId Visual => visual;
        public float Damage => damage;
        public float ShotsPerSecond => shotsPerSecond;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public float SpreadAngle => spreadAngle;
        public int ProjectilesPerShot => projectilesPerShot;
        public float ExplosionRadius => explosionRadius;
        public float ThrowUpwardSpeed => throwUpwardSpeed;
        public float MinThrowSpeed => minThrowSpeed;
        public float MaxThrowSpeed => maxThrowSpeed;
        public float MinThrowUpwardSpeed => minThrowUpwardSpeed;
        public float MaxThrowUpwardSpeed => maxThrowUpwardSpeed;
        public float ThrowChargeDuration => throwChargeDuration;
        public int MagazineSize => magazineSize;
        public int StartingReserveAmmo => Mathf.Min(startingReserveAmmo, maxReserveAmmo);
        public int MaxReserveAmmo => maxReserveAmmo;
        public float ReloadDuration => reloadDuration;
        public int AmmoPerPickup => ammoPerPickup;
        public LayerMask HitMask => hitMask;
        public GameObject ProjectilePrefab => projectilePrefab;
        public GameObject ThrownPrefab => thrownPrefab;
    }
}
