using System;
using Definition.DataStruct;
using Definition.Enum;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Components
{
    public class MovementComponent : MonoBehaviour
    {
        [SerializeField] private bool _isMoving;
        [SerializeField] private Vector3 _direction;
        [SerializeField] private Transform _cachedTransform;

        public float Speed => (_speedBase + _movementStat.Value) * _movementStat.Percent;
        [SerializeField] private float _speedBase;

        private StatComponent _statComponent;

        private StatProperty _movementStat;
        private Action<StatModifier, bool> _movementStatCallback;

        public void OnInit(float speed, Transform target, StatComponent statComponent = null)
        {
            _speedBase = speed;
            _cachedTransform = target;
            _direction = Vector3.forward;

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
            _speedBase = 0;
            _cachedTransform = null;
            _direction = Vector3.zero;
            _isMoving = false;

            if (_statComponent != null)
            {
                _statComponent.Unsubscribe(StatType.MovementSpeed, _movementStatCallback);
                _movementStatCallback = null;
            }

            _statComponent = null;
        }

        private void Move(float deltaTime = 0)
        {
            this.transform.Translate(Speed * deltaTime * _direction);
        }

        public void SetMove(bool isMoving) => _isMoving = isMoving;
        public void SetDirection(Vector3 direction) => _direction = direction;
    }
}