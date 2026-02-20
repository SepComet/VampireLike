using System.Collections.Generic;

namespace Simulation
{
    public sealed class EntityBinding
    {
        private readonly Dictionary<int, int> _entityIdToSimulationIndex = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _simulationIndexToEntityId = new Dictionary<int, int>();

        public int Count => _entityIdToSimulationIndex.Count;

        public void Bind(int entityId, int simulationIndex)
        {
            if (_entityIdToSimulationIndex.TryGetValue(entityId, out int oldSimulationIndex))
            {
                _simulationIndexToEntityId.Remove(oldSimulationIndex);
            }

            if (_simulationIndexToEntityId.TryGetValue(simulationIndex, out int oldEntityId))
            {
                _entityIdToSimulationIndex.Remove(oldEntityId);
            }

            _entityIdToSimulationIndex[entityId] = simulationIndex;
            _simulationIndexToEntityId[simulationIndex] = entityId;
        }

        public void RemapIndex(int entityId, int newSimulationIndex)
        {
            if (!_entityIdToSimulationIndex.TryGetValue(entityId, out int oldSimulationIndex))
            {
                return;
            }

            if (_simulationIndexToEntityId.TryGetValue(newSimulationIndex, out int oldEntityId) &&
                oldEntityId != entityId)
            {
                _entityIdToSimulationIndex.Remove(oldEntityId);
            }

            _simulationIndexToEntityId.Remove(oldSimulationIndex);
            _entityIdToSimulationIndex[entityId] = newSimulationIndex;
            _simulationIndexToEntityId[newSimulationIndex] = entityId;
        }

        public bool TryGetSimulationIndex(int entityId, out int simulationIndex)
        {
            return _entityIdToSimulationIndex.TryGetValue(entityId, out simulationIndex);
        }

        public bool TryGetEntityId(int simulationIndex, out int entityId)
        {
            return _simulationIndexToEntityId.TryGetValue(simulationIndex, out entityId);
        }

        public bool UnbindByEntityId(int entityId)
        {
            if (!_entityIdToSimulationIndex.TryGetValue(entityId, out int simulationIndex))
            {
                return false;
            }

            _entityIdToSimulationIndex.Remove(entityId);
            _simulationIndexToEntityId.Remove(simulationIndex);
            return true;
        }

        public bool UnbindBySimulationIndex(int simulationIndex)
        {
            if (!_simulationIndexToEntityId.TryGetValue(simulationIndex, out int entityId))
            {
                return false;
            }

            _simulationIndexToEntityId.Remove(simulationIndex);
            _entityIdToSimulationIndex.Remove(entityId);
            return true;
        }

        public void Clear()
        {
            _entityIdToSimulationIndex.Clear();
            _simulationIndexToEntityId.Clear();
        }
    }
}
