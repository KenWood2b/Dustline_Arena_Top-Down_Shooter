using UnityEngine;

namespace DustlineArena.Runtime.Weapons
{
    public sealed class FirePointMarker : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(1f, 0.8f, 0.1f, 1f);
        [SerializeField, Min(0.01f)] private float radius = 0.08f;
        [SerializeField, Min(0.01f)] private float directionLength = 0.55f;

        private void OnDrawGizmos()
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position, radius);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * directionLength);
        }
    }
}
