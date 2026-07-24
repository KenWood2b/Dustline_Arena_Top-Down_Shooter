using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Audio;
using DustlineArena.Runtime.Weapons;
using UnityEngine;

namespace DustlineArena.Runtime.Pickups
{
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public sealed class AmmoPickup : MonoBehaviour
    {
        private const string ResourcePath = "Pickups/AmmoPickup";

        [SerializeField, Min(0)] private int amountOverride;
        [SerializeField, Min(1f)] private float lifetime = 25f;
        [SerializeField, Min(0f)] private float rotationSpeed = 80f;
        [SerializeField, Min(0f)] private float hoverHeight = 0.12f;
        [SerializeField] private bool visualFeedbackEnabled;

        private Vector3 basePosition;
        private float spawnedAt;

        private void Awake()
        {
            ResetRuntimeState();
            RefreshFeedback();
        }

        private void Update()
        {
            if (!visualFeedbackEnabled)
            {
                return;
            }

            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            Vector3 position = basePosition;
            position.y += Mathf.Sin((Time.time - spawnedAt) * 3f) * hoverHeight;
            transform.position = position;

            if (Time.time - spawnedAt >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!visualFeedbackEnabled)
            {
                return;
            }

            TeamMember team = other.GetComponentInParent<TeamMember>();
            if (team == null || team.Team != TeamId.Player)
            {
                return;
            }

            ProjectileWeapon weapon = team.GetComponentInChildren<ProjectileWeapon>();
            if (weapon == null || weapon.Config == null)
            {
                return;
            }

            int amount = amountOverride > 0 ? amountOverride : weapon.Config.AmmoPerPickup;
            if (weapon.AddAmmo(amount))
            {
                GameAudio.PlayPickup(transform.position);
                PickupFeedback.ShowPopup(transform.position, $"+{amount} AMMO", new Color(0.84f, 1f, 0.36f, 1f));
                Destroy(gameObject);
            }
        }

        public static AmmoPickup Create(Vector3 position, int amount = 0)
        {
            AmmoPickup prefab = Resources.Load<AmmoPickup>(ResourcePath);
            if (prefab != null)
            {
                AmmoPickup pickup = Instantiate(prefab, position + Vector3.up * 0.18f, Quaternion.identity);
                pickup.amountOverride = Mathf.Max(0, amount);
                pickup.ResetRuntimeState();
                pickup.SetVisualFeedbackEnabled(true);
                return pickup;
            }

            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickupObject.name = "Ammo_Pickup";
            pickupObject.transform.position = position + Vector3.up * 0.45f;
            pickupObject.transform.localScale = new Vector3(0.65f, 0.34f, 0.46f);

            Collider collider = pickupObject.GetComponent<Collider>();
            collider.isTrigger = true;

            Renderer renderer = pickupObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                properties.SetColor("_BaseColor", new Color(0.58f, 0.75f, 0.36f));
                properties.SetColor("_Color", new Color(0.58f, 0.75f, 0.36f));
                renderer.SetPropertyBlock(properties);
            }

            AmmoPickup fallbackPickup = pickupObject.AddComponent<AmmoPickup>();
            fallbackPickup.amountOverride = Mathf.Max(0, amount);
            fallbackPickup.SetVisualFeedbackEnabled(true);
            return fallbackPickup;
        }

        private void ResetRuntimeState()
        {
            basePosition = transform.position;
            spawnedAt = Time.time;
        }

        private void SetVisualFeedbackEnabled(bool enabled)
        {
            visualFeedbackEnabled = enabled;
            RefreshFeedback();
        }

        private void RefreshFeedback()
        {
            if (visualFeedbackEnabled)
            {
                PickupFeedback.Ensure(gameObject, "AMMO", new Color(0.84f, 1f, 0.36f, 1f));
                return;
            }

            PickupFeedback.Remove(gameObject);
        }
    }
}
