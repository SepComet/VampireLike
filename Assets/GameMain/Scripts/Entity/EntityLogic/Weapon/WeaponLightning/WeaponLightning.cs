using System.Collections.Generic;
using CustomUtility;
using Definition.DataStruct;
using Definition.Enum;
using DG.Tweening;
using Entity.EntityData;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity.Weapon
{
    public partial class WeaponLightning : WeaponBase
    {
        private const string HitRadiusParamKey = "HitRadius";

        private WeaponLightningData _weaponData;

        private Quaternion _cachedRotation;
        [SerializeField] private float _rotateSpeed = 5f;

        [SerializeField] private float _takeOffDuration = 0.3f;
        [SerializeField] private float _flyToTargetDuration = 0.2f;
        [SerializeField] private float _strikeDuration = 0.1f;
        [SerializeField] private float _returnDuration = 0.22f;
        
        [SerializeField] private float _hoverHeight = 15f;

        [SerializeField] private LayerMask _hitMask = ~0;
        [SerializeField] private int _maxHitColliders = 32;

        private Sequence _attackSequence;
        private Transform _attackParent;
        private Vector3 _lockedStrikePoint;

        private Collider[] _hitResults;
        private readonly HashSet<int> _hitEntityIds = new();

        private float _hitRadius;
        private float _hitRadiusSqr;

        private IWeaponAttackEffect _attackEffect;

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
            StopAttackTween(false);
            FaceTargetImmediately();

            _isAttacking = true;
            _lockedStrikePoint = ResolveStrikePoint();
            _attackParent = CachedTransform.parent;
            CachedTransform.SetParent(null);
            
            Vector3 takeOffPosition = CachedTransform.position;
            Vector3 hoverPosition = _lockedStrikePoint + Vector3.up * Mathf.Max(0.1f, _hoverHeight);

            _attackSequence = DOTween.Sequence();
            _attackSequence.Append(CachedTransform.DOMove(takeOffPosition, _takeOffDuration).SetEase(Ease.OutQuad));
            _attackSequence.Append(CachedTransform.DOMove(hoverPosition, _flyToTargetDuration).SetEase(Ease.OutSine));
            _attackSequence.AppendCallback(() => CachedTransform.LookAt(_lockedStrikePoint));
            _attackSequence.Append(CachedTransform.DOMove(_lockedStrikePoint, _strikeDuration).SetEase(Ease.InQuad));
            _attackSequence.AppendCallback(() =>
            {
                _attackEffect?.Play(this, _lockedStrikePoint, _target, _hitRadius);
                ApplyGroundAreaDamage();

                if (_attackParent != null)
                {
                    CachedTransform.SetParent(_attackParent);
                }
            });
            _attackSequence.Append(CachedTransform.DOLocalMove(Vector3.zero, _returnDuration).SetEase(Ease.OutSine));
            _attackSequence.AppendCallback(() =>
            {
                _isAttacking = false;
                _attackSequence = null;
                _attackParent = null;
            });
        }

        protected override void Check()
        {
            _target = SelectTarget(_sqrRange);
        }

        private Vector3 ResolveStrikePoint()
        {
            if (_target != null && _target.Available)
            {
                return _target.CachedTransform.position;
            }

            return CachedTransform.position;
        }

        private void ApplyGroundAreaDamage()
        {
            if (_hitRadius <= 0f) return;

            if (TryQueueAreaCollisionQuery(_lockedStrikePoint, _hitRadius, Mathf.Max(1, _maxHitColliders)))
            {
                _hitEntityIds.Clear();
                return;
            }

            if (_hitResults == null || _hitResults.Length == 0) return;

            int hitCount = Physics.OverlapSphereNonAlloc(_lockedStrikePoint, _hitRadius, _hitResults, _hitMask,
                QueryTriggerInteraction.Collide);

            _hitEntityIds.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = _hitResults[i];
                if (collider == null) continue;

                TargetableObject targetable = collider.GetComponentInParent<TargetableObject>();
                if (targetable == null || !targetable.Available || targetable.IsDead) continue;
                if (!_hitEntityIds.Add(targetable.Id)) continue;

                Vector3 delta = targetable.CachedTransform.position - _lockedStrikePoint;
                delta.y = 0f;
                if (delta.sqrMagnitude > _hitRadiusSqr) continue;

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

        protected override bool OnWeaponShow(object userData)
        {
            _weaponData = RequireWeaponData<WeaponLightningData>(userData);
            if (_weaponData == null) return false;
            WeaponData = _weaponData;

            _currAttackTimer = 0f;
            _sqrRange = _weaponData.AttackRange * _weaponData.AttackRange;
            _cachedRotation = CachedTransform.rotation;

            _hitRadius = ReadPositiveParam(HitRadiusParamKey, _weaponData.AttackRange);
            _hitRadiusSqr = _hitRadius * _hitRadius;

            int colliderCapacity = Mathf.Max(1, _maxHitColliders);
            if (_hitResults == null || _hitResults.Length != colliderCapacity)
            {
                _hitResults = new Collider[colliderCapacity];
            }

            _attackEffect = new LightningStrikeAttackEffect();

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

        private float ReadPositiveParam(string paramName, float defaultValue)
        {
            if (_weaponData.Params != null &&
                _weaponData.Params.TryGetValue(paramName.ToLower(), out string rawValue) &&
                float.TryParse(rawValue, out float parsedValue))
            {
                return Mathf.Max(0.1f, parsedValue);
            }

            return Mathf.Max(0.1f, defaultValue);
        }

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
                if (_attackParent != null)
                {
                    CachedTransform.SetParent(_attackParent);
                    CachedTransform.localPosition = Vector3.zero;
                }
                else if (CachedTransform.parent != null)
                {
                    CachedTransform.localPosition = Vector3.zero;
                }

                CachedTransform.rotation = _cachedRotation;
            }

            _attackParent = null;
            _hitEntityIds.Clear();
        }
    }
}
