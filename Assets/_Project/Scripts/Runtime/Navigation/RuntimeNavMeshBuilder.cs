using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace DustlineArena.Runtime.Navigation
{
    [DisallowMultipleComponent]
    public sealed class RuntimeNavMeshBuilder : MonoBehaviour
    {
        [SerializeField] private Transform sourceRoot;
        [SerializeField] private Vector3 buildSize = new Vector3(70f, 12f, 70f);
        [SerializeField, Min(0.05f)] private float agentRadius = 0.42f;
        [SerializeField, Min(0.1f)] private float agentHeight = 2f;
        [SerializeField, Min(0f)] private float agentClimb = 0.35f;
        [SerializeField, Range(0f, 60f)] private float agentMaxSlope = 45f;
        [SerializeField, Min(0.05f)] private float minimumWalkableTopSize = 12f;
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private bool buildAsynchronously = true;

        private readonly List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>(128);
        private NavMeshData navMeshData;
        private NavMeshDataInstance navMeshInstance;
        private Coroutine buildRoutine;

        public bool IsBuilding { get; private set; }
        public bool HasBuilt { get; private set; }

        private void Awake()
        {
            if (!buildOnAwake)
            {
                return;
            }

            if (buildAsynchronously)
            {
                BuildAsync();
                return;
            }

            Build();
        }

        private void OnDestroy()
        {
            if (navMeshInstance.valid)
            {
                navMeshInstance.Remove();
            }
        }

        public void Configure(Transform root, Vector3 size)
        {
            sourceRoot = root;
            buildSize = size;
        }

        public void Build()
        {
            if (buildRoutine != null)
            {
                StopCoroutine(buildRoutine);
                buildRoutine = null;
            }

            IsBuilding = false;
            PrepareBuild(out NavMeshBuildSettings settings, out Bounds bounds);

            if (!NavMeshBuilder.UpdateNavMeshData(navMeshData, settings, sources, bounds))
            {
                Debug.LogWarning("Dustline Arena: failed to build runtime NavMesh.", this);
                return;
            }

            AddNavMeshDataIfNeeded();
            HasBuilt = true;
        }

        public void BuildAsync()
        {
            if (buildRoutine != null)
            {
                StopCoroutine(buildRoutine);
            }

            buildRoutine = StartCoroutine(BuildAsyncRoutine());
        }

        private IEnumerator BuildAsyncRoutine()
        {
            IsBuilding = true;
            PrepareBuild(out NavMeshBuildSettings settings, out Bounds bounds);
            AsyncOperation operation = NavMeshBuilder.UpdateNavMeshDataAsync(navMeshData, settings, sources, bounds);
            if (operation == null)
            {
                if (!NavMeshBuilder.UpdateNavMeshData(navMeshData, settings, sources, bounds))
                {
                    Debug.LogWarning("Dustline Arena: failed to build runtime NavMesh.", this);
                    IsBuilding = false;
                    buildRoutine = null;
                    yield break;
                }

                AddNavMeshDataIfNeeded();
                HasBuilt = true;
                IsBuilding = false;
                buildRoutine = null;
                yield break;
            }

            yield return operation;

            AddNavMeshDataIfNeeded();
            HasBuilt = true;
            IsBuilding = false;
            buildRoutine = null;
        }

        private void PrepareBuild(out NavMeshBuildSettings settings, out Bounds bounds)
        {
            bounds = new Bounds(transform.position, buildSize);
            sources.Clear();
            CollectColliderSources(sourceRoot == null ? transform : sourceRoot, bounds, minimumWalkableTopSize, sources);

            settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = agentRadius;
            settings.agentHeight = agentHeight;
            settings.agentClimb = agentClimb;
            settings.agentSlope = agentMaxSlope;

            if (navMeshData != null)
            {
                return;
            }

            navMeshData = new NavMeshData(settings.agentTypeID);
            navMeshData.position = transform.position;
        }

        private void AddNavMeshDataIfNeeded()
        {
            if (!navMeshInstance.valid)
            {
                navMeshInstance = NavMesh.AddNavMeshData(navMeshData);
            }
        }

        private static void CollectColliderSources(Transform root, Bounds bounds, float minimumWalkableTopSize, List<NavMeshBuildSource> results)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                if (collider == null || !collider.enabled || collider.isTrigger || !bounds.Intersects(collider.bounds))
                {
                    continue;
                }

                int area = IsWalkableSource(collider, minimumWalkableTopSize)
                    ? 0
                    : NavMesh.GetAreaFromName("Not Walkable");
                if (area < 0)
                {
                    area = IsWalkableSource(collider, minimumWalkableTopSize) ? 0 : 1;
                }

                if (collider is BoxCollider box)
                {
                    results.Add(CreateBoxSource(box, area));
                }
                else if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
                {
                    results.Add(CreateMeshSource(meshCollider, area));
                }
            }
        }

        private static bool IsWalkableSource(Collider collider, float minimumWalkableTopSize)
        {
            if (collider.gameObject.name.IndexOf("Ground", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            Bounds bounds = collider.bounds;
            return Mathf.Max(bounds.size.x, bounds.size.z) >= minimumWalkableTopSize && bounds.size.y <= 0.75f;
        }

        private static NavMeshBuildSource CreateBoxSource(BoxCollider box, int area)
        {
            Transform boxTransform = box.transform;
            return new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                area = area,
                size = box.size,
                transform = Matrix4x4.TRS(
                    boxTransform.TransformPoint(box.center),
                    boxTransform.rotation,
                    boxTransform.lossyScale)
            };
        }

        private static NavMeshBuildSource CreateMeshSource(MeshCollider meshCollider, int area)
        {
            return new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                area = area,
                sourceObject = meshCollider.sharedMesh,
                transform = meshCollider.transform.localToWorldMatrix
            };
        }
    }
}
