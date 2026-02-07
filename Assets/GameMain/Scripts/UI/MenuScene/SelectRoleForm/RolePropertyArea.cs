using System.Text;
using DataTable;
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

        public void OnInit(DRRole role)
        {
            _roleNameText.text = role.RoleName;
            // _roleAvatar.texture = AssetUtility.GetUITextureIcon(role.IconName);

            StringBuilder sb = new StringBuilder();
            if (role.InitialProperties != null)
            {
                foreach (var modifier in role.InitialProperties)
                {
                    sb.Append($"{modifier.ToString()}\n");
                }
            }
            _roleInitialPropertyText.text = sb.ToString();


            // foreach (var item in role.InitialItemIds)
            // {
            //     var itemGO = Instantiate(_roleInitialItemParent.GetChild(0), _roleInitialItemParent);
            //     itemGO.GetComponent<RoleItem>().OnInit(item.Key, item.Value);
            // }
        }

        public void OnReset(object userData = null)
        {
            // _roleNameText.text = string.Empty;
            // _roleAvatar.texture = null;
            // _roleInitialPropertyText.text = string.Empty;
            // foreach (Transform item in _roleInitialItemParent)
            // {
            //     Destroy(item.gameObject);
            // }
        }
    }
}