using System;
using System.Collections.Generic;
using GameFramework.ObjectPool;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace UI
{
    public class DisplayListArea : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;

        [SerializeField] private TMP_Text _countText;

        [SerializeField] private DisplayItem _displayItemTemplate;

        [SerializeField] private Transform _displayItemRoot;

        [SerializeField] private Vector2 _startPosition = Vector2.zero;

        [SerializeField] private float _itemSize = 150f;

        [SerializeField] private float _itemSpacing = 10f;

        [SerializeField] private int _fixedColumnCount = 0;

        [SerializeField] private int _instancePoolCapacity = 32;

        private DisplayListAreaContext _context;

        private IObjectPool<DisplayItemObject> _displayItemObjectPool;
        private const string DisplayItemPoolName = "DisplayItem";

        private readonly List<DisplayItem> _activeItems = new List<DisplayItem>();

        public void OnInit(DisplayListAreaContext context)
        {
            if (context == null)
            {
                Log.Warning("DisplayListArea context is invalid.");
                return;
            }

            EnsurePool();
            _context = context;

            if (_titleText != null) _titleText.text = context.Title;
            UpdateCountText(context.CurrentCount, context.MaxCount);

            int itemCount = context.ItemContexts?.Length ?? 0;
            int targetCount = Mathf.Min(context.CurrentCount, itemCount);
            EnsureItemCount(targetCount);
            for (int i = 0; i < targetCount; i++)
            {
                _activeItems[i].OnInit(context.ItemContexts[i], i);
            }

            LayoutItems();
        }

        public void OnReset()
        {
            _context = null;

            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                HideItem(_activeItems[i]);
            }
        }

        public void OnDestroy()
        {
            _displayItemObjectPool.ReleaseAllUnused();
        }

        public DisplayItem AddItem(DisplayItemContext itemContext)
        {
            EnsurePool();
            if (_displayItemObjectPool == null)
            {
                return null;
            }

            DisplayItem item = CreateItem();
            if (item == null)
            {
                return null;
            }

            int index = _activeItems.Count;
            _activeItems.Add(item);
            item.OnInit(itemContext, index);
            item.SetIndex(index);

            int columnCount = _fixedColumnCount > 0 ? _fixedColumnCount : CalculateColumnCount(GetRootWidth());
            LayoutItem(item, index, columnCount);

            int currentCount = _activeItems.Count;
            int maxCount = _context != null ? _context.MaxCount : -1;
            if (_context != null)
            {
                _context.CurrentCount = currentCount;
            }

            UpdateCountText(currentCount, maxCount);

            return item;
        }

        public bool RemoveItemAt(int index)
        {
            if (index < 0 || index >= _activeItems.Count)
            {
                return false;
            }

            HideItem(_activeItems[index]);
            LayoutItems();

            int currentCount = _activeItems.Count;
            int maxCount = _context != null ? _context.MaxCount : -1;
            if (_context != null)
            {
                _context.CurrentCount = currentCount;
            }

            UpdateCountText(currentCount, maxCount);

            return true;
        }

        private void EnsurePool()
        {
            if (_displayItemObjectPool != null) return;

            if (_displayItemRoot == null)
            {
                _displayItemRoot = transform;
            }

            GridLayoutGroup gridLayout = _displayItemRoot.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.enabled = false;
            }

            if (GameEntry.ObjectPool.HasObjectPool<DisplayItemObject>(DisplayItemPoolName))
            {
                _displayItemObjectPool = GameEntry.ObjectPool.GetObjectPool<DisplayItemObject>(DisplayItemPoolName);
                return;
            }

            if (_displayItemTemplate == null)
            {
                Debug.LogError("DisplayListArea requires a DisplayItem template.");
                return;
            }

            _displayItemObjectPool =
                GameEntry.ObjectPool.CreateSingleSpawnObjectPool<DisplayItemObject>(DisplayItemPoolName,
                    _instancePoolCapacity);
        }

        private void EnsureItemCount(int count)
        {
            if (_displayItemObjectPool == null) return;

            for (int i = _activeItems.Count; i < count; i++)
            {
                DisplayItem item = CreateItem();
                if (item == null)
                {
                    break;
                }

                _activeItems.Add(item);
            }

            for (int i = _activeItems.Count - 1; i >= count; i--)
            {
                HideItem(_activeItems[i]);
            }
        }

        private DisplayItem CreateItem()
        {
            DisplayItem item = null;
            DisplayItemObject itemObject = _displayItemObjectPool.Spawn();
            if (itemObject != null)
            {
                item = (DisplayItem)itemObject.Target;
            }
            else
            {
                if (_displayItemTemplate == null)
                {
                    Debug.LogError("DisplayListArea requires a DisplayItem template.");
                    return null;
                }

                item = Instantiate(_displayItemTemplate);
                _displayItemObjectPool.Register(DisplayItemObject.Create(item), true);
            }

            if (_displayItemRoot != null)
            {
                Transform itemTransform = item.transform;
                itemTransform.SetParent(_displayItemRoot, false);
            }

            item.gameObject.SetActive(true);
            return item;
        }

        private void HideItem(DisplayItem item)
        {
            if (item == null) return;
            item.OnReset();
            _activeItems.Remove(item);
            item.gameObject.SetActive(false);
            _displayItemObjectPool?.Unspawn(item);
        }

        private void LayoutItems()
        {
            if (_displayItemRoot == null) return;

            int columnCount = _fixedColumnCount > 0 ? _fixedColumnCount : CalculateColumnCount(GetRootWidth());

            for (int i = 0; i < _activeItems.Count; i++)
            {
                DisplayItem item = _activeItems[i];
                item.SetIndex(i);
                LayoutItem(item, i, columnCount);
            }
        }

        private void LayoutItem(DisplayItem item, int index, int columnCount)
        {
            if (item == null) return;
            RectTransform itemRect = item.GetComponent<RectTransform>();
            if (itemRect == null) return;

            float step = _itemSize + _itemSpacing;
            int row = index / columnCount;
            int col = index % columnCount;

            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(0f, 1f);
            itemRect.pivot = new Vector2(0f, 1f);
            itemRect.sizeDelta = new Vector2(_itemSize, _itemSize);
            itemRect.anchoredPosition = _startPosition + new Vector2(col * step, -row * step);
        }

        private float GetRootWidth()
        {
            RectTransform rootRect = _displayItemRoot as RectTransform;
            return rootRect != null ? rootRect.rect.width : 0f;
        }

        private int CalculateColumnCount(float rootWidth)
        {
            if (rootWidth <= 0f) return 1;

            float step = _itemSize + _itemSpacing;
            int columns = Mathf.FloorToInt((rootWidth + _itemSpacing) / step);
            return Mathf.Max(1, columns);
        }

        private void UpdateCountText(int currentCount, int maxCount)
        {
            if (_countText == null) return;
            _countText.text = maxCount == -1 ? $"({currentCount})" : $"({currentCount}/{maxCount})";
        }
    }
}