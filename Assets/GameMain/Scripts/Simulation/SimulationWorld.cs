using System.Collections.Generic;
using CustomUtility;
using Entity;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Simulation
{
    public sealed class SimulationWorld : GameFrameworkComponent
    {
        private const float DefaultAttackRange = 1f;
        private const int EnemyStateIdle = 0;
        private const int EnemyStateChasing = 1;
        private const int EnemyStateInAttackRange = 2;

        [SerializeField] private bool _useSimulationMovement;

        private readonly List<EnemySimData> _enemies = new List<EnemySimData>();
        private readonly List<ProjectileSimData> _projectiles = new List<ProjectileSimData>();
        private readonly List<PickupSimData> _pickups = new List<PickupSimData>();

        public EntityBinding EnemyBinding { get; } = new EntityBinding();
        public EntityBinding ProjectileBinding { get; } = new EntityBinding();
        public EntityBinding PickupBinding { get; } = new EntityBinding();

        public IReadOnlyList<EnemySimData> Enemies => _enemies;
        public IReadOnlyList<ProjectileSimData> Projectiles => _projectiles;
        public IReadOnlyList<PickupSimData> Pickups => _pickups;
        public bool UseSimulationMovement => _useSimulationMovement;

        public void SetUseSimulationMovement(bool enabled)
        {
            _useSimulationMovement = enabled;
        }

        public int AddEnemy(in EnemySimData simData)
        {
            int simulationIndex = _enemies.Count;
            _enemies.Add(simData);
            EnemyBinding.Bind(simData.EntityId, simulationIndex);
            return simulationIndex;
        }

        public int UpsertEnemy(in EnemySimData simData)
        {
            if (EnemyBinding.TryGetSimulationIndex(simData.EntityId, out int simulationIndex))
            {
                _enemies[simulationIndex] = simData;
                return simulationIndex;
            }

            return AddEnemy(simData);
        }

        public bool RemoveEnemyByEntityId(int entityId)
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
            EnemyBinding.UnbindByEntityId(entityId);
            return true;
        }

        public int AddProjectile(in ProjectileSimData simData)
        {
            int simulationIndex = _projectiles.Count;
            _projectiles.Add(simData);
            ProjectileBinding.Bind(simData.EntityId, simulationIndex);
            return simulationIndex;
        }

        public int UpsertProjectile(in ProjectileSimData simData)
        {
            if (ProjectileBinding.TryGetSimulationIndex(simData.EntityId, out int simulationIndex))
            {
                _projectiles[simulationIndex] = simData;
                return simulationIndex;
            }

            return AddProjectile(simData);
        }

        public bool RemoveProjectileByEntityId(int entityId)
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

        public int AddPickup(in PickupSimData simData)
        {
            int simulationIndex = _pickups.Count;
            _pickups.Add(simData);
            PickupBinding.Bind(simData.EntityId, simulationIndex);
            return simulationIndex;
        }

        public int UpsertPickup(in PickupSimData simData)
        {
            if (PickupBinding.TryGetSimulationIndex(simData.EntityId, out int simulationIndex))
            {
                _pickups[simulationIndex] = simData;
                return simulationIndex;
            }

            return AddPickup(simData);
        }

        public bool RemovePickupByEntityId(int entityId)
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

        public void Tick(in SimulationTickContext context)
        {
            if (!_useSimulationMovement)
            {
                return;
            }

            TickEnemies(in context);
        }

        public void Clear()
        {
            _enemies.Clear();
            _projectiles.Clear();
            _pickups.Clear();

            EnemyBinding.Clear();
            ProjectileBinding.Clear();
            PickupBinding.Clear();
        }

        private void TickEnemies(in SimulationTickContext context)
        {
            if (_enemies.Count == 0 || context.DeltaTime <= 0f)
            {
                return;
            }

            Vector3 playerPosition = context.PlayerPosition;
            playerPosition.y = 0f;
            EntityComponent entityComponent = GameEntry.Entity;

            for (int i = 0; i < _enemies.Count; i++)
            {
                EnemySimData enemy = _enemies[i];
                EnemyBase enemyEntity = null;
                Transform enemyTransform = null;

                if (entityComponent != null &&
                    entityComponent.GetGameEntity(enemy.EntityId) is EnemyBase runtimeEnemy &&
                    runtimeEnemy.Available)
                {
                    enemyEntity = runtimeEnemy;
                    enemyTransform = runtimeEnemy.CachedTransform;
                }

                Vector3 currentPosition = enemy.Position;
                currentPosition.y = 0f;

                Vector3 toPlayer = playerPosition - currentPosition;
                float sqrDistance = toPlayer.sqrMagnitude;
                float attackRange = enemy.AttackRange > 0f ? enemy.AttackRange : DefaultAttackRange;
                float attackRangeSqr = attackRange * attackRange;

                if (sqrDistance <= attackRangeSqr)
                {
                    enemy.State = EnemyStateInAttackRange;
                }
                else if (enemy.Speed <= 0f || sqrDistance <= float.Epsilon)
                {
                    enemy.State = EnemyStateIdle;
                }
                else
                {
                    Vector3 forward = toPlayer.normalized;
                    enemy.Forward = forward;
                    Vector3 desiredPosition = enemy.Position + forward * enemy.Speed * context.DeltaTime;
                    if (enemy.AvoidEnemyOverlap && enemyTransform != null)
                    {
                        int separationIterations = enemy.SeparationIterations > 0 ? enemy.SeparationIterations : 1;
                        desiredPosition = EnemySeparationSolverProvider.Resolve(
                            enemyTransform,
                            desiredPosition,
                            forward,
                            separationIterations);
                    }

                    enemy.Position = desiredPosition;
                    enemy.State = EnemyStateChasing;
                }

                _enemies[i] = enemy;

                if (enemyEntity != null)
                {
                    ApplyEnemyPresentation(enemyEntity, enemy);
                }
            }
        }

        private static void ApplyEnemyPresentation(EnemyBase enemyEntity, in EnemySimData enemyData)
        {
            if (enemyEntity == null || !enemyEntity.Available)
            {
                return;
            }

            enemyEntity.CachedTransform.position = enemyData.Position;

            Vector3 forward = enemyData.Forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > float.Epsilon)
            {
                enemyEntity.CachedTransform.forward = forward.normalized;
            }
        }
    }
}
