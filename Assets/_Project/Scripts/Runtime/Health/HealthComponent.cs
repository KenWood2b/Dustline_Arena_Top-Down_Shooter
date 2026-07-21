using System;
using DustlineArena.Runtime.Common;
using UnityEngine;

namespace DustlineArena.Runtime.Health
{
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath;

        private float currentHealth;
        private bool isInitialized;

        public event Action<HealthChangedArgs> Changed;
        public event Action<DamageInfo> Damaged;
        public event Action Died;
        public static event Action<HealthComponent, DamageInfo> AnyDamaged;

        public float Current => currentHealth;
        public float Max => maxHealth;
        public bool IsAlive => currentHealth > 0f;
        public bool DestroyOnDeath
        {
            get => destroyOnDeath;
            set => destroyOnDeath = value;
        }

        private void Awake()
        {
            ResetHealth();
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
        }

        public void ResetHealth()
        {
            InitializeHealth();
            Changed?.Invoke(new HealthChangedArgs(currentHealth, maxHealth, 0f));
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!isInitialized)
            {
                InitializeHealth();
            }

            if (!IsAlive || damage.Amount <= 0f)
            {
                return;
            }

            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - damage.Amount);

            Damaged?.Invoke(damage);
            AnyDamaged?.Invoke(this, damage);
            Changed?.Invoke(new HealthChangedArgs(currentHealth, maxHealth, currentHealth - previousHealth));

            if (currentHealth <= 0f)
            {
                Died?.Invoke();

                if (destroyOnDeath)
                {
                    Destroy(gameObject);
                }
            }
        }

        public bool Heal(float amount)
        {
            if (!isInitialized)
            {
                InitializeHealth();
            }

            if (!IsAlive || amount <= 0f || currentHealth >= maxHealth)
            {
                return false;
            }

            float previousHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            Changed?.Invoke(new HealthChangedArgs(currentHealth, maxHealth, currentHealth - previousHealth));
            return currentHealth > previousHealth;
        }

        private void InitializeHealth()
        {
            currentHealth = maxHealth;
            isInitialized = true;
        }
    }
}
