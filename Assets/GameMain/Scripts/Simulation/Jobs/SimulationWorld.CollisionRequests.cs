using Entity;
using Entity.Weapon;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        #region Collision Requests

        public bool TryRequestAreaCollision(int sourceEntityId, int sourceOwnerEntityId, in Vector3 center,
            float radius, int maxTargets = 16)
        {
            return TryRequestAreaCollisionInternal(sourceEntityId, sourceOwnerEntityId, in center, radius,
                maxTargets, CollisionShapeCircle, Vector3.forward, 180f, 0f, 0f);
        }

        public bool TryRequestSectorCollision(int sourceEntityId, int sourceOwnerEntityId, in Vector3 center,
            float radius, in Vector3 direction, float halfAngleDeg, int maxTargets = 16)
        {
            return TryRequestAreaCollisionInternal(sourceEntityId, sourceOwnerEntityId, in center, radius,
                maxTargets, CollisionShapeSector, direction, halfAngleDeg, 0f, 0f);
        }

        public bool TryRequestRectangleCollision(int sourceEntityId, int sourceOwnerEntityId, in Vector3 center,
            float halfWidth, float halfLength, in Vector3 direction, int maxTargets = 16)
        {
            float safeHalfWidth = Mathf.Max(0.01f, halfWidth);
            float safeHalfLength = Mathf.Max(0.01f, halfLength);
            float boundingRadius = Mathf.Sqrt(safeHalfWidth * safeHalfWidth + safeHalfLength * safeHalfLength);
            return TryRequestAreaCollisionInternal(sourceEntityId, sourceOwnerEntityId, in center, boundingRadius,
                maxTargets, CollisionShapeRectangle, direction, 0f, safeHalfWidth, safeHalfLength);
        }

        private bool TryRequestAreaCollisionInternal(int sourceEntityId, int sourceOwnerEntityId,
            in Vector3 center, float radius, int maxTargets, int shapeType, in Vector3 direction, float halfAngleDeg,
            float halfWidth, float halfLength)
        {
            if (!_useSimulationMovement)
            {
                return false;
            }

            if (sourceEntityId == 0 || radius <= 0f || maxTargets <= 0)
            {
                return false;
            }

            int resolvedOwnerEntityId = sourceOwnerEntityId != 0 ? sourceOwnerEntityId : sourceEntityId;
            bool sourceWasActiveAtQueryTime = WasCollisionSourceActiveAtQueryTime(sourceEntityId);
            Vector3 normalizedDirection = direction;
            normalizedDirection.y = 0f;
            if (normalizedDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                normalizedDirection = Vector3.forward;
            }
            else
            {
                normalizedDirection.Normalize();
            }

            _areaCollisionRequests.Add(new AreaCollisionRequestData
            {
                SourceEntityId = sourceEntityId,
                SourceOwnerEntityId = resolvedOwnerEntityId,
                SourceWasActiveAtQueryTime = sourceWasActiveAtQueryTime,
                Center = center,
                Radius = Mathf.Max(0.01f, radius),
                MaxTargets = Mathf.Max(1, maxTargets),
                ShapeType = shapeType,
                Direction = normalizedDirection,
                HalfAngleDeg = Mathf.Clamp(halfAngleDeg, 0f, 180f),
                HalfWidth = Mathf.Max(0f, halfWidth),
                HalfLength = Mathf.Max(0f, halfLength)
            });

            return true;
        }

        private int GetPendingAreaCollisionRequestCount()
        {
            return _areaCollisionRequests.Count;
        }

        private int EstimatePendingAreaCollisionCandidateCountFromRequests()
        {
            int expectedCount = 0;
            for (int i = 0; i < _areaCollisionRequests.Count; i++)
            {
                expectedCount += Mathf.Max(1, _areaCollisionRequests[i].MaxTargets);
            }

            return expectedCount;
        }

        private void ClearAreaCollisionTransientBuffers()
        {
            _areaCollisionRequests.Clear();
            _areaCollisionHitEvents.Clear();
            _areaCollisionHitDedupKeys.Clear();
        }

        private static bool WasCollisionSourceActiveAtQueryTime(int sourceEntityId)
        {
            EntityBase sourceEntity = TryGetEntityById(sourceEntityId);
            if (sourceEntity == null || !sourceEntity.Available)
            {
                return false;
            }

            if (sourceEntity is WeaponBase weapon)
            {
                return weapon.IsAttacking;
            }

            if (sourceEntity is EnemyProjectile projectile)
            {
                return projectile.IsActive;
            }

            return true;
        }

        #endregion
    }
}
