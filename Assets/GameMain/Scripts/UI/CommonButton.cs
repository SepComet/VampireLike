//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityGameFramework.Runtime;

namespace UI
{
    public class CommonButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler
    {
        private const float FadeTime = 0.3f;
        private const float OnHoverAlpha = 0.7f;
        private const float OnClickAlpha = 0.6f;

        [SerializeField] private UnityEvent _onPointerEnterAction = null;

        [SerializeField] private UnityEvent _onClickAction = null;

        [SerializeField] private UnityEvent _onPointerExitAction = null;

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
            StartCoroutine(_canvasGroup.FadeToAlpha(OnHoverAlpha, FadeTime));
            _onPointerEnterAction.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            StopAllCoroutines();
            StartCoroutine(_canvasGroup.FadeToAlpha(1f, FadeTime));
            _onPointerExitAction?.Invoke();
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

        /// <summary>
        /// 注册鼠标相关事件
        /// </summary>
        /// <param name="actionId">0、1、2 分别是 Enter、Click、Exit</param>
        /// <param name="action">回调事件</param>
        public void RegisterAction(int actionId, UnityAction action)
        {
            switch (actionId)
            {
                case 0:
                    _onPointerEnterAction.AddListener(action);
                    break;
                case 1:
                    _onClickAction.AddListener(action);
                    break;
                case 2:
                    _onPointerExitAction.AddListener(action);
                    break;
                default:
                    throw new IndexOutOfRangeException("actionId 无效");
            }
        }

        public void UnRegisterAction(int actionId, UnityAction action)
        {
            switch (actionId)
            {
                case 0:
                    _onPointerEnterAction.RemoveListener(action);
                    break;
                case 1:
                    _onClickAction.RemoveListener(action);
                    break;
                case 2:
                    _onPointerExitAction.RemoveListener(action);
                    break;
                default:
                    throw new IndexOutOfRangeException("actionId 无效");
            }
        }
    }
}