using System.Collections.Generic;
using Components;
using CustomUtility;
using Definition.DataStruct;
using Definition.Enum;
using DG.Tweening;
using Entity.EntityData;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity.Weapon
{
    public partial class WeaponLance : WeaponBase
    {
        // 长枪专用数据，包含强类型 ParamsData。
        [SerializeField] private WeaponLanceData _weaponData;

        private Quaternion _cachedRotation;
        // 朝向目标时的旋转速度，可被 ParamsData.RotateSpeed 覆盖。
        [SerializeField] private float _rotateSpeed = 5f;

        private Sequence _attackSequence;
        private Transform _attackParent;

        // 前刺动画耗时。
        [SerializeField] private float _attackDuration = 0.12f;
        // 收枪返回耗时。
        [SerializeField] private float _returnDuration = 0.18f;

        [SerializeField] private LayerMask _hitMask = ~0;
        [SerializeField] private int _maxHitColliders = 32;

        private IWeaponAttackEffect _attackEffect;
        private Collider[] _hitResults;
        private readonly HashSet<int> _hitEntityIds = new();

        // 前戳矩形判定的横向半宽。
        private float _hitHalfWidth;
        // 前戳矩形判定盒体的总高度。
        private float _hitHeight;
        // 盒体中心相对战斗平面的高度偏移。
        private float _hitCenterYOffset;
        // 从判定起点向前延伸的有效刺击长度。
        private float _pierceLength;
        // 判定起点相对武器当前位置的前移量。
        private float _forwardOffset;
        // 本次攻击锁定的前戳方向，避免受位移动画中的武器位置影响。
        private Vector3 _strikeDirection = Vector3.forward;
        // 本次攻击锁定的矩形判定中心。
        private Vector3 _strikeCenter;

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
            CacheStrikeSnapshot();

            _isAttacking = true;
            _attackParent = CachedTransform.parent;
            CachedTransform.SetParent(null);

            Vector3 targetPos = CachedTransform.position + _strikeDirection * _pierceLength;
            _attackSequence = DOTween.Sequence();
            _attackSequence.Append(CachedTransform.DOMove(targetPos, _attackDuration).SetEase(Ease.OutQuad));
            _attackSequence.AppendCallback(() =>
            {
                _attackEffect?.Play(this, _strikeCenter, _target, _hitHalfWidth);
                ApplyPierceDamage();

                if (_attackParent != null)
                {
                    CachedTransform.SetParent(_attackParent);
                }
            });
            _attackSequence.Append(CachedTransform.DOLocalMove(Vector3.zero, _returnDuration).SetEase(Ease.InQuad));
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

        private void CacheStrikeSnapshot()
        {
            _strikeDirection = ResolvePlanarForward();
            Vector3 strikeStart = CachedTransform.position;
            strikeStart.y = ResolveStrikePlaneY();
            strikeStart += _strikeDirection * _forwardOffset;
            _strikeCenter = strikeStart + _strikeDirection * (_pierceLength * 0.5f);
            _strikeCenter.y += _hitCenterYOffset;
        }

        private Vector3 ResolvePlanarForward()
        {
            Vector3 forward = CachedTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            return forward;
        }

        private void ApplyPierceDamage()
        {
            if (_hitHalfWidth <= 0f || _pierceLength <= 0f) return;

            Vector3 strikeDirection = _strikeDirection;
            if (strikeDirection.sqrMagnitude <= Mathf.Epsilon) return;

            Vector3 broadPhaseCenter = _strikeCenter;
            Quaternion broadPhaseRotation = Quaternion.LookRotation(strikeDirection, Vector3.up);

            if (TryQueueRectangleCollisionQuery(broadPhaseCenter, _hitHalfWidth, _pierceLength * 0.5f,
                    strikeDirection, Mathf.Max(1, _maxHitColliders)))
            {
                _hitEntityIds.Clear();
                return;
            }

            int capacity = Mathf.Max(1, _maxHitColliders);
            if (_hitResults == null || _hitResults.Length != capacity)
            {
                _hitResults = new Collider[capacity];
            }

            Vector3 halfExtents = new(_hitHalfWidth, _hitHeight * 0.5f, _pierceLength * 0.5f);
            int hitCount = Physics.OverlapBoxNonAlloc(broadPhaseCenter, halfExtents, _hitResults, broadPhaseRotation,
                _hitMask, QueryTriggerInteraction.Collide);
            _hitEntityIds.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = _hitResults[i];
                if (collider == null) continue;

                TargetableObject targetable = collider.GetComponentInParent<TargetableObject>();
                if (targetable == null || !targetable.Available || targetable.IsDead) continue;
                if (!_hitEntityIds.Add(targetable.Id)) continue;

                if (!IsTargetInsidePierce(targetable, broadPhaseCenter, strikeDirection)) continue;

                AIUtility.PerformCollision(targetable, this);
            }
        }

        private bool IsTargetInsidePierce(TargetableObject targetable, Vector3 strikeCenter, Vector3 strikeDirection)
        {
            Vector3 delta = targetable.CachedTransform.position - strikeCenter;
            delta.y = 0f;

            Vector3 right = Vector3.Cross(Vector3.up, strikeDirection);
            float forwardDistance = Vector3.Dot(delta, strikeDirection);
            float lateralDistance = Vector3.Dot(delta, right);
            float targetRadius = ResolveTargetCollisionRadius(targetable);

            return Mathf.Abs(forwardDistance) <= _pierceLength * 0.5f + targetRadius &&
                   Mathf.Abs(lateralDistance) <= _hitHalfWidth + targetRadius;
        }

        private static float ResolveTargetCollisionRadius(TargetableObject targetable)
        {
            if (targetable == null)
            {
                return 0f;
            }

            MovementComponent movementComponent = targetable.GetComponent<MovementComponent>();
            return movementComponent != null ? Mathf.Max(0f, movementComponent.EnemyBodyRadius) : 0f;
        }

        private float ResolveStrikePlaneY()
        {
            if (_target != null && _target.Available)
            {
                return _target.CachedTransform.position.y;
            }

            if (_attackParent != null)
            {
                return _attackParent.position.y;
            }

            if (CachedTransform.parent != null)
            {
                return CachedTransform.parent.position.y;
            }

            return CachedTransform.position.y;
        }

        protected override bool OnWeaponShow(object userData)
        {
            _weaponData = RequireWeaponData<WeaponLanceData>(userData);
            if (_weaponData == null) return false;
            WeaponData = _weaponData;

            _currAttackTimer = 0f;
            _sqrRange = _weaponData.AttackRange * _weaponData.AttackRange;
            _cachedRotation = CachedTransform.rotation;

            WeaponLanceParamsData paramsData = _weaponData.ParamsData;
            float configuredHalfWidth = paramsData != null && paramsData.HitHalfWidth > 0f
                ? paramsData.HitHalfWidth
                : paramsData != null && paramsData.HitRadius > 0f
                    ? paramsData.HitRadius
                    : 0.45f;
            _hitHalfWidth = Mathf.Max(0.1f, configuredHalfWidth);
            _hitHeight = paramsData != null && paramsData.HitHeight > 0f
                ? Mathf.Max(0.2f, paramsData.HitHeight)
                : 4f;
            _hitCenterYOffset = paramsData != null ? paramsData.HitCenterYOffset : 0f;
            _pierceLength = paramsData != null && paramsData.PierceLength > 0f
                ? paramsData.PierceLength
                : paramsData != null && paramsData.ThrustDistance > 0f
                    ? paramsData.ThrustDistance
                    : _weaponData.AttackRange;
            _forwardOffset = paramsData != null && paramsData.ForwardOffset > 0f
                ? paramsData.ForwardOffset
                : 0f;

            if (paramsData != null && paramsData.RotateSpeed > 0f)
            {
                _rotateSpeed = paramsData.RotateSpeed;
            }

            if (paramsData != null && paramsData.AttackDuration > 0f)
            {
                _attackDuration = paramsData.AttackDuration;
            }

            if (paramsData != null && paramsData.ReturnDuration > 0f)
            {
                _returnDuration = paramsData.ReturnDuration;
            }

            int colliderCapacity = Mathf.Max(1, _maxHitColliders);
            if (_hitResults == null || _hitResults.Length != colliderCapacity)
            {
                _hitResults = new Collider[colliderCapacity];
            }

            _attackEffect = new LanceThrustAttackEffect();

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
                CachedTransform.rotation = _cachedRotation;
            }

            _attackParent = null;
            _hitEntityIds.Clear();
        }

        public float HitHalfWidth => _hitHalfWidth;
        public float PierceLength => _pierceLength;
        public Vector3 StrikeDirection => _strikeDirection;
    }
}
