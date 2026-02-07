using DataTable;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class RoleItem : MonoBehaviour
    {
        [SerializeField] private CommonButton _button = null;
        [SerializeField] private Image _roleImage = null;
        private SelectRoleForm _selectRoleForm = null;
        private DRRole _role = null;

        public void OnInit(SelectRoleForm selectRoleForm, DRRole role)
        {
            this._selectRoleForm = selectRoleForm;
            this._role = role;
            GameEntry.SpriteCache.GetSprite(role.IconName, sprite => _roleImage.sprite = sprite);

            _button.RegisterAction(0, OnPointerEnter);
            _button.RegisterAction(1, OnPointerClick);
            _button.RegisterAction(2, OnPointerExit);
        }

        public void OnReset()
        {
            this._selectRoleForm = null;
            this._role = null;

            _button.UnRegisterAction(0, OnPointerEnter);
            _button.UnRegisterAction(1, OnPointerClick);
            _button.UnRegisterAction(2, OnPointerExit);
        }

        private void OnPointerEnter()
        {
            _selectRoleForm.UpdateShowRole(_role);
        }

        private void OnPointerClick()
        {
            _selectRoleForm.StartGame();
        }

        private void OnPointerExit()
        {
            _selectRoleForm.UpdateShowRole(null);
        }
    }
}