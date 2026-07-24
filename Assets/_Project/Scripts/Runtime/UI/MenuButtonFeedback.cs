using DG.Tweening;
using DustlineArena.Runtime.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DustlineArena.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class MenuButtonFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private Image background;
        private Image accent;
        private TMP_Text label;
        private Color idleBackground;
        private Color highlightedBackground;
        private bool highlighted;

        public void Initialize(
            Image buttonBackground,
            Image buttonAccent,
            TMP_Text buttonLabel,
            Color idleColor,
            Color highlightedColor)
        {
            background = buttonBackground;
            accent = buttonAccent;
            label = buttonLabel;
            idleBackground = idleColor;
            highlightedBackground = highlightedColor;

            if (accent != null)
            {
                accent.rectTransform.localScale = new Vector3(1f, 0f, 1f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            EventSystem.current?.SetSelectedGameObject(gameObject);
            SetHighlighted(true, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlighted(false, false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetHighlighted(true, true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetHighlighted(false, false);
        }

        private void SetHighlighted(bool value, bool playSound)
        {
            if (highlighted == value)
            {
                return;
            }

            highlighted = value;
            if (value && playSound)
            {
                GameAudio.PlayUiSelect();
            }

            DOTween.Kill(this);
            Sequence sequence = DOTween.Sequence().SetId(this).SetUpdate(true);

            if (background != null)
            {
                Color targetColor = value ? highlightedBackground : idleBackground;
                sequence.Join(DOTween.To(
                    () => background.color,
                    color => background.color = color,
                    targetColor,
                    0.12f));
            }

            if (label != null)
            {
                Color targetColor = value ? DustlineUiTheme.Gold : DustlineUiTheme.Text;
                sequence.Join(DOTween.To(
                    () => label.color,
                    color => label.color = color,
                    targetColor,
                    0.12f));
            }

            if (accent != null)
            {
                sequence.Join(accent.rectTransform
                    .DOScaleY(value ? 1f : 0f, 0.14f)
                    .SetEase(value ? Ease.OutBack : Ease.InQuad));
            }
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
            highlighted = false;

            if (background != null)
            {
                background.color = idleBackground;
            }

            if (label != null)
            {
                label.color = DustlineUiTheme.Text;
            }

            if (accent != null)
            {
                accent.rectTransform.localScale = new Vector3(1f, 0f, 1f);
            }
        }
    }
}
