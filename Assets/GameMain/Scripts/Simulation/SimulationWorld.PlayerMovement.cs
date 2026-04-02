using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        private Transform _playerMovementTransform;
        private Vector3 _playerMovementPosition;
        private Vector3 _playerMovementDirection = Vector3.zero;
        private float _playerMovementSpeed;
        private bool _playerMovementActive;

        public void SyncPlayerMovementInput(Transform playerTransform, bool isMoving, in Vector3 direction, float speed)
        {
            if (playerTransform == null)
            {
                ClearPlayerMovementState();
                return;
            }

            if (_playerMovementTransform != playerTransform)
            {
                _playerMovementTransform = playerTransform;
                _playerMovementPosition = playerTransform.position;
            }

            Vector3 planarDirection = direction;
            planarDirection.y = 0f;
            bool hasDirection = planarDirection.sqrMagnitude > Mathf.Epsilon;
            if (hasDirection)
            {
                _playerMovementDirection = planarDirection.normalized;
            }
            else
            {
                _playerMovementDirection = Vector3.zero;
            }

            _playerMovementActive = isMoving && hasDirection;
            _playerMovementSpeed = _playerMovementActive ? Mathf.Max(0f, speed) : 0f;
        }

        public void UnregisterPlayerMovement(Transform playerTransform)
        {
            if (playerTransform == null || _playerMovementTransform != playerTransform)
            {
                return;
            }

            ClearPlayerMovementState();
        }

        private Vector3 ResolvePlayerPositionForTick(in SimulationTickContext context)
        {
            if (_playerMovementTransform == null)
            {
                return context.PlayerPosition;
            }

            if (!_playerMovementActive || _playerMovementSpeed <= 0f ||
                _playerMovementDirection.sqrMagnitude <= Mathf.Epsilon || context.DeltaTime <= 0f)
            {
                _playerMovementPosition = _playerMovementTransform.position;
                return _playerMovementPosition;
            }

            _playerMovementPosition += _playerMovementDirection * (_playerMovementSpeed * context.DeltaTime);
            _playerMovementPosition.y = _playerMovementTransform.position.y;
            _playerMovementTransform.position = _playerMovementPosition;
            return _playerMovementPosition;
        }

        private void ClearPlayerMovementState()
        {
            _playerMovementTransform = null;
            _playerMovementPosition = Vector3.zero;
            _playerMovementDirection = Vector3.zero;
            _playerMovementSpeed = 0f;
            _playerMovementActive = false;
        }
    }
}
