using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Health;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DustlineArena.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class DamagePopupSystem : MonoBehaviour
    {
        private const float PopupDuration = 0.78f;

        private static DamagePopupSystem instance;

        private RectTransform popupLayer;
        private UnityEngine.Camera targetCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }

            GameObject systemObject = new GameObject("Damage_Popup_System", typeof(DamagePopupSystem));
            DontDestroyOnLoad(systemObject);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            BuildCanvas();
        }

        private void OnEnable()
        {
            HealthComponent.AnyDamaged -= OnAnyDamaged;
            HealthComponent.AnyDamaged += OnAnyDamaged;
        }

        private void OnDisable()
        {
            HealthComponent.AnyDamaged -= OnAnyDamaged;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void BuildCanvas()
        {
            if (popupLayer != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject(
                "Damage_Popups",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 180;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            popupLayer = DustlineUiTheme.CreateRect(
                "Popup_Layer",
                canvasRect,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
        }

        private void OnAnyDamaged(HealthComponent target, DamageInfo damage)
        {
            if (target == null || popupLayer == null || damage.Amount <= 0f)
            {
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            Vector3 worldPosition = GetPopupWorldPosition(target, damage);
            Vector3 screenPosition = targetCamera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z <= 0f)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(popupLayer, screenPosition, null, out Vector2 anchoredPosition))
            {
                return;
            }

            TeamId targetTeam = GetTargetTeam(target);
            SpawnPopup(targetTeam, Mathf.CeilToInt(damage.Amount), anchoredPosition);
        }

        private static Vector3 GetPopupWorldPosition(HealthComponent target, DamageInfo damage)
        {
            Vector3 position = damage.Point;
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) || position == Vector3.zero)
            {
                Collider targetCollider = target.GetComponentInChildren<Collider>();
                if (targetCollider != null)
                {
                    Bounds bounds = targetCollider.bounds;
                    return bounds.center + Vector3.up * Mathf.Max(0.35f, bounds.extents.y * 0.45f);
                }

                return target.transform.position + Vector3.up * 1.4f;
            }

            return position + Vector3.up * 0.65f;
        }

        private static TeamId GetTargetTeam(HealthComponent target)
        {
            TeamMember teamMember = target.GetComponentInParent<TeamMember>();
            return teamMember == null ? TeamId.Neutral : teamMember.Team;
        }

        private void SpawnPopup(TeamId targetTeam, int amount, Vector2 anchoredPosition)
        {
            bool isPlayerDamage = targetTeam == TeamId.Player;
            Color color = isPlayerDamage ? new Color(1f, 0.92f, 0.72f, 1f) : DustlineUiTheme.Gold;
            Color outlineColor = isPlayerDamage ? new Color(0.32f, 0.02f, 0.01f, 0.95f) : new Color(0f, 0f, 0f, 0.8f);
            float outlineWidth = isPlayerDamage ? 0.22f : 0.16f;
            float fontSize = isPlayerDamage ? 42f : 32f;

            RectTransform popupRect = DustlineUiTheme.CreateRect(
                isPlayerDamage ? "Player_Damage" : "Enemy_Damage",
                popupLayer,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(140f, 48f),
                anchoredPosition + new Vector2(Random.Range(-18f, 18f), Random.Range(-6f, 12f)));

            TMP_Text text = DustlineUiTheme.AddText(
                popupRect,
                $"-{amount}",
                fontSize,
                color,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            text.raycastTarget = false;
            text.outlineColor = outlineColor;
            text.outlineWidth = outlineWidth;

            CanvasGroup group = popupRect.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 1f;

            Vector2 startPosition = popupRect.anchoredPosition;
            Vector2 endPosition = startPosition + new Vector2(Random.Range(-18f, 18f), isPlayerDamage ? 82f : 64f);
            Vector3 startScale = Vector3.one * 0.62f;
            popupRect.localScale = startScale;

            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(popupRect);

            sequence.Append(DOTween.To(() => popupRect.localScale, value => popupRect.localScale = value, Vector3.one * 1.14f, 0.1f)
                .SetEase(Ease.OutBack));
            sequence.Append(DOTween.To(() => popupRect.localScale, value => popupRect.localScale = value, Vector3.one, 0.08f)
                .SetEase(Ease.OutQuad));
            sequence.Join(DOTween.To(() => popupRect.anchoredPosition, value => popupRect.anchoredPosition = value, endPosition, PopupDuration)
                .SetEase(Ease.OutCubic));
            sequence.Insert(0.34f, DOTween.To(() => group.alpha, value => group.alpha = value, 0f, PopupDuration - 0.34f)
                .SetEase(Ease.InQuad));
            sequence.OnComplete(() =>
            {
                if (popupRect != null)
                {
                    Destroy(popupRect.gameObject);
                }
            });
        }
    }
}
