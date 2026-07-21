using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DustlineArena.Runtime.Pickups
{
    [DisallowMultipleComponent]
    public sealed class PickupFeedback : MonoBehaviour
    {
        private const int RingSegments = 64;

        private static Material ringMaterial;
        private static TMP_FontAsset runtimeFont;

        [SerializeField] private string label = "PICKUP";
        [SerializeField] private Color color = new Color(1f, 0.72f, 0.16f, 1f);
        [SerializeField, Min(0.2f)] private float ringRadius = 0.9f;
        [SerializeField, Min(0.2f)] private float labelHeight = 1.45f;

        private LineRenderer ring;
        private Canvas labelCanvas;
        private TMP_Text labelText;
        private UnityEngine.Camera targetCamera;

        public static PickupFeedback Ensure(GameObject owner, string displayLabel, Color highlightColor)
        {
            if (owner == null)
            {
                return null;
            }

            PickupFeedback feedback = owner.GetComponent<PickupFeedback>();
            if (feedback == null)
            {
                feedback = owner.AddComponent<PickupFeedback>();
            }

            feedback.Configure(displayLabel, highlightColor);
            return feedback;
        }

        public static void Remove(GameObject owner)
        {
            if (owner == null)
            {
                return;
            }

            PickupFeedback feedback = owner.GetComponent<PickupFeedback>();
            if (feedback == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(feedback);
            }
            else
            {
                DestroyImmediate(feedback);
            }
        }

        public static void ShowPopup(Vector3 worldPosition, string text, Color popupColor)
        {
            GameObject popupObject = new GameObject("Pickup_Popup", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            popupObject.transform.position = worldPosition + Vector3.up * 1.55f;

            Canvas canvas = popupObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 95;

            RectTransform rect = popupObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260f, 46f);
            rect.localScale = Vector3.one * 0.012f;

            CanvasGroup group = popupObject.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            TMP_Text popupText = CreateText("Value", rect, text, 24f, popupColor, TextAlignmentOptions.Center);
            popupText.fontStyle = FontStyles.Bold;
            popupText.outlineColor = new Color(0f, 0f, 0f, 0.85f);
            popupText.outlineWidth = 0.18f;

            PickupPopupBillboard billboard = popupObject.AddComponent<PickupPopupBillboard>();
            billboard.Initialize(group, rect);
        }

        private void Awake()
        {
            Build();
            ApplyVisuals();
        }

        private void OnEnable()
        {
            Build();
            ApplyVisuals();
        }

        private void Update()
        {
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
            }

            if (targetCamera != null && labelCanvas != null)
            {
                labelCanvas.transform.rotation = targetCamera.transform.rotation;
            }

            if (ring != null)
            {
                float pulse = 0.5f + Mathf.Sin(Time.time * 4.2f) * 0.5f;
                Color ringColor = color;
                ringColor.a = Mathf.Lerp(0.22f, 0.76f, pulse);
                ring.startColor = ringColor;
                ring.endColor = ringColor;
                ring.widthMultiplier = Mathf.Lerp(0.035f, 0.065f, pulse);
            }
        }

        public void Configure(string displayLabel, Color highlightColor)
        {
            label = string.IsNullOrWhiteSpace(displayLabel) ? "PICKUP" : displayLabel;
            color = highlightColor;
            Build();
            ApplyVisuals();
        }

        private void Build()
        {
            if (ring == null)
            {
                GameObject ringObject = new GameObject("Pickup_Readability_Ring");
                ringObject.transform.SetParent(transform, false);
                ringObject.transform.localPosition = Vector3.up * 0.035f;

                ring = ringObject.AddComponent<LineRenderer>();
                ring.useWorldSpace = false;
                ring.loop = true;
                ring.positionCount = RingSegments;
                ring.numCornerVertices = 3;
                ring.numCapVertices = 3;
                ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ring.receiveShadows = false;
                ring.sharedMaterial = GetRingMaterial();

                for (int i = 0; i < RingSegments; i++)
                {
                    float angle = i / (float)RingSegments * Mathf.PI * 2f;
                    ring.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius);
                }
            }

            if (labelCanvas == null)
            {
                GameObject labelObject = new GameObject("Pickup_Label", typeof(RectTransform), typeof(Canvas));
                labelObject.transform.SetParent(transform, false);
                labelObject.transform.localPosition = Vector3.up * labelHeight;

                labelCanvas = labelObject.GetComponent<Canvas>();
                labelCanvas.renderMode = RenderMode.WorldSpace;
                labelCanvas.sortingOrder = 70;

                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.sizeDelta = new Vector2(220f, 38f);
                labelRect.localScale = Vector3.one * 0.01f;

                labelText = CreateText("Text", labelRect, label, 18f, color, TextAlignmentOptions.Center);
                labelText.fontStyle = FontStyles.Bold;
                labelText.outlineColor = new Color(0f, 0f, 0f, 0.82f);
                labelText.outlineWidth = 0.15f;
            }
        }

        private void ApplyVisuals()
        {
            if (labelText != null)
            {
                labelText.text = label;
                labelText.color = color;
            }

            if (ring != null)
            {
                Color ringColor = color;
                ringColor.a = 0.55f;
                ring.startColor = ringColor;
                ring.endColor = ringColor;
            }
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, float size, Color textColor, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = GetRuntimeFont();
            text.text = value;
            text.fontSize = size;
            text.color = textColor;
            text.alignment = alignment;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Material GetRingMaterial()
        {
            if (ringMaterial != null)
            {
                return ringMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color");
            ringMaterial = new Material(shader)
            {
                name = "M_Runtime_PickupRing",
                color = Color.white,
                hideFlags = HideFlags.HideAndDontSave
            };
            return ringMaterial;
        }

        private static TMP_FontAsset GetRuntimeFont()
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            if (runtimeFont != null)
            {
                return runtimeFont;
            }

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtinFont != null)
            {
                runtimeFont = TMP_FontAsset.CreateFontAsset(builtinFont);
                runtimeFont.name = "Pickup_Runtime_Font";
                runtimeFont.hideFlags = HideFlags.HideAndDontSave;
            }

            return runtimeFont;
        }

        private void OnDestroy()
        {
            if (ring != null)
            {
                Destroy(ring.gameObject);
            }

            if (labelCanvas != null)
            {
                Destroy(labelCanvas.gameObject);
            }
        }
    }

    public sealed class PickupPopupBillboard : MonoBehaviour
    {
        private CanvasGroup group;
        private RectTransform rect;
        private UnityEngine.Camera targetCamera;

        public void Initialize(CanvasGroup popupGroup, RectTransform popupRect)
        {
            group = popupGroup;
            rect = popupRect;

            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(this);
            sequence.Append(rect.DOScale(Vector3.one * 0.0145f, 0.12f).SetEase(Ease.OutBack));
            sequence.Join(transform.DOMove(transform.position + Vector3.up * 0.45f, 0.7f).SetEase(Ease.OutCubic));
            sequence.Insert(0.34f, DOTween.To(() => group.alpha, value => group.alpha = value, 0f, 0.34f).SetEase(Ease.InQuad));
            sequence.OnComplete(() =>
            {
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            });
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
            }

            if (targetCamera != null)
            {
                transform.rotation = targetCamera.transform.rotation;
            }
        }
    }
}
