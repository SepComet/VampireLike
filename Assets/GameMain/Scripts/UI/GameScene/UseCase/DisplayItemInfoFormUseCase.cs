using Definition.DataStruct;
using Entity;
using CustomUtility;
using Entity.Weapon;
using Procedure;

namespace UI
{
    public class DisplayItemInfoFormUseCase : IUIUseCase
    {
        private readonly ProcedureGame _procedureGame;

        private Player Player => _procedureGame != null ? _procedureGame.Player : null;

        public DisplayItemInfoFormUseCase(ProcedureGame procedureGame)
        {
            _procedureGame = procedureGame;
        }

        public DisplayItemInfoFormRawData CreateModel(int index, bool isWeapon)
        {
            if (index < 0 || Player == null)
            {
                return null;
            }

            if (isWeapon)
            {
                var weapons = Player.Weapons;
                if (weapons == null || index >= weapons.Count)
                {
                    return null;
                }

                WeaponBase weapon = weapons[index];
                if (weapon == null || weapon.WeaponData == null)
                {
                    return null;
                }

                return new DisplayItemInfoFormRawData
                {
                    IconAssetName = weapon.WeaponData.IconAssetName,
                    Title = weapon.WeaponData.Title,
                    TypeText = "Weapon",
                    Description = ItemDescUtility.CreateWeaponDescription(weapon),
                    Price = 0,
                    IsWeapon = true
                };
            }

            var props = Player.Props;
            if (props == null || index >= props.Count)
            {
                return null;
            }

            PropItem prop = props[index];
            if (prop == null)
            {
                return null;
            }

            return new DisplayItemInfoFormRawData
            {
                IconAssetName = prop.IconAssetName,
                Title = prop.Title,
                TypeText = "Prop",
                Description = ItemDescUtility.CreatePropDescription(prop),
                Price = 0,
                IsWeapon = false
            };
        }
    }
}