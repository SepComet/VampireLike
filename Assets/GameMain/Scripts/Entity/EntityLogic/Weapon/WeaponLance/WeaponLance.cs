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

        // 枪尖判定半径。
        private float _hitRadius;
        // 从判定起点向前延伸的有效刺击长度。
        private float _pierceLength;
        // 判定起点相对武器当前位置的前移量。
        private float _forwardOffset;
        // 武器模型本身向前突刺的位移长度。
        private float _thrustDistance;

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
            _attackParent = CachedTransform.parent;
            CachedTransform.SetParent(null);

            Vector3 targetPos = CachedTransform.position + CachedTransform.forward * _thrustDistance;
            _attackSequence = DOTween.Sequence();
            _attackSequence.Append(CachedTransform.DOMove(targetPos, _attackDuration).SetEase(Ease.OutQuad));
            _attackSequence.AppendCallback(() =>
            {
                Vector3 strikeCenter = GetStrikeCenter();
                _attackEffect?.Play(this, strikeCenter, _target, _hitRadius);
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

        private Vector3 GetStrikeStart()
        {
            Vector3 forward = CachedTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            return CachedTransform.position + forward * _forwardOffset;
        }

        private Vector3 GetStrikeEnd()
        {
            Vector3 start = GetStrikeStart();
            Vector3 forward = CachedTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            return start + forward * _pierceLength;
        }

        private Vector3 GetStrikeCenter()
        {
            return Vector3.Lerp(GetStrikeStart(), GetStrikeEnd(), 0.5f);
        }

        private void ApplyPierceDamage()
        {
            if (_hitRadius <= 0f || _pierceLength <= 0f) return;

            Vector3 strikeStart = GetStrikeStart();
            Vector3 strikeEnd = GetStrikeEnd();
            Vector3 strikeDirection = strikeEnd - strikeStart;
            strikeDirection.y = 0f;
            if (strikeDirection.sqrMagnitude <= Mathf.Epsilon) return;

            strikeDirection.Normalize();
            float halfAngle = Mathf.Rad2Deg * Mathf.Atan2(_hitRadius, Mathf.Max(0.01f, _pierceLength));
            if (TryQueueSectorCollisionQuery(strikeStart, _pierceLength, in strikeDirection, halfAngle,
                    Mathf.Max(1, _maxHitColliders)))
            {
                _hitEntityIds.Clear();
                return;
            }

            int capacity = Mathf.Max(1, _maxHitColliders);
            float broadPhaseRadius = _pierceLength * 0.5f + _hitRadius;
            Vector3 broadPhaseCenter = Vector3.Lerp(strikeStart, strikeEnd, 0.5f);

            if (_hitResults == null || _hitResults.Length != capacity)
            {
                _hitResults = new Collider[capacity];
            }

            int hitCount = Physics.OverlapSphereNonAlloc(broadPhaseCenter, broadPhaseRadius, _hitResults, _hitMask,
                QueryTriggerInteraction.Collide);

            _hitEntityIds.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = _hitResults[i];
                if (collider == null) continue;

                TargetableObject targetable = collider.GetComponentInParent<TargetableObject>();
                if (targetable == null || !targetable.Available || targetable.IsDead) continue;
                if (!_hitEntityIds.Add(targetable.Id)) continue;

                if (!IsTargetInsidePierce(targetable, strikeStart, strikeDirection)) continue;

                AIUtility.PerformCollision(targetable, this);
            }
        }

        private bool IsTargetInsidePierce(TargetableObject targetable, Vector3 strikeStart, Vector3 strikeDirection)
        {
            Vector3 toTarget = targetable.CachedTransform.position - strikeStart;
            toTarget.y = 0f;

            float projection = Vector3.Dot(toTarget, strikeDirection);
            if (projection < 0f || projection > _pierceLength) return false;

            Vector3 closestPoint = strikeStart + strikeDirection * projection;
            Vector3 delta = targetable.CachedTransform.position - closestPoint;
            delta.y = 0f;
            return delta.sqrMagnitude <= _hitRadius * _hitRadius;
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
            _hitRadius = paramsData != null && paramsData.HitRadius > 0f
                ? Mathf.Max(0.1f, paramsData.HitRadius)
                : 0.45f;
            _thrustDistance = paramsData != null && paramsData.ThrustDistance > 0f
                ? paramsData.ThrustDistance
                : _weaponData.AttackRange;
            _pierceLength = paramsData != null && paramsData.PierceLength > 0f
                ? paramsData.PierceLength
                : Mathf.Max(_weaponData.AttackRange, _thrustDistance);
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

            _attackEffect = new KnifeRangeAttackEffect();

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
    }
}
