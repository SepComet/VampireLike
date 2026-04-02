using Components;
using CustomUtility;
using Definition;
using Definition.DataStruct;
using Entity.EntityData;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity
{
    public class RemoteEnemy : EnemyBase
    {
        private const string EnemyProjectileGroupName = "Bullet";
        private const float EnemyProjectileGroupAutoReleaseInterval = 60f;
        private const int EnemyProjectileGroupCapacity = 64;
        private const float EnemyProjectileGroupExpireTime = 60f;
        private const int EnemyProjectileGroupPriority = 0;

        private const string ProjectileSpeedParamKey = "ProjectileSpeed";
        private const string ProjectileLifeTimeParamKey = "ProjectileLifeTime";
        private const string ProjectileSpawnForwardOffsetParamKey = "ProjectileSpawnForwardOffset";
        private const string ProjectileSpawnHeightOffsetParamKey = "ProjectileSpawnHeightOffset";
        private const string ProjectileAssetNameParamKey = "ProjectileAssetName";

        private MovementComponent _movementComponent;
        private float _attackRange = 1f;
        private float _attackRangeSquared;
        private float _attackCooldown = 1f;
        private int _attackDamage = 1;
        private float _currAttackTimer;

        [SerializeField] private float _projectileSpeed = 12f;
        [SerializeField] private float _projectileLifeTime = 3f;
        [SerializeField] private float _projectileSpawnForwardOffset = 0.7f;
        [SerializeField] private float _projectileSpawnHeightOffset = 0.6f;
        [SerializeField] private string _projectileAssetName = "BulletHandgun";

        private EnemyData _remoteEnemyData;

        protected override TargetableObjectData _targetableObjectData => _remoteEnemyData;
        public override float AttackRange => _attackRange;

        public override ImpactData GetImpactData()
        {
            return new ImpactData(_remoteEnemyData.Camp, _attackDamage);
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
                _movementComponent.OnInit(_remoteEnemyData.SpeedBase, this.CachedTransform, null, true);
                _movementComponent.SetMove(true);

                _attackRange = Mathf.Max(0.1f, _remoteEnemyData.AttackRange);
                _attackRangeSquared = _attackRange * _attackRange;
                _attackCooldown = Mathf.Max(0.01f, _remoteEnemyData.AttackCooldown);
                _attackDamage = Mathf.Max(1, _remoteEnemyData.AttackDamage);

                _projectileSpeed = ReadPositiveParam(ProjectileSpeedParamKey, _projectileSpeed);
                _projectileLifeTime = ReadPositiveParam(ProjectileLifeTimeParamKey, _projectileLifeTime);
                _projectileSpawnForwardOffset = ReadPositiveParam(ProjectileSpawnForwardOffsetParamKey,
                    _projectileSpawnForwardOffset);
                _projectileSpawnHeightOffset = ReadPositiveParam(ProjectileSpawnHeightOffsetParamKey,
                    _projectileSpawnHeightOffset);
                _projectileAssetName = ReadStringParam(ProjectileAssetNameParamKey, _projectileAssetName);

                _currAttackTimer = 0f;
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

            _currAttackTimer += elapseSeconds;

            if (_target == null)
            {
                _movementComponent.SetMove(false);
                return;
            }

            Vector3 toTarget = _target.position - this.CachedTransform.position;
            toTarget.y = 0f;
            float distanceSquared = toTarget.sqrMagnitude;
            if (distanceSquared <= _attackRangeSquared)
            {
                _movementComponent.SetMove(false);
                TryFireProjectile();
            }
            else
            {
                _movementComponent.SetMove(true);
                _movementComponent.SetDirection(GetTargetDirection());
            }
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            _movementComponent.OnReset();
            _healthComponent.OnReset();
            _currAttackTimer = 0f;

            base.OnHide(isShutdown, userData);
        }

        private void TryFireProjectile()
        {
            if (_currAttackTimer < _attackCooldown || _target == null) return;
            if (!EnsureEnemyProjectileGroup()) return;

            Vector3 spawnPosition = this.CachedTransform.position +
                                    this.CachedTransform.forward * _projectileSpawnForwardOffset +
                                    Vector3.up * _projectileSpawnHeightOffset;
            Vector3 direction = _target.position - spawnPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = this.CachedTransform.forward;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = Vector3.forward;
            }
            else
            {
                direction.Normalize();
            }

            int projectileEntityId = GameEntry.Entity.GenerateSerialId();
            var projectileData = new EnemyProjectileData(projectileEntityId, Id, _remoteEnemyData.Camp,
                _attackDamage, _projectileSpeed, _projectileLifeTime, direction)
            {
                Position = spawnPosition,
                Rotation = Quaternion.LookRotation(direction, Vector3.up)
            };

            GameEntry.Entity.ShowEntity(
                entityId: projectileEntityId,
                entityLogicType: typeof(EnemyProjectile),
                entityAssetName: AssetUtility.GetEntityAsset(_projectileAssetName),
                entityGroupName: EnemyProjectileGroupName,
                priority: Constant.AssetPriority.BulletAsset,
                userData: projectileData);

            _currAttackTimer = 0f;
        }

        private static bool EnsureEnemyProjectileGroup()
        {
            var entityComponent = GameEntry.Entity;
            if (entityComponent == null) return false;

            if (entityComponent.HasEntityGroup(EnemyProjectileGroupName))
            {
                return true;
            }

            bool addResult = entityComponent.AddEntityGroup(
                EnemyProjectileGroupName,
                EnemyProjectileGroupAutoReleaseInterval,
                EnemyProjectileGroupCapacity,
                EnemyProjectileGroupExpireTime,
                EnemyProjectileGroupPriority);

            if (!addResult)
            {
                Log.Warning("Can not create entity group '{0}'.", EnemyProjectileGroupName);
                return false;
            }

            return true;
        }

        private float ReadPositiveParam(string paramName, float defaultValue)
        {
            if (_remoteEnemyData != null &&
                _remoteEnemyData.TryGetParam(paramName, out string rawValue) &&
                float.TryParse(rawValue, out float parsedValue))
            {
                return Mathf.Max(0.01f, parsedValue);
            }

            return Mathf.Max(0.01f, defaultValue);
        }

        private string ReadStringParam(string paramName, string defaultValue)
        {
            if (_remoteEnemyData != null &&
                _remoteEnemyData.TryGetParam(paramName, out string rawValue) &&
                !string.IsNullOrWhiteSpace(rawValue))
            {
                return rawValue;
            }

            return defaultValue;
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
