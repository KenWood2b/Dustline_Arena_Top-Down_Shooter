using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Health;
using DustlineArena.Runtime.Weapons;
using UnityEngine;

namespace DustlineArena.Runtime.Pickups
{
    [RequireComponent(typeof(HealthComponent))]
    [DisallowMultipleComponent]
    public sealed class EnemyAmmoDropper : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float dropChance = 0.18f;
        [SerializeField, Range(0f, 1f)] private float lowAmmoChanceBonus = 0.17f;
        [SerializeField, Range(0f, 1f)] private float healthDropChance = 0.035f;
        [SerializeField, Min(1)] private int guaranteedDropAfterKills = 6;
        [SerializeField, Min(0f)] private float pickupScatterRadius = 0.65f;

        private static int playerKillsSinceDrop;
        private HealthComponent health;
        private ProjectileWeapon lastPlayerWeapon;
        private bool damagedByPlayer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedState()
        {
            playerKillsSinceDrop = 0;
        }

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            damagedByPlayer = false;
            lastPlayerWeapon = null;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void OnDamaged(DamageInfo damage)
        {
            if (damage.Source == null)
            {
                return;
            }

            TeamMember sourceTeam = damage.Source.GetComponentInParent<TeamMember>();
            if (sourceTeam == null || sourceTeam.Team != TeamId.Player)
            {
                return;
            }

            damagedByPlayer = true;
            lastPlayerWeapon = damage.Source.GetComponentInParent<ProjectileWeapon>()
                ?? sourceTeam.GetComponentInChildren<ProjectileWeapon>();
        }

        private void OnDied()
        {
            if (!damagedByPlayer)
            {
                return;
            }

            TryDropAmmo();
            TryDropHealth();
        }

        private void TryDropAmmo()
        {
            float effectiveChance = dropChance;
            if (lastPlayerWeapon != null
                && lastPlayerWeapon.Config != null
                && lastPlayerWeapon.TotalAmmo <= lastPlayerWeapon.Config.MagazineSize)
            {
                effectiveChance = Mathf.Clamp01(effectiveChance + lowAmmoChanceBonus);
            }

            bool guaranteed = playerKillsSinceDrop >= Mathf.Max(1, guaranteedDropAfterKills) - 1;
            if (!guaranteed && Random.value > effectiveChance)
            {
                playerKillsSinceDrop++;
                return;
            }

            playerKillsSinceDrop = 0;
            AmmoPickup.Create(GetDropPosition());
        }

        private void TryDropHealth()
        {
            if (healthDropChance <= 0f || Random.value > healthDropChance)
            {
                return;
            }

            HealthPickup.Create(GetDropPosition());
        }

        private Vector3 GetDropPosition()
        {
            if (pickupScatterRadius <= 0f)
            {
                return transform.position;
            }

            Vector2 offset = Random.insideUnitCircle * pickupScatterRadius;
            return transform.position + new Vector3(offset.x, 0f, offset.y);
        }
    }
}
