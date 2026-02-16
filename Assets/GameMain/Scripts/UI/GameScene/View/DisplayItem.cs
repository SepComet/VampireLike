using CustomEvent;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class DisplayItem : MonoBehaviour
    {
        [SerializeField] private IconArea _iconArea;

        [SerializeField] private RectTransform _itemRect;

        private DisplayItemContext _context;

        private int _index = -1;

        public void OnInit(DisplayItemContext context, int index)
        {
            if (context == null) return;

            _context = context;
            _index = index;

            if (_iconArea != null)
            {
                string iconName = _context.IconAssetName;
                GameEntry.SpriteCache.GetSprite(iconName, sprite => { _iconArea.OnInit(sprite, context.Rarity); });
            }
        }

        public void OnReset()
        {
            _context = null;
            _index = -1;

            if (_iconArea != null)
            {
                _iconArea.OnReset();
            }
        }

        public void OnItemInfoShow()
        {
            if (_index < 0) return;
            Vector3 targetPos = _itemRect.position + new Vector3(0, _itemRect.sizeDelta.y / 2, 0);
            GameEntry.Event.Fire(this, DisplayItemShowEventArgs.Create(_index, _context.IsWeapon, targetPos));
        }

        public void SetIndex(int index)
        {
            _index = index;
        }

        public void OnItemInfoHide()
        {
            GameEntry.Event.Fire(this, DisplayItemHideEventArgs.Create());
        }
    }
}