using System.Text;
using DataTable;
using Definition.DataStruct;
using UnityEngine;

namespace UI
{
    public class GoodsItemContext
    {
        public string Title;
        public string Type;
        public Sprite Icon;
        public string Description;
        public int Price;
        
        public static string CreateWeaponDescription(DRWeapon drWeapon)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"伤害：{drWeapon.Attack}\n");
            sb.Append($"冷却：{drWeapon.Cooldown}\n");
            sb.Append($"范围：{drWeapon.AttackRange}\n");
            sb.Append(CreatePropDescription(drWeapon.Modifiers));

            return sb.ToString();
        }

        public static string CreatePropDescription(StatModifier[] modifiers)
        {
            if (modifiers == null || modifiers.Length == 0) return null;
            StringBuilder sb = new StringBuilder();
            foreach (var modifier in modifiers)
            {
                sb.Append(modifier.ToString());
                sb.Append("\n");
            }

            return sb.ToString();
        }
    }    
}
