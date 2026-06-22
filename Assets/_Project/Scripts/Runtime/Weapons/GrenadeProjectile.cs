using System.Collections.Generic;
using DustlineArena.Runtime.Common;
using UnityEngine;

namespace DustlineArena.Runtime.Weapons
{
    [DisallowMultipleComponent]
    public sealed class GrenadeProjectile : MonoBehaviour
    {
        private const int ExplosionHitBufferSize = 64;
        private static Texture2D particleTexture;
        private static Material additiveParticleMaterial;
        private static Material smokeParticleMaterial;

        private DamageInfo damage;
        private Rigidbody body;
        private float fuseTime;
        private float explosionRadius;
        private LayerMask hitMask;
        private bool initialized;
        private bool exploded;
        private readonly Collider[] explosionHitBuffer = new Collider[ExplosionHitBufferSize];
        private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        public void Initialize(
            DamageInfo damageInfo,
            Rigidbody grenadeBody,
            Vector3 initialVelocity,
            float fuse,
            float radius,
            LayerMask explosionHitMask)
        {
            damage = damageInfo;
            body = grenadeBody;
            fuseTime = Mathf.Max(0.1f, fuse);
            explosionRadius = Mathf.Max(0.1f, radius);
            hitMask = explosionHitMask;
            initialized = true;

            body.velocity = initialVelocity;
            body.angularVelocity = Random.onUnitSphere * 10f;
        }

        private void Update()
        {
            if (!initialized || exploded)
            {
                return;
            }

            fuseTime -= Time.deltaTime;
            if (fuseTime <= 0f)
            {
                Explode();
            }
        }

        private void Explode()
        {
            exploded = true;
            Vector3 center = transform.position;
            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                explosionRadius,
                explosionHitBuffer,
                hitMask,
                QueryTriggerInteraction.Collide);
            damagedTargets.Clear();

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = explosionHitBuffer[i];
                TeamMember team = hit.GetComponentInParent<TeamMember>();
                if (team != null && team.Team == damage.SourceTeam)
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive || !damagedTargets.Add(damageable))
                {
                    continue;
                }

                Vector3 targetPoint = hit.ClosestPoint(center);
                float distance = Vector3.Distance(center, targetPoint);
                float falloff = Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(distance / explosionRadius));
                Vector3 direction = (targetPoint - center).normalized;
                damageable.TakeDamage(new DamageInfo(
                    damage.Amount * falloff,
                    damage.Source,
                    damage.SourceTeam,
                    targetPoint,
                    direction));
            }

            CreateExplosionParticles(center, explosionRadius);
            Destroy(gameObject);
        }

        private static void CreateExplosionParticles(Vector3 position, float radius)
        {
            GameObject effect = new GameObject("Grenade_Explosion_FX");
            effect.transform.position = position;

            ParticleSystem flash = effect.AddComponent<ParticleSystem>();
            ConfigureFlash(flash, radius);
            ConfigureSmoke(effect.transform, radius);

            Destroy(effect, 3.5f);
        }

        private static void ConfigureFlash(ParticleSystem particles, float radius)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.duration = 0.25f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 2.2f, radius * 4.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.42f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.28f, 0.02f, 1f),
                new Color(1f, 0.9f, 0.2f, 1f));
            main.maxParticles = 80;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 48)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.18f;

            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.45f), 0f),
                    new GradientColorKey(new Color(1f, 0.15f, 0.01f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetParticleMaterial(true);
            particles.Play();
        }

        private static void ConfigureSmoke(Transform parent, float radius)
        {
            GameObject smokeObject = new GameObject("Smoke");
            smokeObject.transform.SetParent(parent, false);
            ParticleSystem smoke = smokeObject.AddComponent<ParticleSystem>();
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = smoke.main;
            main.loop = false;
            main.duration = 0.3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 0.35f, radius * 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 1.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.22f, 0.2f, 0.18f, 0.75f),
                new Color(0.48f, 0.42f, 0.34f, 0.55f));
            main.gravityModifier = -0.12f;
            main.maxParticles = 36;

            ParticleSystem.EmissionModule emission = smoke.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 22)
            });

            ParticleSystem.ShapeModule shape = smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.28f;

            ParticleSystem.ColorOverLifetimeModule color = smoke.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.42f, 0.36f, 0.3f), 0f),
                    new GradientColorKey(new Color(0.12f, 0.12f, 0.12f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.75f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;

            ParticleSystem.SizeOverLifetimeModule size = smoke.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(
                1f,
                AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1.5f));

            ParticleSystemRenderer renderer = smoke.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetParticleMaterial(false);
            smoke.Play();
        }

        private static Material GetParticleMaterial(bool additive)
        {
            Material cachedMaterial = additive ? additiveParticleMaterial : smokeParticleMaterial;
            if (cachedMaterial != null)
            {
                return cachedMaterial;
            }

            Color color = additive
                ? new Color(1f, 0.42f, 0.04f)
                : new Color(0.35f, 0.31f, 0.27f);
            cachedMaterial = CreateParticleMaterial(color, additive);
            if (additive)
            {
                additiveParticleMaterial = cachedMaterial;
            }
            else
            {
                smokeParticleMaterial = cachedMaterial;
            }

            return cachedMaterial;
        }

        private static Material CreateParticleMaterial(Color color, bool additive)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Unlit/Transparent");
            Material material = new Material(shader)
            {
                color = color
            };

            Texture2D texture = GetParticleTexture();
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (additive && material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 1f);
            }

            return material;
        }

        private static Texture2D GetParticleTexture()
        {
            if (particleTexture != null)
            {
                return particleTexture;
            }

            particleTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
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
