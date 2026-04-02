using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        #region Job Output Commit

        private void ApplyJobOutputsToSimulationState()
        {
            int enemyCount = Mathf.Min(_enemies.Count, _enemyJobOutputs.Length);
            bool hasEnemyPositionChanged = false;
            for (int i = 0; i < enemyCount; i++)
            {
                EnemyJobOutputData output = _enemyJobOutputs[i];
                if (!hasEnemyPositionChanged)
                {
                    Vector3 currentPosition = _enemies[i].Position;
                    hasEnemyPositionChanged = currentPosition.x != output.Position.x ||
                                              currentPosition.z != output.Position.z;
                }

                _enemies[i] = ConvertToEnemySimData(output);
            }

            if (hasEnemyPositionChanged)
            {
                MarkEnemyTargetSpatialIndexDirty();
            }

            int projectileCount = Mathf.Min(_projectiles.Count, _projectileJobOutputs.Length);
            for (int i = 0; i < projectileCount; i++)
            {
                _projectiles[i] = ConvertToProjectileSimData(_projectileJobOutputs[i]);
            }
        }

        #endregion
    }
}
