using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Audio;
using DustlineArena.Runtime.Weapons;
using DustlineArena.Runtime.Health;
using UnityEngine;

namespace DustlineArena.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private MouseAimController aimController;
        [SerializeField] private ProjectileWeapon weapon;
        [SerializeField] private HealthComponent health;

        private bool emptyFeedbackPlayed;

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            if (aimController == null)
            {
                aimController = GetComponent<MouseAimController>();
            }

            if (weapon == null)
            {
                weapon = GetComponentInChildren<ProjectileWeapon>();
            }

            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (health != null && !health.IsAlive)
            {
                return;
            }

            if (inputReader == null || aimController == null || weapon == null)
            {
                return;
            }

            if (inputReader.ReloadPressed)
            {
                weapon.BeginReload();
            }

            if (weapon.Config == null)
            {
                return;
            }

            bool fireRequested = weapon.Config.Visual == WeaponVisualId.Pistol
                ? inputReader.FirePressed
                : inputReader.FireHeld;
            if (!fireRequested || weapon.AmmoInMagazine > 0)
            {
                emptyFeedbackPlayed = false;
            }

            if (fireRequested)
            {
                Vector3 aimOrigin = weapon.Muzzle == null ? transform.position : weapon.Muzzle.position;
                Vector3 fireDirection = aimController.GetAimDirectionFrom(aimOrigin);
                bool fired = weapon.TryFire(fireDirection);
                if (!fired && weapon.AmmoInMagazine == 0)
                {
                    if (!emptyFeedbackPlayed)
                    {
                        GameAudio.PlayEmptyWeapon(aimOrigin);
                        emptyFeedbackPlayed = true;
                    }

                    weapon.BeginReload();
                }
            }
        }

    }
}
