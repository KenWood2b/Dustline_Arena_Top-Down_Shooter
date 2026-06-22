using UnityEngine;
using DustlineArena.Runtime.Health;

namespace DustlineArena.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;

        public Vector2 Move { get; private set; }
        public bool FireHeld { get; private set; }
        public bool FirePressed { get; private set; }
        public bool FireReleased { get; private set; }
        public bool ReloadPressed { get; private set; }

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }
        }

        private void Update()
        {
            if (health != null && !health.IsAlive)
            {
                Move = Vector2.zero;
                FireHeld = false;
                FirePressed = false;
                FireReleased = false;
                ReloadPressed = false;
                return;
            }

            Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Move = Vector2.ClampMagnitude(Move, 1f);

            FireHeld = Input.GetMouseButton(0);
            FirePressed = Input.GetMouseButtonDown(0);
            FireReleased = Input.GetMouseButtonUp(0);
            ReloadPressed = Input.GetKeyDown(KeyCode.R);
        }
    }
}
