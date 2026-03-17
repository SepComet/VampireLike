using Unity.Mathematics;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        #region Enemy Separation Temporal

        private void PrepareEnemySeparationJobBuffers(int enemyCount, int bucketCapacity)
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _enemyJobSeparationOutputs, enemyCount);
            _enemyJobSeparationOutputs.Clear();
            if (enemyCount > 0)
            {
                _enemyJobSeparationOutputs.ResizeUninitialized(enemyCount);
            }

            EnsureCapacity(ref _enemySeparationBuckets, bucketCapacity);
            _enemySeparationBuckets.Clear();

            EnsureCapacity(ref _enemySeparationPreviousPushes, enemyCount);
            EnsureCapacity(ref _enemySeparationCurrentPushes, enemyCount);

            if (_enemySeparationPreviousPushes.Length < enemyCount)
            {
                int oldLength = _enemySeparationPreviousPushes.Length;
                _enemySeparationPreviousPushes.ResizeUninitialized(enemyCount);
                for (int i = oldLength; i < enemyCount; i++)
                {
                    _enemySeparationPreviousPushes[i] = float2.zero;
                }
            }
            else if (_enemySeparationPreviousPushes.Length > enemyCount)
            {
                _enemySeparationPreviousPushes.ResizeUninitialized(enemyCount);
            }

            _enemySeparationCurrentPushes.Clear();
            if (enemyCount > 0)
            {
                _enemySeparationCurrentPushes.ResizeUninitialized(enemyCount);
            }
        }

        private void CommitEnemySeparationTemporalBuffers(int enemyCount)
        {
            if (!_enemySeparationPreviousPushes.IsCreated || !_enemySeparationCurrentPushes.IsCreated)
            {
                return;
            }

            int copyCount = math.min(enemyCount,
                math.min(_enemySeparationPreviousPushes.Length, _enemySeparationCurrentPushes.Length));
            for (int i = 0; i < copyCount; i++)
            {
                _enemySeparationPreviousPushes[i] = _enemySeparationCurrentPushes[i];
            }
        }

        private void OnEnemyAddedToSeparationTemporalBuffers()
        {
            if (_enemySeparationPreviousPushes.IsCreated)
            {
                _enemySeparationPreviousPushes.Add(float2.zero);
            }

            if (_enemySeparationCurrentPushes.IsCreated)
            {
                _enemySeparationCurrentPushes.Add(float2.zero);
            }
        }

        private void OnEnemyRemovedFromSeparationTemporalBuffers(int removedIndex)
        {
            if (_enemySeparationPreviousPushes.IsCreated && removedIndex >= 0 &&
                removedIndex < _enemySeparationPreviousPushes.Length)
            {
                _enemySeparationPreviousPushes.RemoveAtSwapBack(removedIndex);
            }

            if (_enemySeparationCurrentPushes.IsCreated && removedIndex >= 0 &&
                removedIndex < _enemySeparationCurrentPushes.Length)
            {
                _enemySeparationCurrentPushes.RemoveAtSwapBack(removedIndex);
            }
        }

        #endregion
    }
}
