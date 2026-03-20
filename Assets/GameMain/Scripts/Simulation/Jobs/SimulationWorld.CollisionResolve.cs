using Components;
using CustomDebugger;
using CustomUtility;
using Definition.DataStruct;
using Definition.Enum;
using Entity;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        #region Collision Resolve

        private void ResolveCollisionCandidatesOnMainThread()
        {
            if (!_collisionCandidates.IsCreated)
            {
                _lastResolvedAreaHitCount = 0;
                ClearAreaCollisionTransientBuffers();
                return;
            }

            _projectileResolvedEntityIds.Clear();
            _areaCollisionHitEvents.Clear();
            _areaCollisionHitDedupKeys.Clear();

            if (_collisionCandidates.Length == 0)
            {
                _lastResolvedAreaHitCount = 0;
                ClearAreaCollisionTransientBuffers();
                return;
            }

            using (CustomProfilerMarker.Collision_ResolveProjectile.Auto())
            {
                for (int i = 0; i < _collisionCandidates.Length; i++)
                {
                    CollisionCandidateData candidate = _collisionCandidates[i];
                    if (candidate.SourceType == CollisionSourceTypeProjectile)
                    {
                        int projectileEntityId = candidate.SourceEntityId;
                        if (_projectileResolvedEntityIds.Contains(projectileEntityId))
                        {
                            continue;
                        }

                        if (!TryGetActiveProjectileSimData(projectileEntityId, out _, out ProjectileSimData projectile))
                        {
                            _projectileResolvedEntityIds.Add(projectileEntityId);
                            continue;
                        }

                        bool shouldExpireProjectile = true;
                        bool shouldDispatchPresentation = false;
                        int damage = 0;
                        Vector3 hitPosition = projectile.Position;
                        if (TryGetAliveTargetableEntity(candidate.TargetEntityId, out TargetableObject target))
                        {
                            EntityBase sourceEntity = TryGetEntityById(candidate.SourceEntityId);
                            EntityBase ownerEntity = TryGetEntityById(candidate.SourceOwnerEntityId);
                            shouldExpireProjectile = ResolveProjectileHitAgainstTarget(target, sourceEntity,
                                ownerEntity,
                                in projectile,
                                out damage, out hitPosition, out shouldDispatchPresentation);
                        }

                        if (shouldDispatchPresentation)
                        {
                            DispatchProjectileHitPresentationEvent(projectileEntityId, candidate.SourceEntityId,
                                candidate.SourceOwnerEntityId, candidate.TargetEntityId, damage, in hitPosition);
                        }

                        if (shouldExpireProjectile)
                        {
                            MarkProjectileAsExpired(projectileEntityId);
                            _projectileResolvedEntityIds.Add(projectileEntityId);
                        }

                        continue;
                    }

                    if (candidate.SourceType == CollisionSourceTypeArea)
                    {
                        long dedupKey = (((long)candidate.QueryId) << 32) ^ (uint)candidate.TargetEntityId;
                        if (!_areaCollisionHitDedupKeys.Add(dedupKey))
                        {
                            continue;
                        }

                        _areaCollisionHitEvents.Add(new AreaCollisionHitEventData
                        {
                            QueryId = candidate.QueryId,
                            SourceEntityId = candidate.SourceEntityId,
                            SourceOwnerEntityId = candidate.SourceOwnerEntityId,
                            TargetEntityId = candidate.TargetEntityId,
                            SqrDistance = candidate.SqrDistance
                        });
                    }
                }
            }

            int resolvedAreaHitCount;
            using (CustomProfilerMarker.Collision_ResolveArea.Auto())
            {
                resolvedAreaHitCount = ResolveAreaCollisionHitsOnMainThread();
            }

            _lastResolvedAreaHitCount = resolvedAreaHitCount;
            _projectileResolvedEntityIds.Clear();
            ClearAreaCollisionTransientBuffers();
        }

        private bool ResolveProjectileHitAgainstTarget(TargetableObject target, EntityBase sourceEntity,
            EntityBase ownerEntity,
            in ProjectileSimData projectile, out int damage, out Vector3 hitPosition,
            out bool shouldDispatchPresentation)
        {
            damage = 0;
            hitPosition = projectile.Position;
            shouldDispatchPresentation = false;

            if (target == null || !target.Available || target.IsDead)
            {
                return true;
            }

            if (target.CachedTransform != null)
            {
                hitPosition = target.CachedTransform.position;
            }

            if (!TryResolveImpactSource(sourceEntity, ownerEntity, out EntityBase attacker,
                    out ImpactData sourceImpact))
            {
                shouldDispatchPresentation = true;
                return true;
            }

            ImpactData targetImpact = target.GetImpactData();
            if (AIUtility.GetRelation(targetImpact.Camp, sourceImpact.Camp) == RelationType.Friendly)
            {
                return false;
            }

            damage = AIUtility.CalcDamageHP(sourceImpact.AttackBase, sourceImpact.AttackStat,
                targetImpact.DefenseStat,
                targetImpact.DodgeStat);
            shouldDispatchPresentation = true;
            if (damage <= 0)
            {
                return true;
            }

            target.ApplyDamage(attacker ?? sourceEntity ?? ownerEntity, damage);
            return true;
        }

        private int ResolveAreaCollisionHitsOnMainThread()
        {
            if (_areaCollisionHitEvents.Count == 0)
            {
                return 0;
            }

            int resolvedHitCount = 0;
            for (int i = 0; i < _areaCollisionHitEvents.Count; i++)
            {
                AreaCollisionHitEventData hitEvent = _areaCollisionHitEvents[i];
                if (!TryGetCollisionQueryByQueryId(hitEvent.QueryId, out CollisionQueryData query) ||
                    query.SourceType != CollisionSourceTypeArea)
                {
                    continue;
                }

                if (!query.SourceWasActiveAtQueryTime)
                {
                    continue;
                }

                EntityBase sourceEntity = TryGetEntityById(hitEvent.SourceEntityId);
                if (sourceEntity == null || !sourceEntity.Available)
                {
                    continue;
                }

                if (!TryGetAliveTargetableEntity(hitEvent.TargetEntityId, out TargetableObject target))
                {
                    continue;
                }

                float targetRadius = ResolveAreaTargetRadius(target);
                if (!IsAreaTargetInsidePreciseShape(in query, target, targetRadius))
                {
                    continue;
                }

                AIUtility.PerformCollision(target, sourceEntity, true);
                resolvedHitCount++;
            }

            return resolvedHitCount;
        }

        private bool TryGetCollisionQueryByQueryId(int queryId, out CollisionQueryData query)
        {
            query = default;
            if (!_collisionQueryInputs.IsCreated || queryId < 0 || queryId >= _collisionQueryInputs.Length)
            {
                return false;
            }

            CollisionQueryData direct = _collisionQueryInputs[queryId];
            if (direct.QueryId == queryId)
            {
                query = direct;
                return true;
            }

            for (int i = 0; i < _collisionQueryInputs.Length; i++)
            {
                CollisionQueryData candidate = _collisionQueryInputs[i];
                if (candidate.QueryId != queryId)
                {
                    continue;
                }

                query = candidate;
                return true;
            }

            return false;
        }

        private float ResolveAreaTargetRadius(TargetableObject target)
        {
            if (target == null)
            {
                return 0f;
            }

            if (target is EnemyBase && TryGetEnemyData(target.Id, out EnemySimData enemyData))
            {
                return Mathf.Max(0f, enemyData.EnemyBodyRadius);
            }

            MovementComponent movementComponent = target.GetComponent<MovementComponent>();
            return movementComponent != null ? Mathf.Max(0f, movementComponent.EnemyBodyRadius) : 0f;
        }

        private static bool IsAreaTargetInsidePreciseShape(in CollisionQueryData query, TargetableObject target,
            float targetRadius)
        {
            if (target == null || target.CachedTransform == null)
            {
                return false;
            }

            Vector3 center = new Vector3(query.Position.x, query.Position.y, query.Position.z);
            Vector3 toTarget = target.CachedTransform.position - center;
            toTarget.y = 0f;

            float radius = Mathf.Max(0.01f, query.Radius + Mathf.Max(0f, targetRadius));
            float radiusSqr = radius * radius;
            float sqrDistance = toTarget.sqrMagnitude;
            if (sqrDistance > radiusSqr)
            {
                return false;
            }

            if (query.ShapeType == CollisionShapeRectangle)
            {
                Vector3 forwardRect = new Vector3(query.Direction.x, query.Direction.y, query.Direction.z);
                forwardRect.y = 0f;
                if (forwardRect.sqrMagnitude <= Mathf.Epsilon)
                {
                    forwardRect = Vector3.forward;
                }
                else
                {
                    forwardRect.Normalize();
                }

                Vector3 rightRect = Vector3.Cross(Vector3.up, forwardRect);
                float halfWidth = Mathf.Max(0.01f, query.HalfWidth + Mathf.Max(0f, targetRadius));
                float halfLength = Mathf.Max(0.01f, query.HalfLength + Mathf.Max(0f, targetRadius));
                float forwardDistance = Vector3.Dot(toTarget, forwardRect);
                float lateralDistance = Vector3.Dot(toTarget, rightRect);
                return Mathf.Abs(forwardDistance) <= halfLength && Mathf.Abs(lateralDistance) <= halfWidth;
            }

            if (query.ShapeType != CollisionShapeSector)
            {
                return true;
            }

            if (sqrDistance <= Mathf.Epsilon)
            {
                return true;
            }

            Vector3 forward = new Vector3(query.Direction.x, query.Direction.y, query.Direction.z);
            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            float halfAngle = Mathf.Clamp(query.HalfAngleDeg, 0f, 180f);
            float angle = Vector3.Angle(forward, toTarget.normalized);
            return angle <= halfAngle;
        }

        private bool TryGetActiveProjectileSimData(int projectileEntityId, out int simulationIndex,
            out ProjectileSimData projectile)
        {
            simulationIndex = -1;
            projectile = default;

            if (!ProjectileBinding.TryGetSimulationIndex(projectileEntityId, out int foundIndex) || foundIndex < 0 ||
                foundIndex >= _projectiles.Count)
            {
                return false;
            }

            ProjectileSimData data = _projectiles[foundIndex];
            if (!data.Active || data.State != ProjectileStateActive)
            {
                return false;
            }

            simulationIndex = foundIndex;
            projectile = data;
            return true;
        }

        private bool MarkProjectileAsExpired(int projectileEntityId)
        {
            if (!ProjectileBinding.TryGetSimulationIndex(projectileEntityId, out int simulationIndex) ||
                simulationIndex < 0 || simulationIndex >= _projectiles.Count)
            {
                return false;
            }

            ProjectileSimData projectile = _projectiles[simulationIndex];
            projectile.Active = false;
            projectile.State = ProjectileStateExpired;
            projectile.RemainingLifetime = 0f;
            if (projectile.LifeTime > 0f && projectile.Age < projectile.LifeTime)
            {
                projectile.Age = projectile.LifeTime;
            }

            _projectiles[simulationIndex] = projectile;
            return true;
        }

        #endregion
    }
}
