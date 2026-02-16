using System;
using System.Collections.Generic;
using Components;
using CustomComponent;
using Definition.DataStruct;
using Definition.Enum;
using DG.Tweening;
using Entity.EntityData;
using StarForce;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity
{
    public class WeaponKnife : WeaponBase
    {
        private WeaponKnifeData _weaponData;
        private float _currAttackTimer = 0f;
        private EnemyManagerComponent _enemy;
        private EntityBase _target;

        private float _sqrRange;

        private Collider _collider;
        private Rigidbody _rigidbody;
        private Quaternion _cachedRotation;
        [SerializeField] private float _rotateSpeed = 2f;

        private Dictionary<WeaponStateType, WeaponStateBase> _weaponStates;

        private StatProperty _attackStat = new();
        private Action<StatModifier, bool> _attackStatCallback;

        [SerializeField] private float attackDuration = 0.15f; // 戳刺持续时间
        [SerializeField] private float returnDuration = 0.2f; // 回归持续时间

        public override ImpactData GetImpactData()
        {
            return new ImpactData(_weaponData.OwnerCamp, _weaponData.Attack, _attackStat);
        }

        private void InitStates()
        {
            _weaponStates = new Dictionary<WeaponStateType, WeaponStateBase>
            {
                { WeaponStateType.Idle, new IdleState() },
                { WeaponStateType.Check_InRange, new CheckInRangeState() },
                { WeaponStateType.Check_OutRange, new CheckOutRangeState() },
                { WeaponStateType.Attack, new AttackState() }
            };
            foreach (var state in _weaponStates.Values)
            {
                state.OnInit(this);
            }
        }

        #region FSM Methods

        protected override void Attack()
        {
            Vector3 targetPos = CachedTransform.position + CachedTransform.forward * _weaponData.AttackRange;
            _isAttacking = true;
            _collider.enabled = true;
            _rigidbody.detectCollisions = true;
            Transform parentTransform = CachedTransform.parent;
            CachedTransform.SetParent(null);

            var sequence = DOTween.Sequence();
            sequence.Append(CachedTransform.DOMove(targetPos, attackDuration).SetEase(Ease.OutQuad));
            sequence.AppendCallback(() =>
            {
                _collider.enabled = false;
                _rigidbody.detectCollisions = false;
                CachedTransform.SetParent(parentTransform);
            });
            sequence.Append(CachedTransform.DOLocalMove(Vector3.zero, returnDuration / 2f).SetEase(Ease.InQuad));
            sequence.AppendCallback(() => { _isAttacking = false; });
        }

        protected override void Check()
        {
            _target = null;
            float minSqrMagnitude = float.MaxValue;

            var enemies = _enemy.Enemies;
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.Available) continue;

                float sqrMagnitude = AIUtility.GetSqrMagnitude(this, enemy);

                if (minSqrMagnitude < sqrMagnitude) continue;

                _target = enemy;
                minSqrMagnitude = sqrMagnitude;
            }
        }

        private void RotateToTarget(float elapseSeconds)
        {
            if (_target == null || !_target.Available) return;

            Vector3 directionToTarget = (_target.CachedTransform.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);

            var quaternion = Quaternion.Slerp(CachedTransform.rotation, targetRotation, _rotateSpeed * elapseSeconds);
            CachedTransform.rotation = quaternion;
        }

        private void RotateToOrigin(float elapseSeconds)
        {
            var quaternion = Quaternion.Slerp(CachedTransform.rotation, _cachedRotation, _rotateSpeed * elapseSeconds);
            CachedTransform.rotation = quaternion;
        }

        protected override void TransitionTo(WeaponStateType state)
        {
            _currentState?.OnLeave();
            _currentState = _weaponStates[state];
            _currentState.OnEnter();
        }

        #endregion

        #region FSM

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            _weaponData = userData as WeaponKnifeData;
            if (_weaponData == null)
            {
                Log.Error("WeaponKnife data is invalid.");
                return;
            }
            WeaponData = _weaponData;

            _collider = GetComponent<Collider>();
            _rigidbody = GetComponent<Rigidbody>();
            _currAttackTimer = 0;
            _enemy = GameEntry.EnemyManager;
            _sqrRange = _weaponData.AttackRange * _weaponData.AttackRange;
            _cachedRotation = transform.rotation;
            _collider.enabled = false;
            _rigidbody.detectCollisions = false;

            if (_weaponData.OwnerCamp == CampType.Player)
                this.gameObject.layer = LayerMask.NameToLayer("PlayerWeapon");
            if (_weaponData.OwnerCamp == CampType.Enemy)
                this.gameObject.layer = LayerMask.NameToLayer("EnemyWeapon");

            InitStates();
            TransitionTo(WeaponStateType.Idle);
        }

        protected override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            base.OnAttachTo(parentEntity, parentTransform, userData);

            if (parentEntity is Player player)
            {
                var statComponent = player.GetComponent<StatComponent>();
                if (statComponent != null)
                {
                    this._attackStat = statComponent.GetStat(StatType.Attack);
                    this._attackStatCallback = (modifier, b) => statComponent.UpdateStat(_attackStat, modifier, b);
                    statComponent.Subscribe(StatType.Attack, this._attackStatCallback);
                }
            }
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (!_isEnabled) return;

            Log.Debug($"current state: {_currentState}");
            _currentState?.OnUpdate(elapseSeconds, realElapseSeconds);
        }

        protected override void OnDetachFrom(EntityLogic parentEntity, object userData)
        {
            base.OnDetachFrom(parentEntity, userData);

            if (parentEntity is Player player)
            {
                var statComponent = player.GetComponent<StatComponent>();
                if (statComponent != null)
                {
                    statComponent.Unsubscribe(StatType.Attack, this._attackStatCallback);
                }
            }

            this._attackStat = null;
            this._attackStatCallback = null;
        }

        #endregion

        private class IdleState : WeaponStateBase
        {
            private WeaponKnife _weapon;

            public override WeaponStateType State => WeaponStateType.Idle;

            public override void OnInit(WeaponBase weapon) => _weapon = weapon as WeaponKnife;

            public override void OnEnter()
            {
            }

            public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                _weapon.Check();
                _weapon.RotateToOrigin(elapseSeconds);
                if (_weapon._target != null && _weapon._target.Available)
                {
                    _weapon.TransitionTo(WeaponStateType.Check_OutRange);
                }
            }

            public override void OnLeave()
            {
            }
        }

        private class CheckOutRangeState : WeaponStateBase
        {
            private WeaponKnife _weapon;

            public override WeaponStateType State => WeaponStateType.Check_OutRange;

            public override void OnInit(WeaponBase weapon) => _weapon = weapon as WeaponKnife;

            public override void OnEnter()
            {
            }

            public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                _weapon.Check();
                _weapon.RotateToTarget(elapseSeconds);

                if (_weapon._target == null || !_weapon._target.Available)
                {
                    _weapon.TransitionTo(WeaponStateType.Idle);
                    return;
                }

                if (AIUtility.GetSqrMagnitude(_weapon._target, _weapon) < _weapon._sqrRange)
                {
                    _weapon.TransitionTo(WeaponStateType.Check_InRange);
                    return;
                }
            }

            public override void OnLeave()
            {
            }
        }

        private class CheckInRangeState : WeaponStateBase
        {
            private WeaponKnife _weapon;

            public override WeaponStateType State => WeaponStateType.Check_InRange;

            public override void OnInit(WeaponBase weapon) => _weapon = weapon as WeaponKnife;

            public override void OnEnter()
            {
            }

            public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                _weapon.Check();
                _weapon.RotateToTarget(elapseSeconds);
                _weapon._currAttackTimer += elapseSeconds;

                if (_weapon._target == null || !_weapon._target.Available)
                {
                    _weapon.TransitionTo(WeaponStateType.Idle);
                    return;
                }

                if (AIUtility.GetSqrMagnitude(_weapon._target, _weapon) >= _weapon._sqrRange)
                {
                    _weapon.TransitionTo(WeaponStateType.Check_OutRange);
                    return;
                }

                if (_weapon._currAttackTimer >= _weapon._weaponData.Cooldown)
                {
                    _weapon.TransitionTo(WeaponStateType.Attack);
                    return;
                }
            }

            public override void OnLeave()
            {
                _weapon._currAttackTimer = 0;
            }
        }

        private class AttackState : WeaponStateBase
        {
            private WeaponKnife _weapon;

            public override WeaponStateType State => WeaponStateType.Attack;

            public override void OnInit(WeaponBase weapon) => _weapon = weapon as WeaponKnife;

            public override void OnEnter()
            {
                _weapon.Attack();
            }

            public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                if (!_weapon._isAttacking)
                {
                    _weapon.TransitionTo(WeaponStateType.Check_InRange);
                }
            }

            public override void OnLeave()
            {
            }
        }
    }
}
