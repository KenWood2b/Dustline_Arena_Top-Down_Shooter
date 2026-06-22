using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Health;
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

            Vector3 desiredVelocity = new Vector3(inputReader.Move.x, 0f, inputReader.Move.y) * config.MoveSpeed;
            Vector3 velocity = Vector3.MoveTowards(body.velocity, desiredVelocity, config.Acceleration * Time.fixedDeltaTime);
            body.velocity = new Vector3(velocity.x, 0f, velocity.z);

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
