using System;
using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Health;
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

        private Rigidbody body;
        private NavMeshAgent agent;
        private TeamMember teamMember;
        private HealthComponent targetHealth;
        private EnemyConfig configuredConfig;
        private Vector3 lastDestination;
        private float nextDestinationRefreshTime;
        private float nextAttackTime;

        public event Action Attacked;
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
            MoveOnNavMesh();
            FaceTarget();
            TryAttack();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            targetHealth = target == null ? null : target.GetComponentInParent<HealthComponent>();
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

        private void MoveOnNavMesh()
        {
            if (!TryEnsureAgentOnNavMesh())
            {
                StopMoving();
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude <= config.AttackRange * 0.85f)
            {
                StopMoving();
                return;
            }

            agent.isStopped = false;
            Vector3 destination = target.position;
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
                target = null;
                targetHealth = null;
                return;
            }

            HealthComponent health = player.GetComponentInParent<HealthComponent>();
            if (health != null && !health.IsAlive)
            {
                target = null;
                targetHealth = null;
                return;
            }

            target = player.transform;
            targetHealth = health;
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

            return targetHealth == null || targetHealth.IsAlive;
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
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }

        private void TryAttack()
        {
            if (Time.time < nextAttackTime)
            {
                return;
            }

            float sqrAttackRange = config.AttackRange * config.AttackRange;
            if ((target.position - transform.position).sqrMagnitude > sqrAttackRange)
            {
                return;
            }

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
            {
                return;
            }

            nextAttackTime = Time.time + config.AttackCooldown;
            Vector3 direction = (target.position - transform.position).normalized;
            damageable.TakeDamage(new DamageInfo(config.AttackDamage, gameObject, teamMember.Team, target.position, direction));
            Attacked?.Invoke();
        }
    }
}
