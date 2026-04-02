using Components;
using CustomUtility;
using Definition.DataStruct;
using Definition.Enum;
using Entity.EntityData;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity
{
    public class MeleeEnemy : EnemyBase
    {
        private enum AttackStateType
        {
            Idle,
            Check_OutRange,
            Check_InRange,
            Attack
        }

        private MovementComponent _movementComponent;

        private float _attackRange = 1f;
        private float _attackCooldown = 1f;
        private int _attackDamage = 1;

        private float _sqrAttackRange;
        private float _currAttackTimer;

        private AttackStateType _attackState = AttackStateType.Idle;
        private EnemyData _meleeEnemyData;
        private TargetableObject _targetableTarget;

        protected override TargetableObjectData _targetableObjectData => _meleeEnemyData;
        public override float AttackRange => _attackRange;

        public override ImpactData GetImpactData()
        {
            return new ImpactData(_meleeEnemyData.Camp, _attackDamage);
        }

        #region FSM

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            _movementComponent = GetComponent<MovementComponent>();
            _healthComponent = GetComponent<HealthComponent>();
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            if (userData is EnemyData enemyData)
            {
                _meleeEnemyData = enemyData;
                _healthComponent.OnInit(enemyData.MaxHealthBase);
                _movementComponent.OnInit(_meleeEnemyData.SpeedBase, this.CachedTransform, null, true);
                _movementComponent.SetMove(true);

                _attackRange = Mathf.Max(0.1f, _meleeEnemyData.AttackRange);
                _attackCooldown = Mathf.Max(0.01f, _meleeEnemyData.AttackCooldown);
                _attackDamage = Mathf.Max(1, _meleeEnemyData.AttackDamage);
                _sqrAttackRange = _attackRange * _attackRange;

                _currAttackTimer = 0f;
                _attackState = AttackStateType.Idle;
                _targetableTarget = null;

                this.CachedTransform.position = enemyData.Position;
            }
            else
            {
                Log.Error($"Invalid data type. Data type: {userData?.GetType()}");
            }
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            UpdateAttackState(elapseSeconds);
        }

        protected override void OnDead(EntityBase attacker)
        {
            if (Random.value < _meleeEnemyData.DropPercent)
            {
                var data = new CoinData(_meleeEnemyData.DropCoin, GameEntry.Entity.GenerateSerialId(), 10001)
                {
                    Position = this.CachedTransform.position
                };
                GameEntry.Entity.ShowCoin(data);
            }

            if (Random.value < _meleeEnemyData.DropPercent)
            {
                var data = new ExpData(_meleeEnemyData.DropExp, GameEntry.Entity.GenerateSerialId(), 10002)
                {
                    Position = this.CachedTransform.position
                };
                GameEntry.Entity.ShowExp(data);
            }

            base.OnDead(attacker);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            _movementComponent.OnReset();
            _healthComponent.OnReset();
            _targetableTarget = null;
            _currAttackTimer = 0f;
            _attackState = AttackStateType.Idle;

            base.OnHide(isShutdown, userData);
        }

        #endregion

        public override void SetTarget(Transform target)
        {
            base.SetTarget(target);
            _targetableTarget = target != null ? target.GetComponent<TargetableObject>() : null;
        }

        private void UpdateAttackState(float elapseSeconds)
        {
            _currAttackTimer += elapseSeconds;

            switch (_attackState)
            {
                case AttackStateType.Idle:
                    SetMove(false);
                    if (HasValidTarget())
                    {
                        TransitionTo(AttackStateType.Check_OutRange);
                    }

                    break;
                case AttackStateType.Check_OutRange:
                    if (!HasValidTarget())
                    {
                        TransitionTo(AttackStateType.Idle);
                        return;
                    }

                    if (IsTargetInRange())
                    {
                        TransitionTo(AttackStateType.Check_InRange);
                        return;
                    }

                    SetMove(true);
                    _movementComponent.SetDirection(GetTargetDirection());
                    break;
                case AttackStateType.Check_InRange:
                    if (!HasValidTarget())
                    {
                        TransitionTo(AttackStateType.Idle);
                        return;
                    }

                    SetMove(false);
                    if (!IsTargetInRange())
                    {
                        TransitionTo(AttackStateType.Check_OutRange);
                        return;
                    }

                    if (_currAttackTimer >= _attackCooldown)
                    {
                        TransitionTo(AttackStateType.Attack);
                    }

                    break;
            }
        }

        private void TransitionTo(AttackStateType newState)
        {
            _attackState = newState;

            if (_attackState == AttackStateType.Check_InRange)
            {
                SetMove(false);
            }

            if (_attackState == AttackStateType.Check_InRange && _currAttackTimer >= _attackCooldown)
            {
                TransitionTo(AttackStateType.Attack);
                return;
            }

            if (_attackState == AttackStateType.Attack)
            {
                SetMove(false);
                ExecuteAttack();
                _currAttackTimer = 0f;
                TransitionTo(AttackStateType.Check_InRange);
            }
        }

        private bool HasValidTarget()
        {
            if (_target == null) return false;

            if (_targetableTarget == null || _targetableTarget.CachedTransform != _target)
            {
                _targetableTarget = _target.GetComponent<TargetableObject>();
            }

            return _targetableTarget != null && _targetableTarget.Available && !_targetableTarget.IsDead;
        }

        private bool IsTargetInRange()
        {
            if (_target == null) return false;

            Vector3 delta = _target.position - this.CachedTransform.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= _sqrAttackRange;
        }

        private void ExecuteAttack()
        {
            if (!HasValidTarget() || !IsTargetInRange()) return;

            ImpactData targetImpactData = _targetableTarget.GetImpactData();
            ImpactData selfImpactData = GetImpactData();
            if (AIUtility.GetRelation(selfImpactData.Camp, targetImpactData.Camp) == RelationType.Friendly)
            {
                return;
            }

            int damage = AIUtility.CalcDamageHP(_attackDamage, null, targetImpactData.DefenseStat,
                targetImpactData.DodgeStat);
            _targetableTarget.ApplyDamage(this, damage);
        }

        private void SetMove(bool value)
        {
            if (_movementComponent != null)
            {
                _movementComponent.SetMove(value);
            }
        }

        private Vector3 GetTargetDirection()
        {
            if (_target == null)
            {
                return Vector3.zero;
            }

            return new Vector3(
                _target.position.x - this.CachedTransform.position.x,
                0f,
                _target.position.z - this.CachedTransform.position.z
            ).normalized;
        }
    }
}
