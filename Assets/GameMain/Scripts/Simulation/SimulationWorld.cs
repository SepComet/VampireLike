using System.Collections.Generic;
using Components;
using CustomDebugger;
using CustomUtility;
using Entity;
using Entity.EntityData;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Simulation
{
    public sealed partial class SimulationWorld : GameFrameworkComponent
    {
        private const float DefaultAttackRange = 1f;
        private const int EnemyStateIdle = 0;
        private const int EnemyStateChasing = 1;
        private const int EnemyStateInAttackRange = 2;
        
        private struct EnemyTickWorkItem
        {
            public int EntityId;
            public Vector3 CurrentPosition;
            public Vector3 DesiredPosition;
            public Vector3 ToPlayer;
            public Vector3 Forward;
            public Quaternion Rotation;
            public float SqrDistanceToPlayer;
            public float AttackRangeSqr;
            public float Speed;
            public int SeparationIterations;
            public bool AvoidEnemyOverlap;
            public bool CanChase;
            public bool HasRotationUpdate;
            public int NextState;
        }

        [SerializeField] private bool _useSimulationMovement;
        [SerializeField] private bool _useJobSimulation;
        [SerializeField] private bool _useBurstJobs = true;

        private EntitySync _entitySync;
        private Presentation _presentation;

        private readonly List<EnemySimData> _enemies = new List<EnemySimData>();
        private readonly List<ProjectileSimData> _projectiles = new List<ProjectileSimData>();
        private readonly List<PickupSimData> _pickups = new List<PickupSimData>();
        private readonly List<EnemySeparationAgent> _enemySeparationAgents = new List<EnemySeparationAgent>();
        private readonly List<EnemyTickWorkItem> _enemyTickWorkItems = new List<EnemyTickWorkItem>();

        private EntityBinding EnemyBinding { get; } = new EntityBinding();
        private EntityBinding ProjectileBinding { get; } = new EntityBinding();
        private EntityBinding PickupBinding { get; } = new EntityBinding();

        public IReadOnlyList<EnemySimData> Enemies => _enemies;
        public IReadOnlyList<ProjectileSimData> Projectiles => _projectiles;
        public IReadOnlyList<PickupSimData> Pickups => _pickups;
        public bool UseSimulationMovement => _useSimulationMovement;
        public bool UseJobSimulation => _useJobSimulation;
        public bool UseBurstJobs => _useBurstJobs;

        public void SetUseSimulationMovement(bool enabled)
        {
            _useSimulationMovement = enabled;
        }

        public void SetUseJobSimulation(bool enabled)
        {
            _useJobSimulation = enabled;
        }

        public void SetUseBurstJobs(bool enabled)
        {
            _useBurstJobs = enabled;
        }

        protected override void Awake()
        {
            base.Awake();
            _entitySync = new EntitySync(this);
            _presentation = new Presentation(this);
        }

        private void Start()
        {
            _entitySync?.OnStart();
        }

        private void OnDestroy()
        {
            _entitySync?.OnDestroy();
            _entitySync = null;
            _presentation = null;
        }

        private void LateUpdate()
        {
            _presentation?.OnLateUpdate();
        }

        private int AddEnemy(in EnemySimData simData)
        {
            int simulationIndex = _enemies.Count;
            _enemies.Add(simData);
            EnemyBinding.Bind(simData.EntityId, simulationIndex);
            return simulationIndex;
        }

        private int UpsertEnemy(in EnemySimData simData)
        {
            if (!EnemyBinding.TryGetSimulationIndex(simData.EntityId, out int simulationIndex))
            {
                return AddEnemy(simData);
            }

            _enemies[simulationIndex] = simData;
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
            EnemyBinding.UnbindByEntityId(entityId);
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

        private void RegisterProjectileLifecycle(EntityBase projectileEntity)
        {
            if (projectileEntity == null || projectileEntity.CachedTransform == null)
            {
                return;
            }

            UpsertProjectile(CreateProjectileInitialSimData(projectileEntity));
        }

        private void UnregisterProjectileLifecycle(int entityId)
        {
            RemoveProjectileByEntityId(entityId);
        }

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

        public void Tick(in SimulationTickContext context)
        {
            if (!_useSimulationMovement)
            {
                return;
            }

            if (_useJobSimulation)
            {
                // Checkpoint 1: the switch is in place; Checkpoint 3+ will replace this with jobified path.
                using (CustomProfilerMarker.TickEnemies.Auto())
                {
                    TickEnemies(in context);
                }

                return;
            }

            using (CustomProfilerMarker.TickEnemies.Auto())
            {
                TickEnemies(in context);
            }
        }

        public void Clear()
        {
            _enemies.Clear();
            _projectiles.Clear();
            _pickups.Clear();
            _enemySeparationAgents.Clear();
            _enemyTickWorkItems.Clear();

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

            using (CustomProfilerMarker.TickEnemies_BuildInput.Auto())
            {
                BuildEnemyTickInput(in playerPosition);
            }

            using (CustomProfilerMarker.TickEnemies_MoveSeparation.Auto())
            {
                MoveAndSeparateEnemies(context.DeltaTime);
            }

            using (CustomProfilerMarker.TickEnemies_StateUpdate.Auto())
            {
                UpdateEnemyStates();
            }

            using (CustomProfilerMarker.TickEnemies_WriteBack.Auto())
            {
                WriteBackEnemyTickResults();
            }
        }

        private void BuildEnemyTickInput(in Vector3 playerPosition)
        {
            _enemyTickWorkItems.Clear();
            _enemySeparationAgents.Clear();

            for (int i = 0; i < _enemies.Count; i++)
            {
                EnemySimData enemy = _enemies[i];

                Vector3 currentPosition = enemy.Position;
                Vector3 horizontalPosition = currentPosition;
                horizontalPosition.y = 0f;
                Vector3 toPlayer = playerPosition - horizontalPosition;
                float sqrDistance = toPlayer.sqrMagnitude;

                float attackRange = enemy.AttackRange > 0f ? enemy.AttackRange : DefaultAttackRange;
                float attackRangeSqr = attackRange * attackRange;
                bool isInAttackRange = sqrDistance <= attackRangeSqr;
                bool canChase = !isInAttackRange && enemy.Speed > 0f && sqrDistance > float.Epsilon;

                EnemyTickWorkItem workItem = new EnemyTickWorkItem
                {
                    EntityId = enemy.EntityId,
                    CurrentPosition = currentPosition,
                    DesiredPosition = currentPosition,
                    ToPlayer = toPlayer,
                    Forward = enemy.Forward,
                    Rotation = enemy.Rotation,
                    SqrDistanceToPlayer = sqrDistance,
                    AttackRangeSqr = attackRangeSqr,
                    Speed = enemy.Speed,
                    SeparationIterations = enemy.SeparationIterations > 0 ? enemy.SeparationIterations : 1,
                    AvoidEnemyOverlap = enemy.AvoidEnemyOverlap,
                    CanChase = canChase,
                    HasRotationUpdate = false,
                    NextState = EnemyStateIdle
                };
                _enemyTickWorkItems.Add(workItem);

                if (!enemy.AvoidEnemyOverlap) continue;

                _enemySeparationAgents.Add(new EnemySeparationAgent
                {
                    AgentId = enemy.EntityId,
                    Position = horizontalPosition,
                    Radius = enemy.EnemyBodyRadius > 0f ? enemy.EnemyBodyRadius : 0.45f
                });
            }
        }

        private void MoveAndSeparateEnemies(float deltaTime)
        {
            EnemySeparationSolverProvider.SetSimulationAgents(_enemySeparationAgents);

            for (int i = 0; i < _enemyTickWorkItems.Count; i++)
            {
                EnemyTickWorkItem workItem = _enemyTickWorkItems[i];
                if (!workItem.CanChase)
                {
                    _enemyTickWorkItems[i] = workItem;
                    continue;
                }

                Vector3 forward = workItem.ToPlayer.normalized;
                Vector3 desiredPosition = workItem.CurrentPosition + forward * workItem.Speed * deltaTime;

                if (workItem.AvoidEnemyOverlap)
                {
                    desiredPosition = EnemySeparationSolverProvider.ResolveSimulation(
                        workItem.EntityId,
                        desiredPosition,
                        forward,
                        workItem.SeparationIterations);
                }

                workItem.Forward = forward;
                workItem.DesiredPosition = desiredPosition;

                if (forward.sqrMagnitude > float.Epsilon)
                {
                    workItem.Rotation = Quaternion.LookRotation(forward, Vector3.up);
                    workItem.HasRotationUpdate = true;
                }

                _enemyTickWorkItems[i] = workItem;
            }
        }

        private void UpdateEnemyStates()
        {
            for (int i = 0; i < _enemyTickWorkItems.Count; i++)
            {
                EnemyTickWorkItem workItem = _enemyTickWorkItems[i];

                if (workItem.SqrDistanceToPlayer <= workItem.AttackRangeSqr)
                {
                    workItem.NextState = EnemyStateInAttackRange;
                }
                else if (workItem.CanChase)
                {
                    workItem.NextState = EnemyStateChasing;
                }
                else
                {
                    workItem.NextState = EnemyStateIdle;
                }

                _enemyTickWorkItems[i] = workItem;
            }
        }

        private void WriteBackEnemyTickResults()
        {
            for (int i = 0; i < _enemyTickWorkItems.Count; i++)
            {
                EnemySimData enemy = _enemies[i];
                EnemyTickWorkItem workItem = _enemyTickWorkItems[i];

                if (workItem.CanChase)
                {
                    enemy.Forward = workItem.Forward;
                    enemy.Position = workItem.DesiredPosition;
                    if (workItem.HasRotationUpdate)
                    {
                        enemy.Rotation = workItem.Rotation;
                    }
                }

                enemy.State = workItem.NextState;
                _enemies[i] = enemy;
            }
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

            return new EnemySimData
            {
                EntityId = enemy.Id,
                Position = enemyTransform.position,
                Forward = enemyTransform.forward,
                Rotation = enemyTransform.rotation,
                Speed = speed,
                AttackRange = 1f,
                AvoidEnemyOverlap = movementComponent != null && movementComponent.AvoidEnemyOverlap,
                EnemyBodyRadius = movementComponent != null ? movementComponent.EnemyBodyRadius : 0.45f,
                SeparationIterations = movementComponent != null ? movementComponent.SeparationIterations : 2,
                TargetType = 0,
                State = EnemyStateIdle
            };
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

        private static ProjectileSimData CreateProjectileInitialSimData(EntityBase projectileEntity)
        {
            return new ProjectileSimData
            {
                EntityId = projectileEntity.Id,
                OwnerEntityId = 0,
                Position = projectileEntity.CachedTransform.position,
                Forward = projectileEntity.CachedTransform.forward,
                Speed = 0f,
                RemainingLifetime = 0f,
                State = 0
            };
        }
    }
}
