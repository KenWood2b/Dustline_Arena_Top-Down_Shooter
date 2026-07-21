using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DustlineArena.Runtime.UI
{
    internal static class DustlineUiTheme
    {
        public static readonly Color Background = new Color(0.035f, 0.043f, 0.055f, 0.94f);
        public static readonly Color BackgroundSoft = new Color(0.07f, 0.08f, 0.10f, 0.92f);
        public static readonly Color Gold = new Color(1f, 0.72f, 0.16f, 1f);
        public static readonly Color Text = new Color(0.94f, 0.95f, 0.93f, 1f);
        public static readonly Color Muted = new Color(0.60f, 0.64f, 0.66f, 1f);
        public static readonly Color Danger = new Color(0.82f, 0.16f, 0.12f, 1f);

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static TMP_FontAsset runtimeFont;

        public static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 size,
            Vector2 position)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
            return rect;
        }

        public static Image AddImage(RectTransform rect, string spriteName, Color color, bool sliced = true)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(spriteName);
            image.color = color;
            image.raycastTarget = false;
            image.type = sliced && image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            return image;
        }

        public static TMP_Text AddText(
            RectTransform rect,
            string value,
            float size,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left,
            FontStyles style = FontStyles.Normal)
        {
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = GetRuntimeFont();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Sprite LoadSprite(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (!Sprites.TryGetValue(name, out Sprite sprite))
            {
                sprite = Resources.Load<Sprite>($"DustlineUI/{name}");
                Sprites[name] = sprite;
            }

            return sprite;
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
                runtimeFont.name = "Dustline_Runtime_Font";
                runtimeFont.hideFlags = HideFlags.HideAndDontSave;
            }

            return runtimeFont;
        }
    }
}
