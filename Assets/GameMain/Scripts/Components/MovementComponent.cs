using System;
using CustomUtility;
using Definition.DataStruct;
using Definition.Enum;
using UnityEngine;
using CustomDebugger;

namespace Components
{
    public class MovementComponent : MonoBehaviour
    {
        [SerializeField] private bool _isMoving;
        [SerializeField] private Vector3 _direction;
        [SerializeField] private Transform _cachedTransform;
        [SerializeField] private bool _avoidEnemyOverlap;
        [SerializeField] private float _enemyBodyRadius = 0.45f;
        [SerializeField] private int _separationIterations = 2;

        public float Speed => (_speedBase + _movementStat.Value) * _movementStat.Percent;
        public bool AvoidEnemyOverlap => _avoidEnemyOverlap;
        public float EnemyBodyRadius => _enemyBodyRadius;
        public int SeparationIterations => _separationIterations;
        [SerializeField] private float _speedBase;

        private StatComponent _statComponent;

        private StatProperty _movementStat;
        private Action<StatModifier, bool> _movementStatCallback;

        public void OnInit(float speed, Transform target, StatComponent statComponent = null,
            bool avoidEnemyOverlap = false, float enemyBodyRadius = 0.45f, int separationIterations = 2)
        {
            _speedBase = speed;
            _cachedTransform = target;
            _direction = Vector3.forward;
            _avoidEnemyOverlap = avoidEnemyOverlap;
            _enemyBodyRadius = Mathf.Max(0.01f, enemyBodyRadius);
            _separationIterations = Mathf.Max(1, separationIterations);

            _statComponent = statComponent;
            if (_statComponent != null)
            {
                _movementStat = _statComponent.GetStat(StatType.MovementSpeed);
                _movementStatCallback = (modifier, isApply) =>
                    _statComponent.UpdateStat(_movementStat, modifier, isApply);
                _statComponent.Subscribe(StatType.MovementSpeed, _movementStatCallback);
            }
            else
            {
                _movementStat = new StatProperty();
            }

            RefreshEnemyRegistration();
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (_isMoving && _cachedTransform != null)
            {
                Move(elapseSeconds);
            }
        }

        public void OnReset()
        {
            Transform transformToUnregister = _cachedTransform;

            _speedBase = 0;
            _cachedTransform = null;
            _direction = Vector3.zero;
            _isMoving = false;
            _avoidEnemyOverlap = false;
            _enemyBodyRadius = 0.45f;
            _separationIterations = 2;

            if (_statComponent != null)
            {
                _statComponent.Unsubscribe(StatType.MovementSpeed, _movementStatCallback);
                _movementStatCallback = null;
            }

            _statComponent = null;

            UnregisterEnemyMover(transformToUnregister);
        }

        private void Move(float deltaTime = 0)
        {
            using (CustomProfilerMarker.Movement_Update.Auto())
            {
                if (_cachedTransform == null) return;

                Vector3 displacement = Speed * deltaTime * _direction;
                Vector3 nextPosition = _cachedTransform.position + displacement;
                if (_avoidEnemyOverlap)
                {
                    nextPosition = EnemySeparationSolverProvider.Resolve(
                        _cachedTransform,
                        nextPosition,
                        _direction,
                        _separationIterations);
                }

                _cachedTransform.position = nextPosition;
            }
        }

        public void SetMove(bool isMoving) => _isMoving = isMoving;
        public void SetDirection(Vector3 direction) => _direction = direction;

        private void RefreshEnemyRegistration()
        {
            UnregisterEnemyMover();
            if (!_avoidEnemyOverlap) return;
            EnemySeparationSolverProvider.Register(_cachedTransform, _enemyBodyRadius);
        }

        private void UnregisterEnemyMover(Transform transform = null)
        {
            EnemySeparationSolverProvider.Unregister(transform ?? _cachedTransform);
        }
    }
}
