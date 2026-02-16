using CustomEvent;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class RoleItem : MonoBehaviour
    {
        [SerializeField] private Image _roleImage = null;

        private RoleItemContext _context = null;

        public void OnInit(RoleItemContext context)
        {
            _context = context;
            if (_context == null || string.IsNullOrEmpty(_context.IconName))
            {
                return;
            }

            GameEntry.SpriteCache.GetSprite(_context.IconName, sprite => _roleImage.sprite = sprite);
        }

        public void OnReset()
        {
            _context = null;
        }

        public void UpdateShowRole()
        {
            int roleId = _context?.RoleId ?? -1;
            GameEntry.Event.Fire(this, MenuSelectRoleSelectedEventArgs.Create(roleId));
        }

        public void OnConfirmRoleClick()
        {
            GameEntry.Event.Fire(this, MenuSelectRoleConfirmEventArgs.Create());
        }
    }
}
