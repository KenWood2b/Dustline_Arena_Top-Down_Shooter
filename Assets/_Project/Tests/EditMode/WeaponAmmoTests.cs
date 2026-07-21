using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Weapons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DustlineArena.Tests.EditMode
{
    public sealed class WeaponAmmoTests
    {
        private GameObject owner;
        private WeaponConfig config;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void AddAmmo_UsesReserveLimit()
        {
            config = ScriptableObject.CreateInstance<WeaponConfig>();
            SerializedObject serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("magazineSize").intValue = 12;
            serializedConfig.FindProperty("startingReserveAmmo").intValue = 0;
            serializedConfig.FindProperty("maxReserveAmmo").intValue = 20;
            serializedConfig.FindProperty("ammoPerPickup").intValue = 5;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();

            owner = new GameObject("Weapon Ammo Test");
            ProjectileWeapon weapon = owner.AddComponent<ProjectileWeapon>();
            weapon.Equip(config);

            Assert.That(weapon.AmmoInMagazine, Is.EqualTo(12));
            Assert.That(weapon.ReserveAmmo, Is.Zero);
            Assert.That(weapon.AddAmmo(5), Is.True);
            Assert.That(weapon.ReserveAmmo, Is.EqualTo(5));

            weapon.AddAmmo(100);
            Assert.That(weapon.ReserveAmmo, Is.EqualTo(20));
            Assert.That(weapon.AddAmmo(1), Is.False);
        }
    }
}
