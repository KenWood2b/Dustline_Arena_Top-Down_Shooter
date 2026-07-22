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
        public bool GrenadeHeld { get; private set; }
        public bool GrenadePressed { get; private set; }
        public bool GrenadeReleased { get; private set; }

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || health != null && !health.IsAlive)
            {
                ClearInput();
                return;
            }

            Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Move = Vector2.ClampMagnitude(Move, 1f);

            FireHeld = Input.GetMouseButton(0);
            FirePressed = Input.GetMouseButtonDown(0);
            FireReleased = Input.GetMouseButtonUp(0);
            ReloadPressed = Input.GetKeyDown(KeyCode.R);
            GrenadeHeld = Input.GetKey(KeyCode.G) || Input.GetMouseButton(1);
            GrenadePressed = Input.GetKeyDown(KeyCode.G) || Input.GetMouseButtonDown(1);
            GrenadeReleased = !GrenadeHeld && (Input.GetKeyUp(KeyCode.G) || Input.GetMouseButtonUp(1));
        }

        private void ClearInput()
        {
            Move = Vector2.zero;
            FireHeld = false;
            FirePressed = false;
            FireReleased = false;
            ReloadPressed = false;
            GrenadeHeld = false;
            GrenadePressed = false;
            GrenadeReleased = false;
        }
    }
}
