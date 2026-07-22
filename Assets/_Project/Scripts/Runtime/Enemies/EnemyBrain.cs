using System;
using System.Collections.Generic;
using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Health;
using DustlineArena.Runtime.UI;
using UnityEngine;
using UnityEngine.AI;

namespace DustlineArena.Runtime.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(TeamMember))]
    [DisallowMultipleComponent]
    public sealed class EnemyBrain : MonoBehaviour
    {
        private const float DestinationRefreshInterval = 0.12f;
        private const float DestinationRefreshDistanceSqr = 0.25f;

        [SerializeField] private EnemyConfig config;
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] private float approachScatterRadius = 0.9f;
        [SerializeField, Min(0f)] private float attackMovementLockDuration = 0.9f;
        [SerializeField, Min(0f)] private float attackReachPadding = 0.15f;
        [SerializeField] private bool useAttackSlots = true;
        [SerializeField, Range(4, 16)] private int attackSlotsPerRing = 10;
        [SerializeField, Range(1, 4)] private int attackSlotRings = 3;
        [SerializeField, Min(0.1f)] private float attackSlotRingSpacing = 0.72f;
        [SerializeField, Range(0, 99)] private int avoidancePriorityMin = 35;
        [SerializeField, Range(0, 99)] private int avoidancePriorityMax = 65;

        private Rigidbody body;
        private NavMeshAgent agent;
        private TeamMember teamMember;
        private HealthComponent targetHealth;
        private Collider targetCollider;
        private EnemyConfig configuredConfig;
        private Vector3 lastDestination;
        private float nextDestinationRefreshTime;
        private float nextAttackTime;
        private float attackMovementLockedUntil;
        private float chaseBurstUntil;
        private float nextChaseBurstTime;
        private Vector3 approachOffset;
        private int attackSlotIndex = -1;

        public event Action Attacked;
#if UNITY_EDITOR
        public bool SuppressAttacks { get; set; }
