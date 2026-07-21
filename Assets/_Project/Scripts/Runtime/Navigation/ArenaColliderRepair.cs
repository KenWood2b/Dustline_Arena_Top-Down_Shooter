using System;
using UnityEngine;
namespace DustlineArena.Runtime.Navigation
{
    public static class ArenaColliderRepair
    {
        private const string LayoutPrefix = "Props_ArenaSceneLayout_";
        private static readonly string[] BlockingFragments =
        {
            "Container",
            "Trash",
            "Barrier",
            "Sack",
            "Crate",
            "Barrel",
            "Cliff",
            "Rock",
            "Pipe",
            "BoxScatter",
            "CardboardScatter",
            "BoxStack",
            "Planks",
            "Pallet",
            "Wall_"
        };

        private static readonly string[] PassThroughFragments =
        {
            "RoadPatch",
            "RoadWarning",
            "ParkingPatch",
            "RoadCrossing",
            "RoadSmall",
            "Papers",
            "Grass",
            "Flowers",
            "Bush",
            "Fern",
            "Pebble",
            "DirtScuff",
            "DirtPatch",
            "Cone",
            "RoofLight",
            "WallLight"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RepairAfterSceneLoad()
        {
            RepairLoadedArena();
        }

        public static int RepairLoadedArena()
        {
            GameObject arena = GameObject.Find("Arena");
            if (arena == null)
            {
                return 0;
            }

            int repaired = 0;
            foreach (Transform child in arena.transform)
            {
                if (!child.name.StartsWith(LayoutPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                repaired += RepairLayout(child);
            }

            return repaired;
        }

        private static int RepairLayout(Transform layout)
        {
            int repaired = 0;
            foreach (Transform section in layout)
            {
                foreach (Transform placedObject in section)
                {
                    if (ContainsAny(placedObject.name, BlockingFragments))
                    {
                        if (RepairBlockingCollider(placedObject.gameObject))
                        {
                            repaired++;
                        }
                    }
                    else if (ContainsAny(placedObject.name, PassThroughFragments))
                    {
                        DisableSolidColliders(placedObject.gameObject);
                    }
                }
            }

            return repaired;
        }

        private static bool RepairBlockingCollider(GameObject target)
        {
            if (!TryGetLocalRendererBounds(target, out Bounds localBounds))
            {
                return false;
            }

            BoxCollider collider = GetOrCreateRootBoxCollider(target);
            DisableOtherSolidColliders(target, collider);
            collider.enabled = true;
            collider.center = localBounds.center;
            collider.size = localBounds.size;
            return true;
        }

        private static BoxCollider GetOrCreateRootBoxCollider(GameObject target)
        {
            BoxCollider collider = target.GetComponent<BoxCollider>();
            return collider == null ? target.AddComponent<BoxCollider>() : collider;
        }

        private static void DisableOtherSolidColliders(GameObject target, Collider colliderToKeep)
        {
            Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || collider == colliderToKeep || collider.isTrigger)
                {
                    continue;
                }

                collider.enabled = false;
            }
        }

        private static void DisableSolidColliders(GameObject target)
        {
            Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || collider.isTrigger)
                {
                    continue;
                }

                collider.enabled = false;
            }
        }

        private static bool TryGetLocalRendererBounds(GameObject target, out Bounds bounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            bounds = default;
            bool hasBounds = false;
            Matrix4x4 rootWorldToLocal = target.transform.worldToLocalMatrix;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Bounds localRendererBounds = renderer.localBounds;
                Matrix4x4 rendererLocalToRootLocal = rootWorldToLocal * renderer.transform.localToWorldMatrix;
                Encapsulate(rendererLocalToRootLocal.MultiplyPoint3x4(localRendererBounds.min), ref bounds, ref hasBounds);
                Encapsulate(rendererLocalToRootLocal.MultiplyPoint3x4(localRendererBounds.max), ref bounds, ref hasBounds);
                Encapsulate(rendererLocalToRootLocal.MultiplyPoint3x4(new Vector3(localRendererBounds.min.x, localRendererBounds.min.y, localRendererBounds.max.z)), ref bounds, ref hasBounds);
                Encapsulate(rendererLocalToRootLocal.MultiplyPoint3x4(new Vector3(localRendererBounds.min.x, localRendererBounds.max.y, localRendererBounds.min.z)), ref bounds, ref hasBounds);
                Encapsulate(rendererLocalToRootLocal.MultiplyPoint3x4(new Vector3(localRendererBounds.max.x, localRendererBounds.min.y, localRendererBounds.min.z)), ref bounds, ref hasBounds);
                Encapsulate(rendererLocalToRootLocal.MultiplyPoint3x4(new Vector3(localRendererBounds.min.x, localRendererBounds.max.y, localRendererBounds.max.z)), ref bounds, ref hasBounds);
                Encapsulate(rendererLocalToRootLocal.MultiplyPoint3x4(new Vector3(localRendererBounds.max.x, localRendererBounds.min.y, localRendererBounds.max.z)), ref bounds, ref hasBounds);
                Encapsulate(rendererLocalToRootLocal.MultiplyPoint3x4(new Vector3(localRendererBounds.max.x, localRendererBounds.max.y, localRendererBounds.min.z)), ref bounds, ref hasBounds);
            }

            return hasBounds;
        }

        private static void Encapsulate(Vector3 point, ref Bounds bounds, ref bool hasBounds)
        {
            if (!hasBounds)
            {
                bounds = new Bounds(point, Vector3.zero);
                hasBounds = true;
                return;
            }

            bounds.Encapsulate(point);
        }

        private static bool ContainsAny(string value, string[] fragments)
        {
            for (int i = 0; i < fragments.Length; i++)
            {
                if (value.IndexOf(fragments[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
