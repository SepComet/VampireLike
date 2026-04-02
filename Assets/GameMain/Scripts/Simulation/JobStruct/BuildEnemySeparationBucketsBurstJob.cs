using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        [BurstCompile]
        private struct BuildEnemySeparationBucketsBurstJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<EnemyJobOutputData> Inputs;
            public NativeParallelMultiHashMap<long, int>.ParallelWriter Buckets;
            public float CellSize;

            public void Execute(int index)
            {
                BuildEnemySeparationBucket(
                    index,
                    Inputs,
                    Buckets,
                    CellSize
                );
            }
        }

        private static void BuildEnemySeparationBucket(
            int index,
            NativeArray<EnemyJobOutputData> inputs,
            NativeParallelMultiHashMap<long, int>.ParallelWriter buckets,
            float cellSize
        )
        {
            EnemyJobOutputData output = inputs[index];
            if (!output.AvoidEnemyOverlap)
            {
                return;
            }

            float3 position = output.Position;
            position.y = 0f;
            int cellX = (int)math.floor(position.x / cellSize);
            int cellZ = (int)math.floor(position.z / cellSize);
            buckets.Add(SeparationCellKey(cellX, cellZ), index);
        }
    }
}