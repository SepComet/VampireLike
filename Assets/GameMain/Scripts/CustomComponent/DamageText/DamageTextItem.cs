using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CustomComponent
{
    public class DamageTextItem : MonoBehaviour
    {
        private const float RiseDistance = 80f;
        private const float RiseDuration = 0.55f;
        private const float FadeDelay = 0.2f;
        private const float FadeDuration = 0.35f;
        private static readonly Color NormalColor = new Color(1f, 0.32f, 0.23f, 1f);

        [SerializeField] private RectTransform _cachedTransform;

        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField] private TMP_Text _text;

        private Sequence _sequence;
        private Action<DamageTextItem> _onComplete;

        private void Awake()
        {
            if (_cachedTransform == null) _cachedTransform = GetComponent<RectTransform>();
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_text == null) _text = GetComponent<TMP_Text>();
        }

        public void Show(Vector3 worldPosition, int damage, Canvas canvas, Action<DamageTextItem> onComplete)
        {
            if (canvas == null || GameEntry.Scene.MainCamera == null) return;

            _onComplete = onComplete;
            gameObject.SetActive(true);
            KillSequence();

            _canvasGroup.alpha = 1f;
            _cachedTransform.localScale = Vector3.one;
            _text.color = NormalColor;
            _text.text = damage.ToString();

            Vector3 screenPos = GameEntry.Scene.MainCamera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screenPos,
                canvas.worldCamera, out Vector2 localPos);
            localPos.x += UnityEngine.Random.Range(-24f, 24f);
            _cachedTransform.anchoredPosition = localPos;

            _sequence = DOTween.Sequence();
            _sequence.Append(_cachedTransform.DOAnchorPosY(localPos.y + RiseDistance, RiseDuration)
                .SetEase(Ease.OutQuad));
            _sequence.Join(_cachedTransform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.18f, 6, 0.75f));
            _sequence.Join(_canvasGroup.DOFade(0f, FadeDuration).SetDelay(FadeDelay));
            _sequence.OnComplete(() => _onComplete?.Invoke(this));
        }

        public void ResetItem()
        {
            KillSequence();
            _onComplete = null;
            _canvasGroup.alpha = 1f;
            _text.text = string.Empty;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            KillSequence();
        }

        private void KillSequence()
        {
            if (_sequence == null) return;
            _sequence.Kill();
            _sequence = null;
        }
    }
}