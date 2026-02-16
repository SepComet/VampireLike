using System;
using CustomEvent;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UI
{
    public class SelectRoleForm : UGuiForm
    {
        [SerializeField] private GameObject _randomRoleCard = null;

        [SerializeField] private GameObject _roleCard = null;

        [SerializeField] private RolePropertyArea _propertyArea = null;

        private SelectRoleFormContext _context = null;

        private RoleItem[] _roleItems = null;

        public void RefreshUI(SelectRoleFormContext context)
        {
            _context = context;

            int itemIndex = 1, roleIndex = 0;
            while (itemIndex < _roleItems.Length)
            {
                if (roleIndex < _context.RoleItemContexts.Length)
                {
                    _roleItems[itemIndex].OnInit(_context.RoleItemContexts[roleIndex]);
                    _roleItems[itemIndex].gameObject.SetActive(true);
                }
                else
                {
                    _roleItems[itemIndex].gameObject.SetActive(false);
                    _roleItems[itemIndex].OnReset();
                }

                itemIndex++;
                roleIndex++;
            }
            
            UpdateShowRole(_context.RolePropertyAreaContext);
        }

        #region FSM

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            _roleItems = this.transform.GetComponentsInChildren<RoleItem>();
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            if (userData is SelectRoleFormContext context)
            {
                RefreshUI(context);
                return;
            }

            Log.Warning("SelectRoleForm requires SelectRoleFormContext as userData.");
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);

            _context = null;
            foreach (var item in _roleItems)
            {
                item.OnReset();
            }
        }

        #endregion

        public void UpdateShowRole(RolePropertyAreaContext propertyAreaContext)
        {
            if (propertyAreaContext == null)
            {
                _randomRoleCard.SetActive(true);
                _roleCard.SetActive(false);
                _propertyArea.OnReset();
            }
            else
            {
                _randomRoleCard.SetActive(false);
                _propertyArea.OnInit(propertyAreaContext);
                _roleCard.SetActive(true);
            }
        }

        public void OnReturnButtonClick()
        {
            GameEntry.Event.Fire(this, MenuSelectRoleReturnEventArgs.Create());
        }
    }
}