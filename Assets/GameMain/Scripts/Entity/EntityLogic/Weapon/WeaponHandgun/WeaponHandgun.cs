using Definition.DataStruct;
using Definition.Enum;
using Entity.EntityData;
using CustomUtility;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity.Weapon
{
    public partial class WeaponHandgun : WeaponBase
    {
        #region Property

        private WeaponHandgunData _weaponData;

        private Quaternion _cachedRotation;

        [SerializeField] private float _rotateSpeed = 6f;
        [SerializeField] private LayerMask _hitMask = ~0;
        [SerializeField] private Vector3 _fireOriginOffset = Vector3.zero;

        [SerializeField] private float _hitMarkerSize = 0.2f;
        [SerializeField] private float _hitMarkerYOffset = 1.2f;
        [SerializeField] private float _hitMarkerDuration = 0.15f;
        [SerializeField] private Color _hitMarkerColor = new(1f, 0f, 0f, 0.95f);

        private IWeaponAttackEffect _attackEffect;

        #endregion


        public override ImpactData GetImpactData()
        {
            return new ImpactData(_weaponData.OwnerCamp, _weaponData.Attack, AttackStat);
        }

        protected override void BuildStates()
        {
            RegisterState(new IdleState());
            RegisterState(new CheckOutRangeState());
            RegisterState(new CheckInRangeState());
            RegisterState(new AttackState());
        }

        protected override void Attack()
        {
            FaceTargetImmediately();

            Vector3 fireOrigin = CachedTransform.TransformPoint(_fireOriginOffset);
            Vector3 fireDirection = CachedTransform.forward;
            float maxDistance = Mathf.Max(0.1f, _weaponData.AttackRange);

            if (Physics.Raycast(fireOrigin, fireDirection, out RaycastHit hit, maxDistance, _hitMask,
                    QueryTriggerInteraction.Collide))
            {
                TargetableObject targetable = hit.collider.GetComponentInParent<TargetableObject>();
                if (targetable != null && targetable.Available && !targetable.IsDead)
                {
                    _attackEffect?.Play(this, hit.point, targetable, 0f);
                    _isAttacking = true;
                    AIUtility.PerformCollision(targetable, this);
                    _isAttacking = false;
                }
            }
        }

        protected override void Check()
        {
            _target = SelectTarget(_sqrRange);
        }

        private void RotateToTarget(float elapseSeconds)
        {
            if (_target == null || !_target.Available) return;

            Vector3 directionToTarget = (_target.CachedTransform.position - CachedTransform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
            CachedTransform.rotation = Quaternion.Slerp(CachedTransform.rotation, targetRotation,
                _rotateSpeed * elapseSeconds);
        }

        private void RotateToOrigin(float elapseSeconds)
        {
            CachedTransform.rotation = Quaternion.Slerp(CachedTransform.rotation, _cachedRotation,
                _rotateSpeed * elapseSeconds);
        }

        private void FaceTargetImmediately()
        {
            if (_target == null || !_target.Available) return;

            Vector3 directionToTarget = _target.CachedTransform.position - CachedTransform.position;
            if (directionToTarget.sqrMagnitude <= Mathf.Epsilon) return;

            CachedTransform.rotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
        }

        #region Lifecycle

        protected override bool OnWeaponShow(object userData)
        {
            _weaponData = RequireWeaponData<WeaponHandgunData>(userData);
            if (_weaponData == null) return false;

            WeaponData = _weaponData;
            _currAttackTimer = 0f;
            _sqrRange = _weaponData.AttackRange * _weaponData.AttackRange;
            _cachedRotation = CachedTransform.rotation;
            _attackEffect = new HandgunHitMarkerAttackEffect(_hitMarkerSize, _hitMarkerYOffset, _hitMarkerDuration,
                _hitMarkerColor);

            if (_weaponData.OwnerCamp == CampType.Player)
            {
                gameObject.layer = LayerMask.NameToLayer("PlayerWeapon");
                _hitMask = LayerMask.GetMask("Enemy");
            }
            else if (_weaponData.OwnerCamp == CampType.Enemy)
            {
                gameObject.layer = LayerMask.NameToLayer("EnemyWeapon");
                _hitMask = LayerMask.GetMask("Player");
            }

            return true;
        }

        protected override void OnWeaponHide(object userData)
        {
            _attackEffect = null;
        }

        protected override void OnWeaponAttach(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            BindAttackStatFromOwner(parentEntity);
        }

        protected override void OnWeaponDetach(EntityLogic parentEntity, object userData)
        {
            ReleaseAttackStatSubscription();
        }

        #endregion
    }
}
