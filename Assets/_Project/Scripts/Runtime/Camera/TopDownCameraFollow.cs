using UnityEngine;

namespace DustlineArena.Runtime.Camera
{
    [DisallowMultipleComponent]
    public sealed class TopDownCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 18f, -12f);
        [SerializeField, Min(0f)] private float followSharpness = 12f;
        [SerializeField, Min(0f)] private float rotationSharpness = 18f;
        [SerializeField] private bool lookAtTarget = true;
        [SerializeField] private bool lockTargetHeight = true;
        [SerializeField, Min(0f)] private float shakeDecaySharpness = 18f;

        private float targetHeight;
        private float shakeStrength;
        private Vector2 shakeVelocity;
        private bool hasTargetHeight;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 focus = target.position;
            if (lockTargetHeight)
            {
                if (!hasTargetHeight)
                {
                    targetHeight = focus.y;
                    hasTargetHeight = true;
                }

                focus.y = targetHeight;
            }

            Vector3 desiredPosition = focus + offset;
            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, t);

            if (lookAtTarget)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
                float rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);
            }

            ApplyShake();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            hasTargetHeight = false;
        }

        public void AddShake(float strength)
        {
            if (strength <= 0f)
            {
                return;
            }

            shakeStrength = Mathf.Max(shakeStrength, strength);
            shakeVelocity += UnityEngine.Random.insideUnitCircle.normalized * strength;
        }

        private void ApplyShake()
        {
            if (shakeStrength <= 0.001f)
            {
                shakeStrength = 0f;
                shakeVelocity = Vector2.zero;
                return;
            }

            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * shakeStrength + shakeVelocity;
            transform.position += transform.right * randomOffset.x + transform.up * randomOffset.y;

            float decay = 1f - Mathf.Exp(-shakeDecaySharpness * Time.deltaTime);
            shakeStrength = Mathf.Lerp(shakeStrength, 0f, decay);
            shakeVelocity = Vector2.Lerp(shakeVelocity, Vector2.zero, decay);
        }
    }
}
