//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.ObjectPool;
using System.Collections.Generic;
using Entity;
using StarForce;
using UnityEngine;
using UnityEngine.Serialization;
using UnityGameFramework.Runtime;

namespace CustomComponent
{
    public class HPBarComponent : GameFrameworkComponent
    {
        [SerializeField] private HPBarItem _hpBarItemTemplate = null;

        [SerializeField] private Transform _hpBarInstanceRoot = null;

        [SerializeField] private int _instancePoolCapacity = 16;

        private IObjectPool<HPBarItemObject> _hpBarItemObjectPool = null;
        private List<HPBarItem> _activeHPBarItems = null;
        private Canvas _cachedCanvas = null;

        private void Start()
        {
            if (_hpBarInstanceRoot == null)
            {
                Log.Error("You must set HP bar instance root first.");
                return;
            }

            _cachedCanvas = _hpBarInstanceRoot.GetComponent<Canvas>();
            _hpBarItemObjectPool =
                GameEntry.ObjectPool.CreateSingleSpawnObjectPool<HPBarItemObject>("HPBarItem", _instancePoolCapacity);
            _activeHPBarItems = new List<HPBarItem>();
        }

        private void OnDestroy()
        {
        }

        private void Update()
        {
            for (int i = _activeHPBarItems.Count - 1; i >= 0; i--)
            {
                HPBarItem hpBarItem = _activeHPBarItems[i];
                if (hpBarItem.Refresh())
                {
                    continue;
                }

                HideHPBar(hpBarItem);
            }
        }

        public void ShowHPBar(EntityBase entity, float fromHPRatio, float toHPRatio)
        {
            if (entity == null)
            {
                Log.Warning("Entity is invalid.");
                return;
            }

            HPBarItem hpBarItem = GetActiveHPBarItem(entity);
            if (hpBarItem == null)
            {
                hpBarItem = CreateHPBarItem(entity);
                _activeHPBarItems.Add(hpBarItem);
            }

            hpBarItem.Init(entity, _cachedCanvas, fromHPRatio, toHPRatio);
        }

        private void HideHPBar(HPBarItem hpBarItem)
        {
            hpBarItem.Reset();
            _activeHPBarItems.Remove(hpBarItem);
            _hpBarItemObjectPool.Unspawn(hpBarItem);
        }

        private HPBarItem GetActiveHPBarItem(EntityBase entity)
        {
            if (entity == null)
            {
                return null;
            }

            for (int i = 0; i < _activeHPBarItems.Count; i++)
            {
                if (_activeHPBarItems[i].Owner == entity)
                {
                    return _activeHPBarItems[i];
                }
            }

            return null;
        }

        private HPBarItem CreateHPBarItem(EntityBase entity)
        {
            HPBarItem hpBarItem = null;
            HPBarItemObject hpBarItemObject = _hpBarItemObjectPool.Spawn();
            if (hpBarItemObject != null)
            {
                hpBarItem = (HPBarItem)hpBarItemObject.Target;
            }
            else
            {
                hpBarItem = Instantiate(_hpBarItemTemplate);
                Transform transform = hpBarItem.GetComponent<Transform>();
                transform.SetParent(_hpBarInstanceRoot);
                transform.localScale = Vector3.one;
                _hpBarItemObjectPool.Register(HPBarItemObject.Create(hpBarItem), true);
            }

            return hpBarItem;
        }
    }
}