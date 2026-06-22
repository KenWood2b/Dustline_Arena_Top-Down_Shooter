using DustlineArena.Runtime.Config;
using UnityEngine;

namespace DustlineArena.Runtime.Pickups
{
    [DisallowMultipleComponent]
    public sealed class WeaponPickupRingSpawner : MonoBehaviour
    {
        [SerializeField] private WeaponConfig[] weaponConfigs;
        [SerializeField] private GameObject[] weaponModels;
        [SerializeField, Min(1f)] private float radius = 6f;
        [SerializeField, Min(0f)] private float weaponHeight = 0.8f;
        [SerializeField] private float startAngle = 90f;
        [SerializeField] private Color commonColor = Color.white;
        [SerializeField] private Color legendaryColor = new Color(1f, 0.58f, 0.05f);

        private void Awake()
        {
            SpawnPickups();
        }

        public void Configure(WeaponConfig[] configs, GameObject[] models)
        {
            weaponConfigs = configs;
            weaponModels = models;
        }

        private void SpawnPickups()
        {
            int count = Mathf.Min(
                weaponConfigs == null ? 0 : weaponConfigs.Length,
                weaponModels == null ? 0 : weaponModels.Length);
            if (count == 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                WeaponConfig config = weaponConfigs[i];
                GameObject modelPrefab = weaponModels[i];
                if (config == null || modelPrefab == null)
                {
                    continue;
                }

                float angle = startAngle - 360f * i / count;
                float radians = angle * Mathf.Deg2Rad;
                Vector3 localPosition = new Vector3(
                    Mathf.Cos(radians) * radius,
                    0.03f,
                    Mathf.Sin(radians) * radius);
                Color highlightColor = Color.Lerp(
                    commonColor,
                    legendaryColor,
                    count <= 1 ? 1f : i / (float)(count - 1));

                CreatePickup(config, modelPrefab, localPosition, highlightColor);
            }
        }

        private void CreatePickup(
            WeaponConfig config,
            GameObject modelPrefab,
            Vector3 localPosition,
            Color highlightColor)
        {
            GameObject pickupRoot = new GameObject($"Pickup_{config.Visual}");
            pickupRoot.transform.SetParent(transform, false);
            pickupRoot.transform.localPosition = localPosition;

            CreateHighlight(pickupRoot.transform, highlightColor);

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
            pickup.Configure(config, visual.transform, false);
        }

        private static float GetPickupScale(WeaponVisualId visual)
        {
            return visual == WeaponVisualId.Grenade ? 38f : 100f;
        }

        private static void CreateHighlight(Transform parent, Color color)
        {
            GameObject highlight = new GameObject("Pickup_Highlight_Particles");
            highlight.transform.SetParent(parent, false);
            highlight.transform.localPosition = Vector3.up * 0.04f;
            highlight.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem particles = highlight.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.startLifetime = 1.35f;
            main.startSpeed = 0.08f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(color.r, color.g, color.b, 0.28f),
                new Color(color.r, color.g, color.b, 0.9f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 48;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 22f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.78f;
            shape.radiusThickness = 0.06f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Loop;
            shape.arcSpeed = 0.28f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient alphaGradient = new Gradient();
            alphaGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.8f, 0.2f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = alphaGradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1f));

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateParticleMaterial(color);
            particles.Play();
        }

        private static Material CreateParticleMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Unlit/Transparent");
            Material material = new Material(shader)
            {
                name = $"Pickup Particles {ColorUtility.ToHtmlStringRGB(color)}",
                color = color
            };

            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            {
                name = "Soft Particle"
            };
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    Vector2 uv = new Vector2(
                        (x + 0.5f) / texture.width * 2f - 1f,
                        (y + 0.5f) / texture.height * 2f - 1f);
                    float alpha = Mathf.Clamp01(1f - uv.magnitude);
                    alpha *= alpha;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            return material;
        }
    }
}
