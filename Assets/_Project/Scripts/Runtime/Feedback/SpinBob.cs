using UnityEngine;

namespace DustlineArena.Runtime.Feedback
{
    public sealed class SpinBob : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 90f, 0f);
        [SerializeField, Min(0f)] private float bobAmplitude = 0.15f;
        [SerializeField, Min(0f)] private float bobFrequency = 1.5f;

        private Vector3 startPosition;

        private void Awake()
        {
            startPosition = transform.localPosition;
        }

        private void Update()
        {
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);

            if (bobAmplitude <= 0f || bobFrequency <= 0f)
            {
                return;
            }

            float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            transform.localPosition = startPosition + Vector3.up * bob;
        }
    }
}
