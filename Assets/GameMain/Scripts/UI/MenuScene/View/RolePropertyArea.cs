using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class RolePropertyArea : MonoBehaviour
    {
        [SerializeField] private TMP_Text _roleNameText;

        [SerializeField] private RawImage _roleAvatar;

        [SerializeField] private TMP_Text _roleInitialPropertyText;

        [SerializeField] private Transform _roleInitialItemParent;

        private RolePropertyAreaContext _context;

        public void OnInit(RolePropertyAreaContext context)
        {
            _context = context;
            if (_context == null)
            {
                OnReset();
                return;
            }

            _roleNameText.text = _context.RoleName;
            _roleInitialPropertyText.text = _context.InitialPropertyText;
        }

        public void OnReset(object userData = null)
        {
            _context = null;
        }
    }
}
