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

        private float targetHeight;
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
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            hasTargetHeight = false;
        }
    }
}
