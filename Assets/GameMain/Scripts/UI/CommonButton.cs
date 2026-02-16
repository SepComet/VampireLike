//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UI
{
    public class CommonButton : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler,
        IPointerUpHandler
    {
        private const float FadeTime = 0.3f;
        private const float OnHoverAlpha = 0.7f;
        private const float OnClickAlpha = 0.6f;

        [SerializeField] private UnityEvent _onPointerEnterAction = null;

        [SerializeField] private UnityEvent _onClickAction = null;

        [SerializeField] private UnityEvent _onPointerExitAction = null;

        [SerializeField] private bool _enableFade = true;

        private CanvasGroup _canvasGroup = null;

        private void Awake()
        {
            _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
        }

        private void OnDisable()
        {
            _canvasGroup.alpha = 1f;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            StopAllCoroutines();
            if (_enableFade)
                StartCoroutine(_canvasGroup.FadeToAlpha(OnHoverAlpha, FadeTime));
            _onPointerEnterAction.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _canvasGroup.alpha = OnClickAlpha;
            _onClickAction.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _canvasGroup.alpha = OnHoverAlpha;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            StopAllCoroutines();
            if (_enableFade)
                StartCoroutine(_canvasGroup.FadeToAlpha(1.0f, FadeTime));
            _onPointerExitAction.Invoke();
        }
    }
}