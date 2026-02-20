using System.Collections.Generic;
using CustomUtility;
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

        [SerializeField] private bool _useSimulationMovement;

        private EntitySync _entitySync;
        private Presentation _presentation;

        private readonly List<EnemySimData> _enemies = new List<EnemySimData>();
        private readonly List<ProjectileSimData> _projectiles = new List<ProjectileSimData>();
        private readonly List<PickupSimData> _pickups = new List<PickupSimData>();
        private readonly Dictionary<int, Transform> _enemyTransforms = new Dictionary<int, Transform>();

        private EntityBinding EnemyBinding { get; } = new EntityBinding();
        private EntityBinding ProjectileBinding { get; } = new EntityBinding();
        private EntityBinding PickupBinding { get; } = new EntityBinding();

        public IReadOnlyList<EnemySimData> Enemies => _enemies;
        public IReadOnlyList<ProjectileSimData> Projectiles => _projectiles;
        public IReadOnlyList<PickupSimData> Pickups => _pickups;
        public bool UseSimulationMovement => _useSimulationMovement;

        public void SetUseSimulationMovement(bool enabled)
        {
            _useSimulationMovement = enabled;
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
            _enemyTransforms.Remove(entityId);
            return true;
        }

        private void RegisterEnemyTransform(int entityId, Transform transform)
        {
            if (transform == null)
            {
                _enemyTransforms.Remove(entityId);
                return;
            }

            _enemyTransforms[entityId] = transform;
        }

        private void UnregisterEnemyTransform(int entityId)
        {
            _enemyTransforms.Remove(entityId);
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
            _enemyTransforms.Clear();

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

            for (int i = 0; i < _enemies.Count; i++)
            {
                EnemySimData enemy = _enemies[i];
                _enemyTransforms.TryGetValue(enemy.EntityId, out Transform enemyTransform);

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
                    if (forward.sqrMagnitude > float.Epsilon)
                    {
                        enemy.Rotation = Quaternion.LookRotation(forward, Vector3.up);
                    }
                }

                _enemies[i] = enemy;
            }
        }
    }
}