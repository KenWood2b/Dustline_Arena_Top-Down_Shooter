using DustlineArena.Runtime.Config;
using UnityEngine;

namespace DustlineArena.Runtime.Weapons
{
    public sealed class ShellCasingFx : MonoBehaviour
    {
        private const float Lifetime = 4.5f;
        private const float SettleTime = 0.52f;
        private const float GroundProbeUp = 1.1f;
        private const float GroundProbeDistance = 8f;
        private const string PistolCasingResource = "FX/ShellCasings/PistolShellCasing";
        private const string RifleCasingResource = "FX/ShellCasings/RifleShellCasing";
        private const string ShotgunCasingResource = "FX/ShellCasings/ShotgunShellCasing";
        private const string RifleMaterialResource = "Materials/ShellCasings/M_Shell_Brass_URP";
        private const string ShotgunMaterialResource = "Materials/ShellCasings/M_ShotgunShell_Red_URP";

        private static Material rifleCasingMaterial;
        private static Material shotgunCasingMaterial;

        private Vector3 planarVelocity;
        private Vector3 spin;
        private float groundY;
        private float startY;
        private float age;
        private Quaternion settledRotation;

        public static void Spawn(Vector3 position, Vector3 fireDirection, WeaponVisualId weaponVisual)
        {
            if (weaponVisual == WeaponVisualId.Grenade)
            {
                return;
            }

            GameObject casing = CreateCasingObject(weaponVisual);

            Transform casingTransform = casing.transform;
            casingTransform.position = position + Vector3.up * 0.32f;
            casingTransform.localScale = GetCasingScale(weaponVisual);
            casingTransform.rotation = Quaternion.Euler(
                Random.Range(-35f, 35f),
                Random.Range(0f, 360f),
                Random.Range(-35f, 35f));
            ApplyCasingMaterial(casing, weaponVisual);

            ShellCasingFx fx = casing.GetComponent<ShellCasingFx>();
            if (fx == null)
            {
                fx = casing.AddComponent<ShellCasingFx>();
            }

            fx.Initialize(fireDirection);
        }

        private void Initialize(Vector3 fireDirection)
        {
            fireDirection.y = 0f;
            if (fireDirection.sqrMagnitude < 0.0001f)
            {
                fireDirection = transform.forward;
            }

            fireDirection.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, fireDirection).normalized;
            if (Random.value < 0.5f)
            {
                side = -side;
            }

            planarVelocity = side * Random.Range(1.65f, 2.45f) - fireDirection * Random.Range(0.15f, 0.45f);
            spin = new Vector3(
                Random.Range(360f, 720f),
                Random.Range(260f, 620f),
                Random.Range(360f, 720f));
            startY = transform.position.y;
            groundY = FindGroundY(transform.position);
            settledRotation = Quaternion.Euler(
                Random.Range(78f, 102f),
                Random.Range(0f, 360f),
                Random.Range(-16f, 16f));
            age = 0f;
        }

        private void Update()
        {
            age += Time.deltaTime;

            Vector3 position = transform.position;
            position += planarVelocity * Time.deltaTime;
            planarVelocity = Vector3.Lerp(planarVelocity, Vector3.zero, Time.deltaTime * 3.6f);

            float t = Mathf.Clamp01(age / SettleTime);
            float arc = Mathf.Sin(t * Mathf.PI) * 0.28f;
            position.y = Mathf.Lerp(startY, groundY, t) + arc;
            if (age >= SettleTime)
            {
                position.y = groundY;
                transform.rotation = Quaternion.Slerp(transform.rotation, settledRotation, Time.deltaTime * 9f);
            }
            else
            {
                transform.Rotate(spin * Time.deltaTime, Space.Self);
            }

            transform.position = position;

            if (age >= Lifetime)
            {
                Destroy(gameObject);
            }
        }

