using Entity;
using Entity.EntityData;

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
            ClearPlayerMovementState();
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

        public void SyncEnemyMovementInput(int entityId, bool isMoving, in UnityEngine.Vector3 direction, float speed,
            bool avoidEnemyOverlap, float enemyBodyRadius, int separationIterations)
        {
            if (!EnemyBinding.TryGetSimulationIndex(entityId, out int simulationIndex) || simulationIndex < 0 ||
                simulationIndex >= _enemies.Count)
            {
                return;
            }

            EnemySimData enemyData = _enemies[simulationIndex];
            enemyData.Speed = isMoving ? UnityEngine.Mathf.Max(0f, speed) : 0f;
            enemyData.AvoidEnemyOverlap = avoidEnemyOverlap;
            enemyData.EnemyBodyRadius = UnityEngine.Mathf.Max(0.01f, enemyBodyRadius);
            enemyData.SeparationIterations = UnityEngine.Mathf.Max(1, separationIterations);

            UnityEngine.Vector3 planarDirection = direction;
            planarDirection.y = 0f;
            if (planarDirection.sqrMagnitude > UnityEngine.Mathf.Epsilon)
            {
                enemyData.Forward = planarDirection.normalized;
            }

            _enemies[simulationIndex] = enemyData;
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

        #endregion
    }
}
