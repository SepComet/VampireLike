using Components;
using Entity;
using Entity.EntityData;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        #region Simulation State Lifecycle

        public void ClearSimulationState()
        {
            _enemies.Clear();
            _projectiles.Clear();
            _pickups.Clear();
            _projectileRecycleEntityIds.Clear();
            _projectileResolvedEntityIds.Clear();
            _areaCollisionRequests.Clear();
            _areaCollisionHitEvents.Clear();
            _areaCollisionHitDedupKeys.Clear();
            ClearJobDataChannels();

            EnemyBinding.Clear();
            ProjectileBinding.Clear();
            PickupBinding.Clear();
        }

        #endregion

        #region Enemy Simulation State

        private int AddEnemy(in EnemySimData simData)
        {
            int simulationIndex = _enemies.Count;
            _enemies.Add(simData);
            EnemyBinding.Bind(simData.EntityId, simulationIndex);
            OnEnemyAddedToSeparationTemporalBuffers();
            MarkEnemyTargetSpatialIndexDirty();
            return simulationIndex;
        }

        private int UpsertEnemy(in EnemySimData simData)
        {
            if (!EnemyBinding.TryGetSimulationIndex(simData.EntityId, out int simulationIndex))
            {
                return AddEnemy(simData);
            }

            _enemies[simulationIndex] = simData;
            MarkEnemyTargetSpatialIndexDirty();
            return simulationIndex;
        }

        private bool RemoveEnemyByEntityId(int entityId)
        {
            if (!EnemyBinding.TryGetSimulationIndex(entityId, out int simulationIndex))
            {
                return false;
            }

            int lastIndex = _enemies.Count - 1;
            if (simulationIndex != lastIndex)
            {
                EnemySimData movedData = _enemies[lastIndex];
                _enemies[simulationIndex] = movedData;
                EnemyBinding.RemapIndex(movedData.EntityId, simulationIndex);
            }

            _enemies.RemoveAt(lastIndex);
            OnEnemyRemovedFromSeparationTemporalBuffers(simulationIndex);
            EnemyBinding.UnbindByEntityId(entityId);
            MarkEnemyTargetSpatialIndexDirty();
            return true;
        }

        private void RegisterEnemyLifecycle(EnemyBase enemy, object userData)
        {
            if (enemy == null || enemy.CachedTransform == null)
            {
                return;
            }

            EnemyData enemyData = userData as EnemyData;
            UpsertEnemy(CreateEnemyInitialSimData(enemy, enemyData));
        }

        private void UnregisterEnemyLifecycle(int entityId)
        {
            RemoveEnemyByEntityId(entityId);
        }

        private bool TryGetEnemyData(int entityId, out EnemySimData enemyData)
        {
            if (!EnemyBinding.TryGetSimulationIndex(entityId, out int simulationIndex) || simulationIndex < 0 ||
                simulationIndex >= _enemies.Count)
            {
                enemyData = default;
                return false;
            }

            enemyData = _enemies[simulationIndex];
            return true;
        }

        private static EnemySimData CreateEnemyInitialSimData(EnemyBase enemy, EnemyData enemyData)
        {
            Transform enemyTransform = enemy.CachedTransform;
            MovementComponent movementComponent = enemy.GetComponent<MovementComponent>();

            float speed = 0f;
            if (enemyData != null)
            {
                speed = enemyData.SpeedBase;
            }
            else if (movementComponent != null)
            {
                speed = movementComponent.Speed;
            }

            float attackRange = enemy != null && enemy.AttackRange > 0f
                ? enemy.AttackRange
                : DefaultAttackRange;

            return new EnemySimData
            {
                EntityId = enemy.Id,
                Position = enemyTransform.position,
                Forward = enemyTransform.forward,
                Rotation = enemyTransform.rotation,
                Speed = speed,
                AttackRange = attackRange,
                AvoidEnemyOverlap = movementComponent != null && movementComponent.AvoidEnemyOverlap,
                EnemyBodyRadius = movementComponent != null ? movementComponent.EnemyBodyRadius : 0.45f,
                SeparationIterations = movementComponent != null ? movementComponent.SeparationIterations : 2,
                TargetType = 0,
                State = EnemyStateIdle
            };
        }

        #endregion

        #region Projectile Simulation State

        private int AddProjectile(in ProjectileSimData simData)
        {
            int simulationIndex = _projectiles.Count;
            _projectiles.Add(simData);
            ProjectileBinding.Bind(simData.EntityId, simulationIndex);
            return simulationIndex;
        }

        private int UpsertProjectile(in ProjectileSimData simData)
        {
            if (!ProjectileBinding.TryGetSimulationIndex(simData.EntityId, out int simulationIndex))
            {
                return AddProjectile(simData);
            }

            _projectiles[simulationIndex] = simData;
            return simulationIndex;
        }

        private bool RemoveProjectileByEntityId(int entityId)
        {
            if (!ProjectileBinding.TryGetSimulationIndex(entityId, out int simulationIndex))
            {
                return false;
            }

            int lastIndex = _projectiles.Count - 1;
            if (simulationIndex != lastIndex)
            {
                ProjectileSimData movedData = _projectiles[lastIndex];
                _projectiles[simulationIndex] = movedData;
                ProjectileBinding.RemapIndex(movedData.EntityId, simulationIndex);
            }

            _projectiles.RemoveAt(lastIndex);
            ProjectileBinding.UnbindByEntityId(entityId);
            return true;
        }

        private void RegisterProjectileLifecycle(EntityBase projectileEntity, object userData)
        {
            if (projectileEntity == null || projectileEntity.CachedTransform == null)
            {
                return;
            }

            UpsertProjectile(CreateProjectileInitialSimData(projectileEntity, userData));
        }

        private void UnregisterProjectileLifecycle(int entityId)
        {
            RemoveProjectileByEntityId(entityId);
        }

        private static ProjectileSimData CreateProjectileInitialSimData(EntityBase projectileEntity, object userData)
        {
            Vector3 forward = projectileEntity.CachedTransform.forward;
            int ownerEntityId = 0;
            Vector3 velocity = Vector3.zero;
            float speed = 0f;
            float lifeTime = 0f;

            if (userData is EnemyProjectileData enemyProjectileData)
            {
                ownerEntityId = enemyProjectileData.OwnerEntityId;

                Vector3 direction = enemyProjectileData.Direction;
                direction.y = 0f;
                if (direction.sqrMagnitude > Mathf.Epsilon)
                {
                    direction.Normalize();
                    forward = direction;
                }
                else if (forward.sqrMagnitude > Mathf.Epsilon)
                {
                    forward = forward.normalized;
                }
                else
                {
                    forward = Vector3.forward;
                }

                speed = Mathf.Max(0f, enemyProjectileData.Speed);
                velocity = forward * speed;
                lifeTime = Mathf.Max(0f, enemyProjectileData.LifeTime);
            }

            return new ProjectileSimData
            {
                EntityId = projectileEntity.Id,
                OwnerEntityId = ownerEntityId,
                Position = projectileEntity.CachedTransform.position,
                Forward = forward,
                Velocity = velocity,
                Speed = speed,
                LifeTime = lifeTime,
                Age = 0f,
                Active = true,
                RemainingLifetime = lifeTime,
                State = ProjectileStateActive
            };
        }

        #endregion

        #region Pickup Simulation State

        private int AddPickup(in PickupSimData simData)
        {
            int simulationIndex = _pickups.Count;
            _pickups.Add(simData);
            PickupBinding.Bind(simData.EntityId, simulationIndex);
            return simulationIndex;
        }

        private int UpsertPickup(in PickupSimData simData)
        {
            if (!PickupBinding.TryGetSimulationIndex(simData.EntityId, out int simulationIndex))
            {
                return AddPickup(simData);
            }

            _pickups[simulationIndex] = simData;
            return simulationIndex;
        }

        private bool RemovePickupByEntityId(int entityId)
        {
            if (!PickupBinding.TryGetSimulationIndex(entityId, out int simulationIndex))
            {
                return false;
            }

            int lastIndex = _pickups.Count - 1;
            if (simulationIndex != lastIndex)
            {
                PickupSimData movedData = _pickups[lastIndex];
                _pickups[simulationIndex] = movedData;
                PickupBinding.RemapIndex(movedData.EntityId, simulationIndex);
            }

            _pickups.RemoveAt(lastIndex);
            PickupBinding.UnbindByEntityId(entityId);
            return true;
        }

        private void RegisterPickupLifecycle(EntityBase pickupEntity)
        {
            if (pickupEntity == null || pickupEntity.CachedTransform == null)
            {
                return;
            }

            UpsertPickup(CreatePickupInitialSimData(pickupEntity));
        }

        private void UnregisterPickupLifecycle(int entityId)
        {
            RemovePickupByEntityId(entityId);
        }

        private static PickupSimData CreatePickupInitialSimData(EntityBase pickupEntity)
        {
            return new PickupSimData
            {
                EntityId = pickupEntity.Id,
                Position = pickupEntity.CachedTransform.position,
                PickupRadius = 0.35f,
                State = 0
            };
        }

        #endregion
    }
}
