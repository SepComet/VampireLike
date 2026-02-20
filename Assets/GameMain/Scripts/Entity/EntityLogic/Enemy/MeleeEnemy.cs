using Components;
using Definition.DataStruct;
using Entity.EntityData;
using Entity.Weapon;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity
{
    public class MeleeEnemy : EnemyBase
    {
        private MovementComponent _movementComponent;
        private float _attackRange = 1f;
        private float _attackRangeSquared;
        private EnemyData _meleeEnemyData;
        private WeaponBase _weapon;

        protected override TargetableObjectData _targetableObjectData => _meleeEnemyData;

        public override ImpactData GetImpactData()
        {
            return new ImpactData(_meleeEnemyData.Camp, 0);
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
                _attackRangeSquared = _attackRange * _attackRange;
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

            if (_target == null)
            {
                _movementComponent.SetMove(false);
                _movementComponent.OnUpdate(elapseSeconds, realElapseSeconds);
                return;
            }

            float distanceSquared = (this.CachedTransform.position - _target.position).sqrMagnitude;
            if (distanceSquared < _attackRangeSquared)
            {
                // 攻击
                _movementComponent.SetMove(false);
            }
            else
            {
                _movementComponent.SetMove(true);
                _movementComponent.SetDirection(GetTargetDirection());
            }

            _movementComponent.OnUpdate(elapseSeconds, realElapseSeconds);
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

            base.OnHide(isShutdown, userData);
        }

        #endregion

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
