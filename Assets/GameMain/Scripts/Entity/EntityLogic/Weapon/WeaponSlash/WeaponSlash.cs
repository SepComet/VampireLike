using System.Collections.Generic;
using Definition.DataStruct;
using Definition.Enum;
using DG.Tweening;
using Entity.EntityData;
using CustomUtility;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity.Weapon
{
    public partial class WeaponSlash : WeaponBase
    {
        #region Property

        private const string SectorAngleParamKey = "SectorAngle";

        private WeaponSlashData _weaponData;

        private Quaternion _cachedRotation;
        [SerializeField] private float _rotateSpeed = 4f;

        private Sequence _attackSequence;
        private Vector3 _attackCenter;
        private Quaternion _cachedAttackRotation;
        private IWeaponAttackEffect _attackEffect;

        [SerializeField] private float attackDuration = 0.4f;
        [SerializeField] private float returnDuration = 0.4f;
        [SerializeField] private float _swingAngleScale = 1f;

        [SerializeField] private LayerMask _hitMask = ~0;
        [SerializeField] private int _maxHitColliders = 32;

        private Collider[] _hitResults;
        private readonly HashSet<int> _hitEntityIds = new();

        private float _attackRadius;
        private float _attackRadiusSqr;
        private float _sectorAngle;

        #endregion


        public override ImpactData GetImpactData()
        {
            return new ImpactData(_weaponData.OwnerCamp, _weaponData.Attack, AttackStat);
        }

        protected override void BuildStates()
        {
            RegisterState(new IdleState());
            RegisterState(new CheckInRangeState());
            RegisterState(new CheckOutRangeState());
            RegisterState(new AttackState());
        }

        protected override void Attack()
        {
            StopAttackTween(false);
            FaceTargetImmediately();

            _isAttacking = true;
            _attackCenter = CachedTransform.position;
            _cachedAttackRotation = CachedTransform.rotation;

            float swingAngle = Mathf.Clamp(_sectorAngle * Mathf.Max(0.1f, _swingAngleScale), 15f, 170f);
            Quaternion startRotation = Quaternion.AngleAxis(-swingAngle * 0.5f, Vector3.up) * _cachedAttackRotation;
            Quaternion endRotation = Quaternion.AngleAxis(swingAngle * 0.5f, Vector3.up) * _cachedAttackRotation;
            CachedTransform.rotation = startRotation;

            _attackSequence = DOTween.Sequence();
            _attackSequence.Append(
                CachedTransform.DORotateQuaternion(endRotation, attackDuration).SetEase(Ease.OutSine));
            _attackSequence.InsertCallback(attackDuration * 0.5f, () =>
            {
                _attackEffect?.Play(this, _attackCenter, _target, _attackRadius);
                ApplySectorDamage();
            });
            _attackSequence.Append(CachedTransform.DORotateQuaternion(_cachedAttackRotation, returnDuration)
                .SetEase(Ease.InSine));
            _attackSequence.AppendCallback(() =>
            {
                _isAttacking = false;
                _attackSequence = null;
            });
        }

        protected override void Check()
        {
            _target = SelectTarget(_sqrRange);
        }

        private void ApplySectorDamage()
        {
            if (_attackRadius <= 0f || _hitResults == null || _hitResults.Length == 0) return;

            int hitCount = Physics.OverlapSphereNonAlloc(_attackCenter, _attackRadius, _hitResults, _hitMask,
                QueryTriggerInteraction.Collide);

            _hitEntityIds.Clear();
            Vector3 forward = CachedTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();

            float halfAngle = _sectorAngle * 0.5f;
            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = _hitResults[i];
                if (collider == null) continue;

                TargetableObject targetable = collider.GetComponentInParent<TargetableObject>();
                if (targetable == null || !targetable.Available || targetable.IsDead) continue;
                if (!_hitEntityIds.Add(targetable.Id)) continue;

                Vector3 toTarget = targetable.CachedTransform.position - _attackCenter;
                toTarget.y = 0f;

                float sqrDistance = toTarget.sqrMagnitude;
                if (sqrDistance <= Mathf.Epsilon || sqrDistance > _attackRadiusSqr) continue;

                float angle = Vector3.Angle(forward, toTarget.normalized);
                if (angle > halfAngle) continue;

                AIUtility.PerformCollision(targetable, this);
            }
        }

        private void RotateToTarget(float elapseSeconds)
        {
            if (_target == null || !_target.Available) return;

            Vector3 directionToTarget = _target.CachedTransform.position - CachedTransform.position;
            directionToTarget.y = 0f;
            if (directionToTarget.sqrMagnitude <= Mathf.Epsilon) return;

            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
            CachedTransform.rotation =
                Quaternion.Slerp(CachedTransform.rotation, targetRotation, _rotateSpeed * elapseSeconds);
        }

        private void RotateToOrigin(float elapseSeconds)
        {
            CachedTransform.rotation =
                Quaternion.Slerp(CachedTransform.rotation, _cachedRotation, _rotateSpeed * elapseSeconds);
        }

        private void FaceTargetImmediately()
        {
            if (_target == null || !_target.Available) return;

            Vector3 directionToTarget = _target.CachedTransform.position - CachedTransform.position;
            directionToTarget.y = 0f;
            if (directionToTarget.sqrMagnitude <= Mathf.Epsilon) return;

            CachedTransform.rotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
        }

        #region Lifecycle

        protected override bool OnWeaponShow(object userData)
        {
            _weaponData = RequireWeaponData<WeaponSlashData>(userData);
            if (_weaponData == null) return false;
            WeaponData = _weaponData;

            _currAttackTimer = 0f;
            _sqrRange = _weaponData.AttackRange * _weaponData.AttackRange;
            _cachedRotation = CachedTransform.rotation;

            _attackRadius = Mathf.Max(0.1f, _weaponData.AttackRange);
            _attackRadiusSqr = _attackRadius * _attackRadius;

            _sectorAngle = 90f;
            if (_weaponData.Params != null &&
                _weaponData.Params.TryGetValue(SectorAngleParamKey.ToLower(), out string rawAngle))
            {
                if (float.TryParse(rawAngle, out float parsedAngle))
                {
                    _sectorAngle = Mathf.Clamp(parsedAngle, 1f, 360f);
                }
            }

            int capacity = Mathf.Max(1, _maxHitColliders);
            if (_hitResults == null || _hitResults.Length != capacity)
            {
                _hitResults = new Collider[capacity];
            }

            _attackEffect = new SlashSectorAttackEffect();

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
            StopAttackTween(true);
            _attackEffect = null;
        }

        protected override void OnWeaponAttach(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            BindAttackStatFromOwner(parentEntity);
        }

        protected override void OnWeaponDetach(EntityLogic parentEntity, object userData)
        {
            StopAttackTween(true);
            ReleaseAttackStatSubscription();
        }

        protected override void OnEnabledChanged(bool enabled)
        {
            if (!enabled)
            {
                StopAttackTween(true);
            }
        }

        #endregion


        private void StopAttackTween(bool resetTransform)
        {
            if (_attackSequence != null)
            {
                _attackSequence.Kill();
                _attackSequence = null;
            }

            _isAttacking = false;

            if (resetTransform)
            {
                CachedTransform.rotation = _cachedAttackRotation;
            }

            _hitEntityIds.Clear();
        }

        public float SectorAngle => _sectorAngle;
    }
}