#endif
        public Vector3 PlanarVelocity
        {
            get
            {
                Vector3 velocity = agent != null && agent.enabled ? agent.velocity : body != null ? body.velocity : Vector3.zero;
                velocity.y = 0f;
                return velocity;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            agent = GetComponent<NavMeshAgent>();
            teamMember = GetComponent<TeamMember>();
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.isKinematic = true;
            body.useGravity = false;
            ConfigureAgentIfNeeded();

            if (GetComponent<EnemyHealthBar>() == null)
            {
                gameObject.AddComponent<EnemyHealthBar>();
            }
        }

        private void OnEnable()
        {
            nextDestinationRefreshTime = 0f;
            nextAttackTime = 0f;
            attackMovementLockedUntil = 0f;
            chaseBurstUntil = 0f;
            nextChaseBurstTime = Time.time + UnityEngine.Random.Range(0f, 0.6f);
            lastDestination = new Vector3(float.PositiveInfinity, 0f, float.PositiveInfinity);
            RollApproachOffset();
            RollAvoidancePriority();
#if UNITY_EDITOR
            SuppressAttacks = false;
#endif
        }

        private void OnDisable()
        {
            ReleaseAttackSlot();
        }

        private void OnDestroy()
        {
            ReleaseAttackSlot();
        }

        private void Update()
        {
            if (!HasAliveTarget())
            {
                AcquirePlayerTarget();
            }

            if (config == null || !HasAliveTarget())
            {
                StopMoving();
                return;
            }

            ConfigureAgentIfNeeded();
            if (IsAttackMovementLocked())
            {
                ApplyMovementSpeed(1f);
                StopMoving();
                FaceTarget();
                return;
            }

            MoveOnNavMesh();
            FaceTarget();
            TryAttack();
        }

        public void SetTarget(Transform newTarget)
        {
            if (target != newTarget)
            {
                ReleaseAttackSlot();
            }

            target = newTarget;
            targetHealth = target == null ? null : target.GetComponentInParent<HealthComponent>();
            targetCollider = target == null ? null : target.GetComponentInParent<Collider>();
            RollApproachOffset();
        }

        private void ConfigureAgentIfNeeded()
        {
            if (agent == null || config == null || configuredConfig == config)
            {
                return;
            }

            agent.speed = config.MoveSpeed;
            agent.acceleration = config.Acceleration;
            agent.stoppingDistance = config.AttackRange * 0.85f;
            agent.radius = 0.42f;
            agent.height = 2f;
            agent.baseOffset = 0f;
            agent.angularSpeed = 720f;
            agent.autoBraking = true;
            agent.updateRotation = false;
            agent.updateUpAxis = true;
            int walkableArea = NavMesh.GetAreaFromName("Walkable");
            agent.areaMask = walkableArea >= 0 ? 1 << walkableArea : 1;
            configuredConfig = config;
        }

        private void UpdateChaseBurst(float targetDistance)
        {
            if (agent == null || config == null || !config.UseChaseBurst)
            {
                ApplyMovementSpeed(1f);
                return;
            }

            if (Time.time < chaseBurstUntil)
            {
                ApplyMovementSpeed(config.ChaseBurstSpeedMultiplier);
                return;
            }

            ApplyMovementSpeed(1f);
            if (targetDistance < config.ChaseBurstMinDistance || Time.time < nextChaseBurstTime)
            {
                return;
            }

            chaseBurstUntil = Time.time + config.ChaseBurstDuration;
            nextChaseBurstTime = chaseBurstUntil
                + config.ChaseBurstCooldown
                + UnityEngine.Random.Range(0f, config.ChaseBurstCooldown * 0.25f);
            ApplyMovementSpeed(config.ChaseBurstSpeedMultiplier);
        }

        private void ApplyMovementSpeed(float multiplier)
        {
            if (agent == null || config == null)
            {
                return;
            }

            float speedMultiplier = Mathf.Max(1f, multiplier);
            agent.speed = config.MoveSpeed * speedMultiplier;
            agent.acceleration = config.Acceleration * (speedMultiplier > 1f ? 1.25f : 1f);
        }

        private void RollAvoidancePriority()
        {
            if (agent == null)
            {
                return;
            }

            int min = Mathf.Clamp(Mathf.Min(avoidancePriorityMin, avoidancePriorityMax), 0, 99);
            int max = Mathf.Clamp(Mathf.Max(avoidancePriorityMin, avoidancePriorityMax), 0, 99);
            agent.avoidancePriority = UnityEngine.Random.Range(min, max + 1);
        }

        private void MoveOnNavMesh()
        {
            if (!TryEnsureAgentOnNavMesh())
            {
                StopMoving();
                return;
            }

            Vector3 toTarget = GetPlanarAttackOffset();
            float targetDistance = toTarget.magnitude;
            if (targetDistance <= config.AttackRange * 0.85f)
            {
                ApplyMovementSpeed(1f);
                StopMoving();
                return;
            }

            UpdateChaseBurst(targetDistance);
            agent.isStopped = false;
            Vector3 destination = GetApproachDestination();
            if (Time.time < nextDestinationRefreshTime
                && (destination - lastDestination).sqrMagnitude < DestinationRefreshDistanceSqr)
            {
                return;
            }

            if (agent.SetDestination(destination))
            {
                lastDestination = destination;
                nextDestinationRefreshTime = Time.time + DestinationRefreshInterval;
            }
        }

        private bool TryEnsureAgentOnNavMesh()
        {
            if (agent == null || !agent.enabled)
            {
                return false;
            }

            if (agent.isOnNavMesh)
            {
                return true;
            }

            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            {
                return false;
            }

            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        private void AcquirePlayerTarget()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                ReleaseAttackSlot();
                target = null;
                targetHealth = null;
                targetCollider = null;
                return;
            }

            HealthComponent health = player.GetComponentInParent<HealthComponent>();
            if (health != null && !health.IsAlive)
            {
                ReleaseAttackSlot();
                target = null;
                targetHealth = null;
                targetCollider = null;
                return;
            }

            target = player.transform;
            targetHealth = health;
            targetCollider = player.GetComponentInParent<Collider>();
            ReleaseAttackSlot();
            RollApproachOffset();
        }

        private void RollApproachOffset()
        {
            if (approachScatterRadius <= 0f)
            {
                approachOffset = Vector3.zero;
                return;
            }

            Vector2 offset = UnityEngine.Random.insideUnitCircle;
            if (offset.sqrMagnitude < 0.01f)
            {
                offset = Vector2.right;
            }

            offset = offset.normalized * UnityEngine.Random.Range(approachScatterRadius * 0.35f, approachScatterRadius);
            approachOffset = new Vector3(offset.x, 0f, offset.y);
        }

        private bool HasAliveTarget()
        {
            if (target == null)
            {
                return false;
            }

            if (targetHealth == null)
            {
                targetHealth = target.GetComponentInParent<HealthComponent>();
            }

            if (targetCollider == null)
            {
                targetCollider = target.GetComponentInParent<Collider>();
            }

            return targetHealth == null || targetHealth.IsAlive;
        }

        private Vector3 GetApproachDestination()
        {
            if (!useAttackSlots || target == null || config == null)
            {
                return target == null ? transform.position : target.position + approachOffset;
            }

            Vector3 targetPosition = target.position;
            Vector3 preferredDirection = transform.position - targetPosition;
            preferredDirection.y = 0f;
            if (preferredDirection.sqrMagnitude < 0.001f)
            {
                preferredDirection = approachOffset.sqrMagnitude > 0.001f ? approachOffset : Vector3.forward;
            }

            attackSlotIndex = AttackSlotCoordinator.Reserve(
                target,
                this,
                preferredDirection,
                attackSlotsPerRing,
                attackSlotRings);

            float ringRadius = GetAttackSlotRadius(attackSlotIndex);
            Vector3 slotDirection = AttackSlotCoordinator.GetSlotDirection(attackSlotIndex, attackSlotsPerRing);
            Vector3 personalOffset = approachOffset.sqrMagnitude > 0.001f ? approachOffset.normalized * 0.08f : Vector3.zero;
            return targetPosition + slotDirection * ringRadius + personalOffset;
        }

        private float GetAttackSlotRadius(int slotIndex)
        {
            int ring = attackSlotsPerRing <= 0 ? 0 : Mathf.Max(0, slotIndex / attackSlotsPerRing);
            float baseRadius = Mathf.Max(0.65f, config.AttackRange * 0.72f);
            return baseRadius + ring * attackSlotRingSpacing;
        }

        private void ReleaseAttackSlot()
        {
            if (attackSlotIndex < 0)
            {
                return;
            }

            AttackSlotCoordinator.Release(this);
            attackSlotIndex = -1;
        }

        private void StopMoving()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                if (!agent.isStopped || agent.hasPath)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }
            }

            if (body == null || body.isKinematic)
            {
                return;
            }

            body.velocity = new Vector3(0f, body.velocity.y, 0f);
            body.angularVelocity = Vector3.zero;
        }

        private void FaceTarget()
        {
            Vector3 toTarget = GetPlanarTargetOffset();

            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }

        private void TryAttack()
        {
#if UNITY_EDITOR
            if (SuppressAttacks)
            {
                return;
            }
#endif
            if (Time.time < nextAttackTime)
            {
                return;
            }

            float attackRange = config.AttackRange + attackReachPadding;
            float sqrAttackRange = attackRange * attackRange;
            Vector3 toTarget = GetPlanarAttackOffset();
            if (toTarget.sqrMagnitude > sqrAttackRange)
            {
                return;
            }

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
            {
                return;
            }

            nextAttackTime = Time.time + config.AttackCooldown;
            attackMovementLockedUntil = Time.time + Mathf.Max(attackMovementLockDuration, config.AttackCooldown);
            Vector3 direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : transform.forward;
            damageable.TakeDamage(new DamageInfo(config.AttackDamage, gameObject, teamMember.Team, target.position, direction));
            StopMoving();
            Attacked?.Invoke();
        }

        private bool IsAttackMovementLocked()
        {
            return Time.time < attackMovementLockedUntil;
        }

        private Vector3 GetPlanarTargetOffset()
        {
            if (target == null)
            {
                return Vector3.zero;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            return toTarget;
        }

        private Vector3 GetPlanarAttackOffset()
        {
            if (targetCollider == null)
            {
                return GetPlanarTargetOffset();
            }

            Vector3 probe = transform.position;
            probe.y = targetCollider.bounds.center.y;
            Vector3 closest = targetCollider.ClosestPoint(probe);
            Vector3 toTarget = closest - transform.position;
            toTarget.y = 0f;
            return toTarget.sqrMagnitude > 0.0001f ? toTarget : GetPlanarTargetOffset();
        }

        private static class AttackSlotCoordinator
        {
            private static readonly List<Reservation> Reservations = new List<Reservation>(64);

            public static int Reserve(
                Transform target,
                EnemyBrain owner,
                Vector3 preferredDirection,
                int slotsPerRing,
                int ringCount)
            {
                if (target == null || owner == null)
                {
                    return 0;
                }

                slotsPerRing = Mathf.Max(1, slotsPerRing);
                ringCount = Mathf.Max(1, ringCount);
                int maxSlots = slotsPerRing * ringCount;

                for (int i = Reservations.Count - 1; i >= 0; i--)
                {
                    Reservation reservation = Reservations[i];
                    if (reservation.Owner == null || reservation.Target == null)
                    {
                        Reservations.RemoveAt(i);
                        continue;
                    }

                    if (reservation.Owner == owner)
                    {
                        if (reservation.Target == target && reservation.Index < maxSlots)
                        {
                            return reservation.Index;
                        }

                        Reservations.RemoveAt(i);
                    }
                }

                Vector3 preferred = preferredDirection;
                preferred.y = 0f;
                if (preferred.sqrMagnitude < 0.001f)
                {
                    preferred = Vector3.forward;
                }

                preferred.Normalize();

                int bestIndex = 0;
                float bestScore = float.MaxValue;
                for (int index = 0; index < maxSlots; index++)
                {
                    if (IsReserved(target, index))
                    {
                        continue;
                    }

                    Vector3 slotDirection = GetSlotDirection(index, slotsPerRing);
                    float angleScore = Vector3.Angle(preferred, slotDirection) / 180f;
                    int ring = index / slotsPerRing;
                    float ringScore = ring * 0.35f;
                    float score = angleScore + ringScore;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestIndex = index;
                    }
                }

                Reservations.Add(new Reservation(target, owner, bestIndex));
                return bestIndex;
            }

            public static void Release(EnemyBrain owner)
            {
                if (owner == null)
                {
                    return;
                }

                for (int i = Reservations.Count - 1; i >= 0; i--)
                {
                    if (Reservations[i].Owner == owner)
                    {
                        Reservations.RemoveAt(i);
                    }
                }
            }

            public static Vector3 GetSlotDirection(int index, int slotsPerRing)
            {
                slotsPerRing = Mathf.Max(1, slotsPerRing);
                int slot = Mathf.Abs(index) % slotsPerRing;
                float angle = slot / (float)slotsPerRing * Mathf.PI * 2f;
                return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            }

            private static bool IsReserved(Transform target, int index)
            {
                for (int i = Reservations.Count - 1; i >= 0; i--)
                {
                    Reservation reservation = Reservations[i];
                    if (reservation.Owner == null || reservation.Target == null)
                    {
                        Reservations.RemoveAt(i);
                        continue;
                    }

                    if (reservation.Target == target && reservation.Index == index)
                    {
                        return true;
                    }
                }

                return false;
            }

            private readonly struct Reservation
            {
                public readonly Transform Target;
                public readonly EnemyBrain Owner;
                public readonly int Index;

                public Reservation(Transform target, EnemyBrain owner, int index)
                {
                    Target = target;
                    Owner = owner;
                    Index = index;
                }
            }
        }
    }
}
