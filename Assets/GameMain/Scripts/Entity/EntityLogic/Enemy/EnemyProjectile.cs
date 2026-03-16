using Definition.DataStruct;
using Definition.Enum;
using Entity.EntityData;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity
{
    public class EnemyProjectile : EntityBase
    {
        private EnemyProjectileData _projectileData;
        private Vector3 _direction = Vector3.forward;
        private float _elapsedTime;
        private bool _isActive;
        private bool _isSimulationDriven;
        private ImpactData _impactData;
        private Collider[] _cachedColliders;

        public bool IsActive => _isActive;
        public ImpactData GetImpactData() => _impactData;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            _projectileData = userData as EnemyProjectileData;
            if (_projectileData == null)
            {
                Log.Error("Enemy projectile data is invalid.");
                _isActive = false;
                GameEntry.Entity.HideEntity(this);
                return;
            }

            _isActive = true;
            _elapsedTime = 0f;
            _impactData = new ImpactData(_projectileData.OwnerCamp, _projectileData.AttackDamage);

            _direction = _projectileData.Direction;
            _direction.y = 0f;
            if (_direction.sqrMagnitude <= Mathf.Epsilon)
            {
                _direction = CachedTransform.forward;
                _direction.y = 0f;
            }

            if (_direction.sqrMagnitude <= Mathf.Epsilon)
            {
                _direction = Vector3.forward;
            }
            else
            {
                _direction.Normalize();
            }

            CachedTransform.rotation = Quaternion.LookRotation(_direction, Vector3.up);

            if (_projectileData.OwnerCamp == CampType.Player)
            {
                gameObject.layer = LayerMask.NameToLayer("PlayerWeapon");
            }
            else if (_projectileData.OwnerCamp == CampType.Enemy)
            {
                gameObject.layer = LayerMask.NameToLayer("EnemyWeapon");
            }

            _isSimulationDriven = IsDrivenBySimulationWorld();
            SetColliderEnabled(!_isSimulationDriven);
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (!_isActive || _projectileData == null) return;

            bool isSimulationDriven = IsDrivenBySimulationWorld();
            if (isSimulationDriven != _isSimulationDriven)
            {
                _isSimulationDriven = isSimulationDriven;
                SetColliderEnabled(!_isSimulationDriven);
            }

            if (_isSimulationDriven) return;

            if (_projectileData.Speed > 0f)
            {
                CachedTransform.position += _direction * (_projectileData.Speed * elapseSeconds);
            }

            _elapsedTime += elapseSeconds;
            if (_projectileData.LifeTime > 0f && _elapsedTime >= _projectileData.LifeTime)
            {
                Expire();
            }
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            _isActive = false;
            _projectileData = null;
            _elapsedTime = 0f;
            _isSimulationDriven = false;
            _impactData = default;
            _direction = Vector3.forward;

            base.OnHide(isShutdown, userData);
        }

        public void Expire()
        {
            if (!_isActive) return;
            _isActive = false;
            GameEntry.Entity.HideEntity(this);
        }

        private static bool IsDrivenBySimulationWorld()
        {
            var simulationWorld = GameEntry.SimulationWorld;
            return simulationWorld != null && simulationWorld.UseSimulationMovement;
        }

        private void SetColliderEnabled(bool enabled)
        {
            _cachedColliders ??= GetComponentsInChildren<Collider>(true);
            if (_cachedColliders == null) return;

            for (int i = 0; i < _cachedColliders.Length; i++)
            {
                Collider collider = _cachedColliders[i];
                if (collider == null)
                {
                    continue;
                }

                collider.enabled = enabled;
            }
        }
    }
}