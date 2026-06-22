using DustlineArena.Runtime.Health;
using UnityEngine;

namespace DustlineArena.Runtime.Pickups
{
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public sealed class HealthPickup : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float healAmount = 25f;
        [SerializeField] private bool destroyOnPickup = true;

        private void Reset()
        {
            Collider pickupCollider = GetComponent<Collider>();
            pickupCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            HealthComponent health = other.GetComponentInParent<HealthComponent>();
            if (health == null || !health.IsAlive)
            {
                return;
            }

            if (health.Heal(healAmount) && destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }
    }
}
