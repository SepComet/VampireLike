using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        [BurstCompile]
        private struct QueryCollisionCandidatesBurstJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<CollisionQueryData> Queries;
            [ReadOnly] public NativeParallelMultiHashMap<long, int> EnemyBuckets;
            [ReadOnly] public NativeArray<EnemyJobOutputData> EnemyOutputs;
            public NativeList<CollisionCandidateData>.ParallelWriter Candidates;
            public bool HasEnemyTargets;
            public bool HasPlayerTarget;
            public int PlayerTargetEntityId;
            public float3 PlayerPosition;
            public float CellSize;

            public void Execute(int index)
            {
                CollisionQueryData query = Queries[index];
                float radiusSqr = query.Radius * query.Radius;
                int centerCellX = (int)math.floor(query.Position.x / CellSize);
                int centerCellZ = (int)math.floor(query.Position.z / CellSize);
                int queryRange = math.max(1, (int)math.ceil(query.Radius / CellSize));
                int selectedCount = 0;
                bool reachedLimit = false;

                if (HasPlayerTarget && query.SourceEntityId != PlayerTargetEntityId &&
                    query.SourceOwnerEntityId != PlayerTargetEntityId)
                {
                    float3 playerPosition = PlayerPosition;
                    playerPosition.y = query.Position.y;
                    float3 playerDelta = playerPosition - query.Position;
                    float playerSqrDistance = math.lengthsq(playerDelta);
                    if (playerSqrDistance <= radiusSqr)
                    {
                        Candidates.AddNoResize(new CollisionCandidateData
                        {
                            QueryId = query.QueryId,
                            SourceType = query.SourceType,
                            SourceEntityId = query.SourceEntityId,
                            SourceOwnerEntityId = query.SourceOwnerEntityId,
                            TargetEntityId = PlayerTargetEntityId,
                            SqrDistance = playerSqrDistance
                        });

                        selectedCount++;
                        if (selectedCount >= query.MaxTargets)
                        {
                            reachedLimit = true;
                        }
                    }
                }

                if (!HasEnemyTargets || reachedLimit)
                {
                    return;
                }

                for (int dx = -queryRange; dx <= queryRange && !reachedLimit; dx++)
                {
                    for (int dz = -queryRange; dz <= queryRange && !reachedLimit; dz++)
                    {
                        long key = SeparationCellKey(centerCellX + dx, centerCellZ + dz);
                        if (!EnemyBuckets.TryGetFirstValue(key, out int enemyIndex,
                                out NativeParallelMultiHashMapIterator<long> iterator))
                        {
                            continue;
                        }

                        do
                        {
                            if (enemyIndex < 0 || enemyIndex >= EnemyOutputs.Length)
                            {
                                continue;
                            }

                            EnemyJobOutputData enemy = EnemyOutputs[enemyIndex];
                            if (enemy.EntityId == query.SourceOwnerEntityId)
                            {
                                continue;
                            }

                            float3 delta = new float3(
                                enemy.Position.x - query.Position.x,
                                enemy.Position.y - query.Position.y,
                                enemy.Position.z - query.Position.z);
                            float sqrDistance = math.lengthsq(delta);
                            if (sqrDistance > radiusSqr)
                            {
                                continue;
                            }

                            Candidates.AddNoResize(new CollisionCandidateData
                            {
                                QueryId = query.QueryId,
                                SourceType = query.SourceType,
                                SourceEntityId = query.SourceEntityId,
                                SourceOwnerEntityId = query.SourceOwnerEntityId,
                                TargetEntityId = enemy.EntityId,
                                SqrDistance = sqrDistance
                            });

                            selectedCount++;

                            if (selectedCount >= query.MaxTargets)
                            {
                                reachedLimit = true;
                                break;
                            }
                        } while (EnemyBuckets.TryGetNextValue(out enemyIndex, ref iterator));
                    }
                }
            }
        }
    }
}