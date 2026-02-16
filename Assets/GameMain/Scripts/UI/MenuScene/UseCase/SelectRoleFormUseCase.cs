using System.Text;
using DataTable;
using GameFramework.DataTable;
using Procedure;
using UnityEngine;

namespace UI
{
    public class SelectRoleFormUseCase : IUIUseCase
    {
        private readonly IDataTable<DRRole> _roleDataTable;

        private readonly ProcedureStartMenu _procedureStartMenu;

        public int SelectedRoleId { get; private set; }

        public SelectRoleFormUseCase(ProcedureStartMenu procedureStartMenu)
        {
            _procedureStartMenu = procedureStartMenu;
            _roleDataTable = GameEntry.DataTable.GetDataTable<DRRole>();
        }

        public SelectRoleFormRawData CreateModel()
        {
            return BuildModel(0);
        }

        public SelectRoleFormRawData SelectRole(int roleId)
        {
            SelectedRoleId = roleId;
            return BuildModel(roleId);
        }

        public bool ConfirmSelectedRole()
        {
            DRRole[] roles = _roleDataTable != null ? _roleDataTable.GetAllDataRows() : null;
            if (roles == null || roles.Length == 0)
            {
                return false;
            }

            if (SelectedRoleId < 0 || _roleDataTable.GetDataRow(SelectedRoleId) == null)
            {
                int randomIndex = Random.Range(0, roles.Length);
                SelectedRoleId = roles[randomIndex].Id;
            }

            bool result = SelectedRoleId >= 0;
            if (result)
            {
                _procedureStartMenu.StartGame(SelectedRoleId);
            }

            return result;
        }

        private SelectRoleFormRawData BuildModel(int selectedRoleId)
        {
            DRRole[] roles = _roleDataTable.GetAllDataRows();
            int count = _roleDataTable.Count;
            int[] roleIds = new int[count];
            string[] iconNames = new string[count];
            for (int i = 0; i < count; i++)
            {
                roleIds[i] = roles[i].Id;
                iconNames[i] = roles[i].IconName;
            }

            DRRole selectedRole = selectedRoleId > 0 ? _roleDataTable.GetDataRow(selectedRoleId) : null;
            return new SelectRoleFormRawData
            {
                RoleIds = roleIds,
                RoleIconNames = iconNames,
                SelectedRoleId = selectedRole?.Id ?? -1,
                SelectedRoleName = selectedRole?.RoleName,
                SelectedRoleInitialPropertyText = selectedRole != null
                    ? BuildRoleInitialPropertyText(selectedRole)
                    : null
            };
        }

        private static string BuildRoleInitialPropertyText(DRRole role)
        {
            if (role == null || role.InitialProperties == null || role.InitialProperties.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < role.InitialProperties.Length; i++)
            {
                sb.Append(role.InitialProperties[i]);
                sb.Append('\n');
            }

            return sb.ToString();
        }
    }
}