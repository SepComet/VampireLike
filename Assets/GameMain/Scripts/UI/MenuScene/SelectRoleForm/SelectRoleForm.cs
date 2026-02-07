using System.Linq;
using DataTable;
using GameFramework.DataTable;
using Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UI
{
    public class SelectRoleForm : UGuiForm
    {
        [SerializeField] private GameObject _randomRoleCard = null;

        [SerializeField] private GameObject _roleCard = null;

        [SerializeField] private RolePropertyArea _propertyArea = null;

        private DRRole _currentRole = null;
        private IDataTable<DRRole> _roleDataTable = null;
        private DRRole[] _roleList = null;
        private RoleItem[] _roleItems = null;

        private ProcedureStartMenu _procedure;

        #region FSM

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            if (userData is ProcedureStartMenu procedure)
            {
                _procedure = procedure;
            }

            _roleDataTable = GameEntry.DataTable.GetDataTable<DRRole>();
            _roleItems = this.transform.GetComponentsInChildren<RoleItem>();
            _roleList = _roleDataTable.GetAllDataRows();
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            Log.Debug($"{_roleList.Length} || {_roleItems.Length}");
            for (int i = 0; i < _roleItems.Length; i++)
            {
                if (i < _roleList.Length)
                {
                    _roleItems[i].gameObject.SetActive(true);
                    _roleItems[i].OnInit(this, _roleList[i]);
                }
                else
                {
                    _roleItems[i].gameObject.SetActive(false);
                }
            }
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);

            foreach (var item in _roleItems)
            {
                item.OnReset();
            }
        }

        #endregion

        public void OnReturnButtonClick()
        {
            GameEntry.UI.CloseUIForm(this);
        }

        public void UpdateShowRole(DRRole role)
        {
            _currentRole = role;

            if (role == null)
            {
                _randomRoleCard.SetActive(true);
                _roleCard.SetActive(false);
                _propertyArea.OnReset();
            }
            else
            {
                _randomRoleCard.SetActive(false);
                _propertyArea.OnInit(role);
                _roleCard.SetActive(true);
            }
        }

        public void StartGame()
        {
            if (_currentRole == null)
            {
                int roleIndex = Random.Range(0, _roleList.Length);
                _currentRole = _roleList[roleIndex];
            }

            _procedure?.StartGame(_currentRole.Id);
        }
    }
}