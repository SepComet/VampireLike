using System.Collections.Generic;
using UnityGameFramework.Runtime;

namespace Simulation
{
    public sealed class SimulationWorld : GameFrameworkComponent
    {
        private readonly List<EnemySimData> _enemies = new List<EnemySimData>();
        private readonly List<ProjectileSimData> _projectiles = new List<ProjectileSimData>();
        private readonly List<PickupSimData> _pickups = new List<PickupSimData>();

        public EntityBinding EnemyBinding { get; } = new EntityBinding();
        public EntityBinding ProjectileBinding { get; } = new EntityBinding();
        public EntityBinding PickupBinding { get; } = new EntityBinding();

        public IReadOnlyList<EnemySimData> Enemies => _enemies;
        public IReadOnlyList<ProjectileSimData> Projectiles => _projectiles;
        public IReadOnlyList<PickupSimData> Pickups => _pickups;

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
            _ = context;
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
    }
}
