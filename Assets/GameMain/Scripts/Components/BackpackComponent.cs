using System.Collections.Generic;
using CustomEvent;
using Definition.DataStruct;
using Entity;
using UnityEngine;

namespace Components
{
    public class BackpackComponent : MonoBehaviour
    {
        /// <summary>
        /// 武器槽容量，由DRRole决定。
        /// </summary>
        private int _weaponCapacity;

        /// <summary>
        /// 玩家类引用。
        /// </summary>
        private Player _player;

        /// <summary>
        /// 武器列表。
        /// </summary>
        private List<WeaponBase> _weapons;

        /// <summary>
        /// 道具列表。
        /// </summary>
        private List<PropItem> _props;

        /// <summary>
        /// 玩家状态组件引用，用于道具效果的应用。
        /// </summary>
        private StatComponent _statComponent;

        /// <summary>
        /// 武器装备槽，在 Editor 中进行配置，表示武器在世界中的显示位置。
        /// </summary>
        [SerializeField] private Transform[] _weaponSlots;

        public void OnInit(Player player, int weaponCapacity)
        {
            _weaponCapacity = weaponCapacity;
            _player = player;

            _weapons = new List<WeaponBase>(_weaponCapacity);
            _props = new List<PropItem>();
            _statComponent = GetComponent<StatComponent>();
        }

        public void OnReset()
        {
            _weapons.Clear();
            _props.Clear();
        }

        public bool AttachWeapon(WeaponBase weapon)
        {
            if (_weapons.Count == _weaponCapacity) return false;
            GameEntry.Entity.AttachEntity(weapon.Id, _player.Id, _weaponSlots[_weapons.Count]);
            _weapons.Add(weapon);
            weapon.SetEnabled(true);
            return true;
        }

        public bool DetachWeapon(WeaponBase weapon)
        {
            bool result = _weapons.Remove(weapon);
            if (result)
            {
                GameEntry.Entity.DetachEntity(weapon.Id, _player.Id);
            }

            return result;
        }

        public bool AttachProp(PropItem prop)
        {
            _props.Add(prop);
            prop.OnAttach(_statComponent);
            return true;
        }

        public bool DetachProp(PropItem prop)
        {
            _props.Remove(prop);
            prop.OnDetach(_statComponent);
            return true;
        }

        public void SetWeaponState(bool state)
        {
            if (_weapons == null) return;
            foreach (var weapon in _weapons)
            {
                weapon.SetEnabled(state);
            }
        }
    }
}