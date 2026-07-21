using DustlineArena.Runtime.Health;
using UnityEngine;
using UnityEngine.UI;

namespace DustlineArena.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);

        private Canvas canvas;
        private CanvasGroup group;
        private Image fill;
        private UnityEngine.Camera targetCamera;

        private void Awake()
        {
            health = health == null ? GetComponentInParent<HealthComponent>() : health;
            BuildBar();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Changed += OnHealthChanged;
                health.Died += OnDied;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Changed -= OnHealthChanged;
                health.Died -= OnDied;
            }
        }

        private void LateUpdate()
        {
            if (canvas == null)
            {
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
            }

            if (targetCamera != null)
            {
                canvas.transform.rotation = targetCamera.transform.rotation;
            }
        }

        private void BuildBar()
        {
            if (canvas != null)
            {
                return;
            }

            GameObject barObject = new GameObject(
                "Enemy_Health_Bar",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup));
            RectTransform barRect = barObject.GetComponent<RectTransform>();
            barRect.SetParent(transform, false);
            barRect.localPosition = worldOffset;
            barRect.localRotation = Quaternion.identity;
            barRect.localScale = Vector3.one * 0.01f;
            barRect.sizeDelta = new Vector2(128f, 14f);

            canvas = barObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 30;
            group = barObject.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            RectTransform background = DustlineUiTheme.CreateRect(
                "Background", barRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-4f, -4f), Vector2.zero);
            DustlineUiTheme.AddImage(background, "PanelDark", DustlineUiTheme.Background, true);

            RectTransform fillRect = DustlineUiTheme.CreateRect(
                "Health", background, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-6f, -6f), Vector2.zero);
            fill = DustlineUiTheme.AddImage(fillRect, "HealthFill", Color.white, false);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        private void OnHealthChanged(HealthChangedArgs args)
        {
            SetValue(args.Normalized, args.Current < args.Max && args.Current > 0f);
        }

        private void OnDied()
        {
            SetValue(0f, false);
        }

        private void Refresh()
        {
            if (health == null)
            {
                SetValue(0f, false);
                return;
            }

            float normalized = health.Max <= 0f ? 0f : health.Current / health.Max;
            SetValue(normalized, health.IsAlive && health.Current < health.Max);
        }

        private void SetValue(float normalized, bool visible)
        {
            if (fill != null)
            {
                fill.fillAmount = Mathf.Clamp01(normalized);
                fill.color = normalized <= 0.25f ? DustlineUiTheme.Danger : Color.white;
            }

            if (group != null)
            {
                group.alpha = visible ? 1f : 0f;
            }
        }
    }
}
