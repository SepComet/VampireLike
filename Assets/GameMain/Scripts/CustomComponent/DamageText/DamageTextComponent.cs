using System.Collections.Generic;
using GameFramework.ObjectPool;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace CustomComponent
{
    public class DamageTextComponent : GameFrameworkComponent
    {
        [SerializeField] private int _instancePoolCapacity = 32;

        [SerializeField] private string _poolName = "DamageTextItem";

        [SerializeField] private RectTransform _instanceRoot;

        [SerializeField] private Canvas _canvas;

        [SerializeField] private GameObject _damageTextItemPrefab;

        private IObjectPool<DamageTextItemObject> _damageTextItemPool;

        private readonly List<DamageTextItem> _activeDamageTextItems = new();

        private void Start()
        {
            _damageTextItemPool =
                GameEntry.ObjectPool.CreateSingleSpawnObjectPool<DamageTextItemObject>(_poolName,
                    _instancePoolCapacity);
        }

        public void ShowDamage(Vector3 worldPosition, int damage)
        {
            if (damage <= 0 || _damageTextItemPool == null || _canvas == null) return;

            DamageTextItem item = CreateDamageTextItem();
            if (item == null) return;

            item.Show(worldPosition, damage, _canvas, OnItemComplete);
            _activeDamageTextItems.Add(item);
        }

        private DamageTextItem CreateDamageTextItem()
        {
            DamageTextItemObject itemObject = _damageTextItemPool.Spawn();
            if (itemObject != null)
            {
                return (DamageTextItem)itemObject.Target;
            }
            
            GameObject itemGo = Instantiate(_damageTextItemPrefab, _instanceRoot, false);
            
            DamageTextItem item = itemGo.GetComponent<DamageTextItem>();
            _damageTextItemPool.Register(DamageTextItemObject.Create(item), true);
            return item;
        }

        private void OnItemComplete(DamageTextItem item)
        {
            if (item == null) return;
            item.ResetItem();
            _activeDamageTextItems.Remove(item);
            _damageTextItemPool.Unspawn(item);
        }
    }
}