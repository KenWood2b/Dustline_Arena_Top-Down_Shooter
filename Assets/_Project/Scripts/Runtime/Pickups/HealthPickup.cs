using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Health;
using UnityEngine;

namespace DustlineArena.Runtime.Pickups
{
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public sealed class HealthPickup : MonoBehaviour
    {
        private const string ResourcePath = "Pickups/HealthPickup";

        [SerializeField, Min(0f)] private float healAmount = 25f;
        [SerializeField] private bool destroyOnPickup = true;
        [SerializeField, Min(1f)] private float lifetime = 25f;
        [SerializeField, Min(0f)] private float rotationSpeed = 65f;
        [SerializeField, Min(0f)] private float hoverHeight = 0.1f;
        [SerializeField] private bool visualFeedbackEnabled;

        private Vector3 basePosition;
        private float spawnedAt;

        private void Awake()
        {
            ResetRuntimeState();
            RefreshFeedback();
        }

        private void Reset()
        {
            Collider pickupCollider = GetComponent<Collider>();
            pickupCollider.isTrigger = true;
        }

        private void Update()
        {
            if (!visualFeedbackEnabled)
            {
                return;
            }

            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            Vector3 position = basePosition;
            position.y += Mathf.Sin((Time.time - spawnedAt) * 2.7f) * hoverHeight;
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

            HealthComponent health = other.GetComponentInParent<HealthComponent>();
            if (health == null || !health.IsAlive)
            {
                return;
            }

            float previousHealth = health.Current;
            if (health.Heal(healAmount) && destroyOnPickup)
            {
                int healed = Mathf.CeilToInt(health.Current - previousHealth);
                PickupFeedback.ShowPopup(transform.position, $"+{healed} HP", new Color(0.46f, 1f, 0.55f, 1f));
                Destroy(gameObject);
            }
        }

        public static HealthPickup Create(Vector3 position, float amount = 0f)
        {
            HealthPickup prefab = Resources.Load<HealthPickup>(ResourcePath);
            if (prefab != null)
            {
                HealthPickup pickup = Instantiate(prefab, position + Vector3.up * 0.18f, Quaternion.identity);
                if (amount > 0f)
                {
                    pickup.healAmount = amount;
                }

                pickup.ResetRuntimeState();
                pickup.SetVisualFeedbackEnabled(true);
                return pickup;
            }

            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickupObject.name = "Health_Pickup";
            pickupObject.transform.position = position + Vector3.up * 0.45f;
            pickupObject.transform.localScale = new Vector3(0.72f, 0.32f, 0.48f);

            Collider collider = pickupObject.GetComponent<Collider>();
            collider.isTrigger = true;

            Renderer renderer = pickupObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                properties.SetColor("_BaseColor", new Color(0.12f, 0.72f, 0.28f));
                properties.SetColor("_Color", new Color(0.12f, 0.72f, 0.28f));
                renderer.SetPropertyBlock(properties);
            }

            HealthPickup fallbackPickup = pickupObject.AddComponent<HealthPickup>();
            if (amount > 0f)
            {
                fallbackPickup.healAmount = amount;
            }

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
                PickupFeedback.Ensure(gameObject, "MEDKIT", new Color(0.46f, 1f, 0.55f, 1f));
                return;
            }

            PickupFeedback.Remove(gameObject);
        }
    }
}
