using DustlineArena.Runtime.Config;
using UnityEngine;

namespace DustlineArena.Runtime.Feedback
{
    public static class CombatVfx
    {
        private static Texture2D particleTexture;
        private static Material flashMaterial;
        private static Material sparkMaterial;
        private static Material hitMaterial;

        public static void SpawnMuzzleFlash(Transform muzzle, Vector3 direction, WeaponVisualId visual)
        {
            if (muzzle == null)
            {
                return;
            }

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector3.forward;
            }

            GameObject effect = new GameObject("Muzzle_Flash_FX");
            effect.transform.SetPositionAndRotation(muzzle.position + Vector3.up * 0.32f, Quaternion.LookRotation(direction.normalized, Vector3.up));
            effect.transform.SetParent(muzzle, true);

            ParticleSystem flash = effect.AddComponent<ParticleSystem>();
            ConfigureMuzzleFlash(flash, visual);

            Object.Destroy(effect, 0.45f);
        }

        public static void SpawnImpact(Vector3 position, Vector3 direction, bool hitDamageable)
        {
            GameObject effect = new GameObject(hitDamageable ? "Hit_Impact_FX" : "Surface_Impact_FX");
            effect.transform.position = position + Vector3.up * (hitDamageable ? 0.42f : 0.18f);
            if (direction.sqrMagnitude > 0.001f)
            {
                effect.transform.rotation = Quaternion.LookRotation(-direction.normalized, Vector3.up);
            }

            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            ConfigureImpact(particles, hitDamageable);

            Object.Destroy(effect, hitDamageable ? 1.05f : 0.8f);
        }

        private static void ConfigureMuzzleFlash(ParticleSystem particles, WeaponVisualId visual)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            bool shotgun = visual == WeaponVisualId.Shotgun;
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.duration = 0.16f;
            main.startLifetime = shotgun
                ? new ParticleSystem.MinMaxCurve(0.11f, 0.22f)
                : new ParticleSystem.MinMaxCurve(0.075f, 0.14f);
            main.startSpeed = shotgun
                ? new ParticleSystem.MinMaxCurve(2.4f, 4.8f)
                : new ParticleSystem.MinMaxCurve(1.6f, 3.2f);
            main.startSize = shotgun
                ? new ParticleSystem.MinMaxCurve(0.62f, 1.18f)
                : new ParticleSystem.MinMaxCurve(0.34f, 0.72f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.48f, 0.08f, 0.95f),
                new Color(1f, 0.94f, 0.42f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = shotgun ? 32 : 18;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, shotgun ? (short)28 : (short)14)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = shotgun ? 32f : 18f;
            shape.radius = shotgun ? 0.18f : 0.08f;
            shape.length = shotgun ? 0.7f : 0.42f;

            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = CreateFadeGradient(
                new Color(1f, 0.95f, 0.48f, 1f),
                new Color(1f, 0.24f, 0.02f, 0f));

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetFlashMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            particles.Play();
        }

        private static void ConfigureImpact(ParticleSystem particles, bool hitDamageable)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            Color start = hitDamageable
                ? new Color(1f, 0.22f, 0.08f, 0.95f)
                : new Color(1f, 0.78f, 0.22f, 0.95f);
            Color end = hitDamageable
                ? new Color(0.42f, 0.03f, 0.02f, 0f)
                : new Color(1f, 0.34f, 0.04f, 0f);

            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.duration = 0.22f;
            main.startLifetime = hitDamageable
                ? new ParticleSystem.MinMaxCurve(0.28f, 0.58f)
                : new ParticleSystem.MinMaxCurve(0.16f, 0.36f);
            main.startSpeed = hitDamageable
                ? new ParticleSystem.MinMaxCurve(1.2f, 3.6f)
                : new ParticleSystem.MinMaxCurve(2.2f, 6.2f);
            main.startSize = hitDamageable
                ? new ParticleSystem.MinMaxCurve(0.22f, 0.48f)
                : new ParticleSystem.MinMaxCurve(0.14f, 0.32f);
            main.startColor = new ParticleSystem.MinMaxGradient(start, Color.white);
            main.gravityModifier = hitDamageable ? 0.25f : 0.08f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = hitDamageable ? 32 : 22;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, hitDamageable ? (short)26 : (short)18)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = hitDamageable ? 48f : 34f;
            shape.radius = hitDamageable ? 0.18f : 0.1f;

            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = CreateFadeGradient(start, end);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = hitDamageable ? GetHitMaterial() : GetSparkMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            particles.Play();
        }

        private static ParticleSystem.MinMaxGradient CreateFadeGradient(Color start, Color end)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(end, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(start.a, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static Material GetFlashMaterial()
        {
            if (flashMaterial == null)
            {
                flashMaterial = CreateParticleMaterial("M_Runtime_MuzzleFlash", new Color(1f, 0.62f, 0.08f, 1f), true);
            }

            return flashMaterial;
        }

        private static Material GetSparkMaterial()
        {
            if (sparkMaterial == null)
            {
                sparkMaterial = CreateParticleMaterial("M_Runtime_ImpactSpark", new Color(1f, 0.58f, 0.08f, 1f), true);
            }

            return sparkMaterial;
        }

        private static Material GetHitMaterial()
        {
            if (hitMaterial == null)
            {
                hitMaterial = CreateParticleMaterial("M_Runtime_HitImpact", new Color(0.78f, 0.06f, 0.03f, 1f), false);
            }

            return hitMaterial;
        }

        private static Material CreateParticleMaterial(string name, Color color, bool additive)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Unlit/Transparent");
            Material material = new Material(shader)
            {
                name = name,
                color = color,
                hideFlags = HideFlags.HideAndDontSave
            };

            Texture2D texture = GetParticleTexture();
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", additive ? 1f : 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", additive
                    ? (float)UnityEngine.Rendering.BlendMode.One
                    : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return material;
        }

        private static Texture2D GetParticleTexture()
        {
            if (particleTexture != null)
            {
                return particleTexture;
            }

            particleTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            {
                name = "T_Runtime_CombatVfxParticle",
                hideFlags = HideFlags.HideAndDontSave
            };

            for (int y = 0; y < particleTexture.height; y++)
            {
                for (int x = 0; x < particleTexture.width; x++)
                {
                    Vector2 uv = new Vector2(
                        (x + 0.5f) / particleTexture.width * 2f - 1f,
                        (y + 0.5f) / particleTexture.height * 2f - 1f);
                    float alpha = Mathf.Clamp01(1f - uv.magnitude);
                    particleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            particleTexture.Apply();
            return particleTexture;
        }
    }
}
