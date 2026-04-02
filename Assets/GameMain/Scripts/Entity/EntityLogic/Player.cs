using System.Collections.Generic;
using Components;
using CustomEvent;
using DataTable;
using Definition.DataStruct;
using Entity.EntityData;
using GameFramework.Event;
using UnityGameFramework.Runtime;
using Entity.Weapon;

namespace Entity
{
    public class Player : TargetableObject
    {
        protected override TargetableObjectData _targetableObjectData => _playerData;

        private InputComponent _inputComponent;
        private MovementComponent _movementComponent;
        private AbsorbComponent _absorbComponent;
        private StatComponent _statComponent;
        private BackpackComponent _backpackComponent;
        private PlayerData _playerData;

        /// <summary>
        /// 当前金币。
        /// </summary>
        private int _coin;

        /// <summary>
        /// 当前经验。
        /// </summary>
        private int _exp;

        /// <summary>
        /// 每级所需的经验。
        /// tip: _expRequires[1] 表示 Lv 0 -> Lv 1 需要的经验
        /// </summary>
        private int[] _expRequires;

        /// <summary>
        /// 当前的角色等级。
        /// </summary>
        private int _currentLevel;

        /// <summary>
        /// 玩家是否启用。
        /// </summary>
        private bool _enable;

        private bool _weaponEnabled = true;

        public int Coin
        {
            get => _coin;
            set
            {
                GameEntry.Event.Fire(this, PlayerCoinChangeEventArgs.Create(value - _coin, value));
                _coin = value;
            }
        }

        public int Exp
        {
            get => _exp;
            set
            {
                if (value >= _expRequires[_currentLevel + 1])
                {
                    value -= _expRequires[_currentLevel + 1];
                    _currentLevel++;
                    PendingLevelPoints++;
                    GameEntry.Event.Fire(this, PlayerLevelUpEventArgs.Create());
                }

                GameEntry.Event.Fire(this, PlayerExpChangeEventArgs.Create(value, _expRequires[_currentLevel + 1]));
                _exp = value;
            }
        }

        public int CurrentLevel => _currentLevel;

        public int PendingLevelPoints { get; set; }

        public IReadOnlyList<WeaponBase> Weapons => _backpackComponent != null ? _backpackComponent.Weapons : null;
        public IReadOnlyList<PropItem> Props => _backpackComponent != null ? _backpackComponent.Props : null;
        public int WeaponCapacity => _backpackComponent != null ? _backpackComponent.WeaponCapacity : 0;

        public bool AddProp(PropItem prop)
        {
            if (prop == null || _backpackComponent == null) return false;
            return _backpackComponent.AttachProp(prop);
        }

        public bool RemoveWeapon(WeaponBase weapon)
        {
            if (weapon == null || _backpackComponent == null)
            {
                return false;
            }

            if (!_backpackComponent.DetachWeapon(weapon))
            {
                return false;
            }

            GameEntry.Entity.HideEntity(weapon);
            return true;
        }

        public bool Enable
        {
            get => _enable;
            set
            {
                if (value == _enable) return;
                _enable = value;
                _movementComponent.SetMove(value);
                _backpackComponent.SetWeaponState(value && _weaponEnabled);
                _inputComponent.SetListening(value);
            }
        }

        public bool WeaponEnabled => _weaponEnabled;

        public void SetWeaponEnabled(bool enabled)
        {
            _weaponEnabled = enabled;
            _backpackComponent?.SetWeaponState(_enable && _weaponEnabled);
        }

        public void InitRole(DRRole role)
        {
            // MovementComponent
            _movementComponent.OnInit(role.Speed, this.CachedTransform, _statComponent);
            _movementComponent.SetMove(true);

            // HealthComponent
            _healthComponent.OnInit(role.MaxHealth, _statComponent);

            // BackpackComponent
            Coin = role.Coin;
            _backpackComponent.OnInit(this, role.WeaponCapacity);
            GameEntry.Entity.ShowWeapon(new WeaponKnifeData(GameEntry.Entity.GenerateSerialId(), Id, _playerData.Camp));

            // StatComponent
            foreach (var modifier in role.InitialProperties)
            {
                _statComponent.AddModifier(modifier);
            }

            _expRequires = role.ExpRequires;

            // Fire Events
            GameEntry.Event.Fire(this,
                PlayerHealthChangeEventArgs.Create(0, _healthComponent.CurrentHealth, _healthComponent.MaxHealth));

            GameEntry.Event.Fire(this, PlayerExpChangeEventArgs.Create(0, 100));

            GameEntry.Event.Fire(this,
                PlayerCoinChangeEventArgs.Create(Coin, Coin));
        }

        #region FSM

        public override ImpactData GetImpactData()
        {
            return new ImpactData(_playerData.Camp, 0, null, _healthComponent.DefenseStat, _healthComponent.DodgeStat);
        }

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            _inputComponent = GetComponent<InputComponent>();
            _movementComponent = GetComponent<MovementComponent>();
            _absorbComponent = GetComponent<AbsorbComponent>();
            _statComponent = GetComponent<StatComponent>();
            _healthComponent = GetComponent<HealthComponent>();
            _backpackComponent = GetComponent<BackpackComponent>();

            if (_absorbComponent == null)
            {
                _absorbComponent = gameObject.AddComponent<AbsorbComponent>();
            }
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            GameEntry.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, ShowEntitySuccess);

            _statComponent.OnInit();

            if (userData is PlayerData playerData)
            {
                _playerData = playerData;
            }

            _inputComponent.OnInit();
            _inputComponent.SetListening(true);
            _absorbComponent.OnInit(this, _statComponent);
            _weaponEnabled = true;

            _currentLevel = 0;
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            _inputComponent.OnUpdate(elapseSeconds, realElapseSeconds);

            _movementComponent.SetDirection(_inputComponent.Direction);
            _absorbComponent.OnUpdate(elapseSeconds, realElapseSeconds);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            _inputComponent.OnReset();
            _movementComponent.OnReset();
            _healthComponent.OnReset();
            _statComponent.OnReset();
            _backpackComponent.OnReset();
            _absorbComponent.OnReset();

            base.OnHide(isShutdown, userData);
        }

        #endregion

        #region Event Handlers

        private void ShowEntitySuccess(object sender, GameEventArgs e)
        {
            if (!(e is ShowEntitySuccessEventArgs args)) return;
            if (args.Entity.Logic is WeaponBase weapon)
            {
                _backpackComponent.AttachWeapon(weapon);
            }
        }

        #endregion
    }
}
