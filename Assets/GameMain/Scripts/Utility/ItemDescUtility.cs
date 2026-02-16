using System.Text;
using DataTable;
using Definition.DataStruct;
using Entity;
using Entity.EntityData;

namespace Game.Utility
{
    public static class ItemDescUtility
    {
        public static string CreatePropDescription(StatModifier[] modifiers)
        {
            if (modifiers == null || modifiers.Length == 0)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            foreach (StatModifier modifier in modifiers)
            {
                sb.Append(modifier);
                sb.Append('\n');
            }

            return sb.ToString();
        }

        public static string CreateWeaponDescription(DRWeapon drWeapon)
        {
            if (drWeapon == null)
            {
                return null;
            }

            return CreateWeaponDescription(drWeapon.Attack, drWeapon.Cooldown, drWeapon.AttackRange, drWeapon.Modifiers);
        }

        public static string CreateWeaponDescription(WeaponBase weapon)
        {
            if (weapon == null || weapon.WeaponData == null)
            {
                return null;
            }

            return CreateWeaponDescription(weapon.WeaponData);
        }
        
        public static string CreateWeaponDescription(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                return null;
            }

            return CreateWeaponDescription(weaponData.Attack, weaponData.Cooldown,
                weaponData.AttackRange, weaponData.Modifiers);
        }

        public static string CreatePropDescription(PropItem prop)
        {
            if (prop == null)
            {
                return null;
            }

            return CreatePropDescription(prop.Modifiers);
        }

        private static string CreateWeaponDescription(int attack, float cooldown, float attackRange,
            StatModifier[] modifiers)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Attack: {attack}");
            sb.AppendLine($"Cooldown: {cooldown}");
            sb.AppendLine($"Range: {attackRange}");

            string modifiersDesc = CreatePropDescription(modifiers);
            if (!string.IsNullOrEmpty(modifiersDesc))
            {
                sb.Append(modifiersDesc);
            }

            return sb.ToString();
        }
    }
}
