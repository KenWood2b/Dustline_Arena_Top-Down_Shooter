using System;
using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Health;
using DustlineArena.Runtime.Weapons;
using UnityEngine;

namespace DustlineArena.Runtime.Pickups
{
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public sealed class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private WeaponConfig weaponConfig;
        [SerializeField] private bool destroyOnPickup = true;
        [SerializeField] private Transform animatedVisual;
        [SerializeField, Min(0f)] private float rotationSpeed = 70f;
        [SerializeField, Min(0f)] private float hoverHeight = 0.12f;
        [SerializeField, Min(0f)] private float hoverSpeed = 1.8f;

        private Vector3 basePosition;
        private Vector3 visualBaseLocalPosition;

        public event Action<WeaponPickup> PickedUp;

        private void OnEnable()
        {
            basePosition = transform.position;
            if (animatedVisual != null)
            {
                visualBaseLocalPosition = animatedVisual.localPosition;
            }
        }

        private void Reset()
        {
            Collider pickupCollider = GetComponent<Collider>();
            pickupCollider.isTrigger = true;
        }

        private void Update()
        {
            if (animatedVisual != null)
            {
                animatedVisual.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

                Vector3 localPosition = visualBaseLocalPosition;
                localPosition.y += Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
                animatedVisual.localPosition = localPosition;
                return;
            }

            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            Vector3 worldPosition = basePosition;
            worldPosition.y += Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
            transform.position = worldPosition;
        }

        public void Configure(WeaponConfig config, Transform visual, bool consumeOnPickup = true)
        {
            weaponConfig = config;
            animatedVisual = visual;
            destroyOnPickup = consumeOnPickup;
            basePosition = transform.position;
            visualBaseLocalPosition = animatedVisual == null
                ? Vector3.zero
                : animatedVisual.localPosition;
        }

        private void OnTriggerEnter(Collider other)
        {
            HealthComponent health = other.GetComponentInParent<HealthComponent>();
            if (health == null || !health.IsAlive)
            {
                return;
            }

            TeamMember teamMember = other.GetComponentInParent<TeamMember>();
            if (teamMember == null || teamMember.Team != TeamId.Player)
            {
                return;
            }

            ProjectileWeapon weapon = other.GetComponentInParent<ProjectileWeapon>()
                ?? other.GetComponentInChildren<ProjectileWeapon>();

            if (weapon == null)
            {
                weapon = health.GetComponentInChildren<ProjectileWeapon>();
            }

            if (weapon == null || weaponConfig == null)
            {
                return;
            }

            weapon.Equip(weaponConfig);
            PickedUp?.Invoke(this);

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }
    }
}
