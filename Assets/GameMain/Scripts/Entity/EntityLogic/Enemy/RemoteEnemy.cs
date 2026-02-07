using Components;
using Definition.DataStruct;
using Entity.EntityData;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity
{
    public class RemoteEnemy : EnemyBase
    {
        private float _speed;
        private MovementComponent _movementComponent;
        private float _attackRange = 1f;
        private float _attackRangeSquared;
        private EnemyData _remoteEnemyData;

        protected override TargetableObjectData _targetableObjectData => _remoteEnemyData;

        public override ImpactData GetImpactData()
        {
            return new ImpactData(_remoteEnemyData.Camp, 0);
        }

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
                _remoteEnemyData = enemyData;
                _healthComponent.OnInit(enemyData.MaxHealthBase);
                _movementComponent.OnInit(_remoteEnemyData.SpeedBase, this.CachedTransform);
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

        protected override void OnHide(bool isShutdown, object userData)
        {
            _movementComponent.OnReset();
            _healthComponent.OnReset();

            base.OnHide(isShutdown, userData);
        }

        private Vector3 GetTargetDirection()
        {
            return new Vector3(
                _target.position.x - this.CachedTransform.position.x,
                0f,
                _target.position.z - this.CachedTransform.position.z
            ).normalized;
        }
    }
}