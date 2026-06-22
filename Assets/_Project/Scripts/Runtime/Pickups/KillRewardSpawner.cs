using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Spawning;
using UnityEngine;

namespace DustlineArena.Runtime.Pickups
{
    [DisallowMultipleComponent]
    public sealed class KillRewardSpawner : MonoBehaviour
    {
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private int[] killThresholds = { 8, 20, 32 };
        [SerializeField] private WeaponConfig[] weaponConfigs;
        [SerializeField] private GameObject[] weaponModels;
        [SerializeField] private Transform[] rewardPoints;
        [SerializeField, Min(0f)] private float weaponHeight = 0.8f;

        private int nextRewardIndex;
        private int lastRewardPointIndex = -1;
        private WeaponPickup activePickup;

        private void OnEnable()
        {
            if (waveSpawner != null)
            {
                waveSpawner.EnemyKilled += OnEnemyKilled;
            }
        }

        private void OnDisable()
        {
            if (waveSpawner != null)
            {
                waveSpawner.EnemyKilled -= OnEnemyKilled;
            }

            if (activePickup != null)
            {
                activePickup.PickedUp -= OnPickupCollected;
            }
        }

        public void Configure(
            WaveSpawner source,
            int[] thresholds,
            WeaponConfig[] configs,
            GameObject[] models,
            Transform[] points)
        {
            waveSpawner = source;
            killThresholds = thresholds;
            weaponConfigs = configs;
            weaponModels = models;
            rewardPoints = points;
        }

        private void OnEnemyKilled(int totalKills)
        {
            TrySpawnEligibleReward(totalKills);
        }

        private void TrySpawnEligibleReward(int totalKills)
        {
            if (activePickup != null
                || killThresholds == null
                || nextRewardIndex >= killThresholds.Length
                || totalKills < killThresholds[nextRewardIndex])
            {
                return;
            }

            int rewardCount = Mathf.Min(
                weaponConfigs == null ? 0 : weaponConfigs.Length,
                weaponModels == null ? 0 : weaponModels.Length);
            if (rewardCount == 0 || rewardPoints == null || rewardPoints.Length == 0)
            {
                return;
            }

            int rewardIndex = Mathf.Min(nextRewardIndex, rewardCount - 1);
            Transform rewardPoint = SelectRewardPoint();
            activePickup = CreatePickup(
                weaponConfigs[rewardIndex],
                weaponModels[rewardIndex],
                rewardPoint.position);
            if (activePickup == null)
            {
                return;
            }

            activePickup.PickedUp += OnPickupCollected;
            nextRewardIndex++;
        }

        private Transform SelectRewardPoint()
        {
            int index = Random.Range(0, rewardPoints.Length);
            if (rewardPoints.Length > 1 && index == lastRewardPointIndex)
            {
                index = (index + Random.Range(1, rewardPoints.Length)) % rewardPoints.Length;
            }

            lastRewardPointIndex = index;
            return rewardPoints[index];
        }

        private WeaponPickup CreatePickup(WeaponConfig config, GameObject modelPrefab, Vector3 position)
        {
            if (config == null || modelPrefab == null)
            {
                return null;
            }

            GameObject pickupRoot = new GameObject($"KillReward_{config.Visual}");
            pickupRoot.transform.SetParent(transform, true);
            pickupRoot.transform.position = position;

            GameObject visual = Instantiate(modelPrefab, pickupRoot.transform);
            visual.name = config.Visual + "_Visual";
            visual.transform.localPosition = Vector3.up * weaponHeight;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * GetPickupScale(config.Visual);

            BoxCollider trigger = pickupRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.up * weaponHeight;
            trigger.size = new Vector3(1.8f, 1.6f, 1.8f);

            WeaponPickup pickup = pickupRoot.AddComponent<WeaponPickup>();
            pickup.Configure(config, visual.transform);
            return pickup;
        }

        private void OnPickupCollected(WeaponPickup pickup)
        {
            pickup.PickedUp -= OnPickupCollected;
            activePickup = null;
            int totalKills = waveSpawner == null ? 0 : waveSpawner.TotalKills;
            TrySpawnEligibleReward(totalKills);
        }

        private static float GetPickupScale(WeaponVisualId visual)
        {
            return visual == WeaponVisualId.Grenade ? 38f : 100f;
        }
    }
}
