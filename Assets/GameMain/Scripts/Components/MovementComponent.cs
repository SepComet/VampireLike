using System;
using CustomUtility;
using Definition.DataStruct;
using Definition.Enum;
using Entity;
using Simulation;
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
        public bool IsMoving => _isMoving;
        public Vector3 Direction => _direction;
        public Transform CachedTransform => _cachedTransform;
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
                {
                    _statComponent.UpdateStat(_movementStat, modifier, isApply);
                    SyncToSimulationWorld();
                };
                _statComponent.Subscribe(StatType.MovementSpeed, _movementStatCallback);
            }
            else
            {
                _movementStat = new StatProperty();
            }

            SyncToSimulationWorld();
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            SyncToSimulationWorld();
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

            if (transformToUnregister != null)
            {
                var simulationWorld = GameEntry.SimulationWorld;
                simulationWorld?.UnregisterPlayerMovement(transformToUnregister);
            }
        }

        public void SetMove(bool isMoving)
        {
            _isMoving = isMoving;
            SyncToSimulationWorld();
        }

        public void SetDirection(Vector3 direction)
        {
            _direction = direction;
            SyncToSimulationWorld();
        }

        private void SyncToSimulationWorld()
        {
            using (CustomProfilerMarker.Movement_Update.Auto())
            {
                if (_cachedTransform == null)
                {
                    return;
                }

                SimulationWorld simulationWorld = GameEntry.SimulationWorld;
                if (simulationWorld == null)
                {
                    return;
                }

                Vector3 direction = _direction;
                direction.y = 0f;
                if (direction.sqrMagnitude > Mathf.Epsilon)
                {
                    direction.Normalize();
                }

                if (TryGetComponent(out Player _))
                {
                    simulationWorld.SyncPlayerMovementInput(_cachedTransform, _isMoving, direction, Speed);
                    return;
                }

                if (TryGetComponent(out EnemyBase enemy))
                {
                    simulationWorld.SyncEnemyMovementInput(enemy.Id, _isMoving, direction, Speed,
                        _avoidEnemyOverlap, _enemyBodyRadius, _separationIterations);
                }
            }
        }
    }
}
