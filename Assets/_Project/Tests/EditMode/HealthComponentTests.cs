using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Health;
using NUnit.Framework;
using UnityEngine;

namespace DustlineArena.Tests.EditMode
{
    public sealed class HealthComponentTests
    {
        private GameObject owner;
        private HealthComponent health;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("Health Test Owner");
            health = owner.AddComponent<HealthComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(owner);
        }

        [Test]
        public void TakeDamage_ReducesHealthAndRaisesEvents()
        {
            int damagedCount = 0;
            int changedCount = 0;
            health.Damaged += _ => damagedCount++;
            health.Changed += _ => changedCount++;

            health.TakeDamage(CreateDamage(25f));

            Assert.That(health.Current, Is.EqualTo(75f));
            Assert.That(damagedCount, Is.EqualTo(1));
            Assert.That(changedCount, Is.EqualTo(1));
        }

        [Test]
        public void TakeDamage_WhenLethal_RaisesDeathOnlyOnce()
        {
            int deathCount = 0;
            health.Died += () => deathCount++;

            health.TakeDamage(CreateDamage(150f));
            health.TakeDamage(CreateDamage(10f));

            Assert.That(health.IsAlive, Is.False);
            Assert.That(health.Current, Is.Zero);
            Assert.That(deathCount, Is.EqualTo(1));
        }

        [Test]
        public void Heal_ClampsAtMaximumHealth()
        {
            health.TakeDamage(CreateDamage(30f));

            bool healed = health.Heal(100f);

            Assert.That(healed, Is.True);
            Assert.That(health.Current, Is.EqualTo(health.Max));
        }

        [Test]
        public void ResetHealth_RestoresDeadComponent()
        {
            health.TakeDamage(CreateDamage(health.Max));

            health.ResetHealth();

            Assert.That(health.IsAlive, Is.True);
            Assert.That(health.Current, Is.EqualTo(health.Max));
        }

        private DamageInfo CreateDamage(float amount)
        {
            return new DamageInfo(amount, owner, TeamId.Enemy, owner.transform.position, Vector3.forward);
        }
    }
}
