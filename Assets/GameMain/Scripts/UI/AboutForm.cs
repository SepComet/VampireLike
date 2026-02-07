//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using StarForce;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace UI
{
    public class AboutForm : UGuiForm
    {
        [SerializeField] private RectTransform _rectTransform = null;
        [SerializeField] private float _scrollSpeed = 1f;

        private float _initPosition = 0f;

#if UNITY_2017_3_OR_NEWER
        protected override void OnInit(object userData)
#else
        protected internal override void OnInit(object userData)
#endif
        {
            base.OnInit(userData);

            CanvasScaler canvasScaler = GetComponentInParent<CanvasScaler>();
            if (canvasScaler == null)
            {
                Log.Warning("Can not find CanvasScaler component.");
                return;
            }

            _initPosition = -0.5f * canvasScaler.referenceResolution.x * Screen.height / Screen.width;
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnOpen(object userData)
#else
        protected internal override void OnOpen(object userData)
#endif
        {
            base.OnOpen(userData);

            _rectTransform.SetLocalPositionY(_initPosition);

            // 换个音乐
            GameEntry.Sound.PlayMusic(3);
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnClose(bool isShutdown, object userData)
#else
        protected internal override void OnClose(bool isShutdown, object userData)
#endif
        {
            base.OnClose(isShutdown, userData);

            // 还原音乐
            GameEntry.Sound.PlayMusic(1);
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
#else
        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
#endif
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            _rectTransform.AddLocalPositionY(_scrollSpeed * elapseSeconds);
            if (_rectTransform.localPosition.y > _rectTransform.sizeDelta.y - _initPosition)
            {
                _rectTransform.SetLocalPositionY(_initPosition);
            }
        }
    }
}