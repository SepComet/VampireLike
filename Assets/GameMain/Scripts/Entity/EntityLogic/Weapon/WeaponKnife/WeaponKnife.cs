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
    public partial class WeaponKnife : WeaponBase
    {
        private const string HitRadiusParamKey = "HitRadius";

        private WeaponKnifeData _weaponData;

        private Quaternion _cachedRotation;
        [SerializeField] private float _rotateSpeed = 4f;

        private Sequence _attackSequence;
        private Transform _attackParent;
        private Vector3 _attackCenter;

        [SerializeField] private float attackDuration = 0.15f;
        [SerializeField] private float returnDuration = 0.2f;

        [SerializeField] private LayerMask _hitMask = ~0;
        [SerializeField] private int _maxHitColliders = 32;
        
        private IWeaponAttackEffect _attackEffect;
        private Collider[] _hitResults;
        private readonly HashSet<int> _hitEntityIds = new();

        private float _hitRadius;
        private float _hitRadiusSqr;

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
            Transform parentTransform = CachedTransform.parent;
            _attackParent = parentTransform;
            _attackCenter = _target != null ? _target.CachedTransform.position : CachedTransform.position;
            CachedTransform.SetParent(null);

            Vector3 targetPos = CachedTransform.position + CachedTransform.forward * _weaponData.AttackRange;
            _attackSequence = DOTween.Sequence();
            _attackSequence.Append(CachedTransform.DOMove(targetPos, attackDuration).SetEase(Ease.OutQuad));
            _attackSequence.AppendCallback(() =>
            {
                _attackEffect?.Play(this, _attackCenter, null, _hitRadius);
                ApplyGroundAreaDamage();
                if (_attackParent != null)
                {
                    CachedTransform.SetParent(_attackParent);
                }
            });
            _attackSequence.Append(
                CachedTransform.DOLocalMove(Vector3.zero, returnDuration * 0.5f).SetEase(Ease.InQuad));
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

        private void RotateToTarget(float elapseSeconds)
        {
            if (_target == null || !_target.Available) return;

            Vector3 directionToTarget = _target.CachedTransform.position - CachedTransform.position;
            if (directionToTarget.sqrMagnitude <= Mathf.Epsilon) return;

            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
            CachedTransform.rotation =
                Quaternion.Slerp(CachedTransform.rotation, targetRotation, _rotateSpeed * elapseSeconds);
        }

        public void RotateToOrigin(float elapseSeconds)
        {
            CachedTransform.rotation =
                Quaternion.Slerp(CachedTransform.rotation, _cachedRotation, _rotateSpeed * elapseSeconds);
        }

        private void FaceTargetImmediately()
        {
            if (_target == null || !_target.Available) return;

            Vector3 directionToTarget = _target.CachedTransform.position - CachedTransform.position;
            if (directionToTarget.sqrMagnitude <= Mathf.Epsilon) return;

            CachedTransform.rotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
        }

        private void ApplyGroundAreaDamage()
        {
            if (_hitRadius <= 0f || _hitResults == null || _hitResults.Length == 0) return;

            int hitCount = Physics.OverlapSphereNonAlloc(_attackCenter, _hitRadius, _hitResults, _hitMask,
                QueryTriggerInteraction.Collide);

            _hitEntityIds.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = _hitResults[i];
                if (collider == null) continue;

                TargetableObject targetable = collider.GetComponentInParent<TargetableObject>();
                if (targetable == null || !targetable.Available || targetable.IsDead) continue;
                if (!_hitEntityIds.Add(targetable.Id)) continue;

                Vector3 delta = targetable.CachedTransform.position - _attackCenter;
                delta.y = 0f;
                if (delta.sqrMagnitude > _hitRadiusSqr) continue;

                AIUtility.PerformCollision(targetable, this);
            }
        }

        protected override bool OnWeaponShow(object userData)
        {
            _weaponData = RequireWeaponData<WeaponKnifeData>(userData);
            if (_weaponData == null) return false;
            WeaponData = _weaponData;

            _currAttackTimer = 0f;
            _sqrRange = _weaponData.AttackRange * _weaponData.AttackRange;
            _cachedRotation = CachedTransform.rotation;

            string hitRadiusRaw = _weaponData.GetParamsString(HitRadiusParamKey);
            if (!float.TryParse(hitRadiusRaw, out _hitRadius))
            {
                _hitRadius = _weaponData.AttackRange;
            }
            _hitRadius = Mathf.Max(0.1f, _hitRadius);

            _hitRadiusSqr = _hitRadius * _hitRadius;
            _attackEffect = new KnifeRangeAttackEffect();

            int colliderCapacity = Mathf.Max(1, _maxHitColliders);
            if (_hitResults == null || _hitResults.Length != colliderCapacity)
            {
                _hitResults = new Collider[colliderCapacity];
            }

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

        private void StopAttackTween(bool resetTransform)
        {
            if (_attackSequence != null)
            {
                _attackSequence.Kill();
                _attackSequence = null;
            }

            _isAttacking = false;

            if (resetTransform && _attackParent != null)
            {
                CachedTransform.SetParent(_attackParent);
                CachedTransform.localPosition = Vector3.zero;
            }

            _attackParent = null;
            _hitEntityIds.Clear();
        }
    }
}