        private static GameObject CreateCasingObject(WeaponVisualId weaponVisual)
        {
            string resourcePath = GetCasingResourcePath(weaponVisual);
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null)
            {
                GameObject casing = Instantiate(prefab);
                casing.name = GetCasingObjectName(weaponVisual, false);
                return casing;
            }

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = GetCasingObjectName(weaponVisual, true);
            Collider fallbackCollider = fallback.GetComponent<Collider>();
            if (fallbackCollider != null)
            {
                Destroy(fallbackCollider);
            }

            return fallback;
        }

        private static string GetCasingResourcePath(WeaponVisualId weaponVisual)
        {
            switch (weaponVisual)
            {
                case WeaponVisualId.Pistol:
                    return PistolCasingResource;
                case WeaponVisualId.Shotgun:
                    return ShotgunCasingResource;
                case WeaponVisualId.AK:
                case WeaponVisualId.SMG:
                default:
                    return RifleCasingResource;
            }
        }

        private static Vector3 GetCasingScale(WeaponVisualId weaponVisual)
        {
            switch (weaponVisual)
            {
                case WeaponVisualId.Shotgun:
                    return Vector3.one * 2.4f;
                case WeaponVisualId.Pistol:
                    return Vector3.one * 2f;
                case WeaponVisualId.AK:
                case WeaponVisualId.SMG:
                default:
                    return Vector3.one * 1.65f;
            }
        }

        private static string GetCasingObjectName(WeaponVisualId weaponVisual, bool fallback)
        {
            string suffix = fallback ? "_Fallback" : "_Fx";
            switch (weaponVisual)
            {
                case WeaponVisualId.Pistol:
                    return "Pistol_Casing" + suffix;
                case WeaponVisualId.Shotgun:
                    return "Shotgun_Shell" + suffix;
                case WeaponVisualId.AK:
                case WeaponVisualId.SMG:
                default:
                    return "Rifle_Casing" + suffix;
            }
        }

        private static void ApplyCasingMaterial(GameObject casing, WeaponVisualId weaponVisual)
        {
            if (casing == null)
            {
                return;
            }

            Material material = weaponVisual == WeaponVisualId.Shotgun
                ? GetOrCreateCasingMaterial(ref shotgunCasingMaterial, ShotgunMaterialResource, "M_Runtime_ShotgunShell", new Color(0.72f, 0.08f, 0.035f, 1f))
                : GetOrCreateCasingMaterial(ref rifleCasingMaterial, RifleMaterialResource, "M_Runtime_RifleCasing", new Color(0.82f, 0.52f, 0.18f, 1f));

            Renderer[] renderers = casing.GetComponentsInChildren<Renderer>(includeInactive: true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer casingRenderer = renderers[i];
                Material[] materials = casingRenderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    casingRenderer.sharedMaterial = material;
                }
                else
                {
                    for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        materials[materialIndex] = material;
                    }

                    casingRenderer.sharedMaterials = materials;
                }

                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                casingRenderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", material.color);
                properties.SetColor("_Color", material.color);
                properties.SetColor("_Tint", material.color);
                casingRenderer.SetPropertyBlock(properties);
            }
        }

        private static Material GetOrCreateCasingMaterial(ref Material cachedMaterial, string resourcePath, string materialName, Color fallbackColor)
        {
            if (cachedMaterial != null)
            {
                return cachedMaterial;
            }

            cachedMaterial = Resources.Load<Material>(resourcePath);
            if (cachedMaterial != null)
            {
                return cachedMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Unlit/Color");

            cachedMaterial = new Material(shader)
            {
                name = materialName,
                color = fallbackColor,
                hideFlags = HideFlags.HideAndDontSave
            };
            cachedMaterial.SetColor("_BaseColor", fallbackColor);
            cachedMaterial.SetColor("_Color", fallbackColor);
            cachedMaterial.SetColor("_Tint", fallbackColor);
            return cachedMaterial;
        }

        private static float FindGroundY(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * GroundProbeUp;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, GroundProbeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                return 0.03f;
            }

            float lowestY = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.point.y > position.y + 0.1f || hit.point.y >= lowestY)
                {
                    continue;
                }

                lowestY = hit.point.y;
            }

            return float.IsPositiveInfinity(lowestY) ? 0.03f : lowestY + 0.03f;
        }
    }
}
