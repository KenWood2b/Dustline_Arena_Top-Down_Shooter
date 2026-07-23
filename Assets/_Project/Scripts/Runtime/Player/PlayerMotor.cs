using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Health;
using System;
using UnityEngine;

namespace DustlineArena.Runtime.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private HealthComponent health;

        private Rigidbody body;
        private CapsuleCollider capsuleCollider;
        private float fixedHeight;
        private bool dodgeQueued;
        private bool isDodging;
        private float dodgeEndTime;
        private float nextDodgeTime;
        private Vector3 dodgeDirection;

        public event Action<Vector3> Dodged;
        public bool IsDodging => isDodging;
        public float DodgeCooldownProgress01
        {
            get
            {
                if (config == null || config.DodgeCooldown <= 0f)
                {
                    return 1f;
                }

                return 1f - Mathf.Clamp01((nextDodgeTime - Time.time) / config.DodgeCooldown);
            }
        }

        public bool IsDodgeReady => DodgeCooldownProgress01 >= 1f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.useGravity = false;
            fixedHeight = transform.position.y;
            capsuleCollider = GetComponent<CapsuleCollider>();

            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += FreezeDeadBody;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= FreezeDeadBody;
            }
        }

        private void Update()
        {
            if (inputReader != null && inputReader.DodgePressed)
            {
                dodgeQueued = true;
            }
        }

        private void FixedUpdate()
        {
            if (config == null || inputReader == null)
            {
                return;
            }

            if (health != null && !health.IsAlive)
            {
                FreezeDeadBody();
                return;
            }

            if (dodgeQueued)
            {
                TryBeginDodge();
                dodgeQueued = false;
            }

            if (isDodging)
            {
                ApplyDodgeVelocity();
                return;
            }

            Vector3 desiredVelocity = new Vector3(inputReader.Move.x, 0f, inputReader.Move.y) * config.MoveSpeed;
            Vector3 velocity = Vector3.MoveTowards(body.velocity, desiredVelocity, config.Acceleration * Time.fixedDeltaTime);
            body.velocity = new Vector3(velocity.x, 0f, velocity.z);

            KeepFixedHeight();
        }

        private void TryBeginDodge()
        {
            if (Time.time < nextDodgeTime)
            {
                return;
            }

            Vector3 direction = new Vector3(inputReader.Move.x, 0f, inputReader.Move.y);
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = transform.forward;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            dodgeDirection = direction.normalized;
            isDodging = true;
            dodgeEndTime = Time.time + config.DodgeDuration;
            nextDodgeTime = Time.time + config.DodgeCooldown;
            health?.SetInvulnerable(config.DodgeInvulnerabilityDuration);
            Dodged?.Invoke(dodgeDirection);
        }

        private void ApplyDodgeVelocity()
        {
            body.velocity = dodgeDirection * config.DodgeSpeed;
            KeepFixedHeight();

            if (Time.time < dodgeEndTime)
            {
                return;
            }

            isDodging = false;
            body.velocity = Vector3.zero;
        }

        private void KeepFixedHeight()
        {
            Vector3 position = body.position;
            if (!Mathf.Approximately(position.y, fixedHeight))
            {
                body.position = new Vector3(position.x, fixedHeight, position.z);
            }
        }

        private void FreezeDeadBody()
        {
            if (body == null)
            {
                return;
            }

            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;

            if (capsuleCollider != null)
            {
                capsuleCollider.enabled = false;
            }
        }
    }
}
