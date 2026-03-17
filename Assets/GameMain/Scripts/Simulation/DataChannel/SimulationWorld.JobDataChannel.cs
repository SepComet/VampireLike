using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        // Shared native buffers, collision stats, and channel-level constants.
        private const int CollisionSourceTypeProjectile = 1;
        private const int CollisionSourceTypeArea = 2;
        private const int CollisionShapeCircle = 0;
        private const int CollisionShapeSector = 1;

        private NativeList<EnemyJobInputData> _enemyJobInputs;
        private NativeList<EnemyJobOutputData> _enemyJobOutputs;
        private NativeList<EnemyJobOutputData> _enemyJobSeparationOutputs;
        private NativeList<float2> _enemySeparationPreviousPushes;
        private NativeList<float2> _enemySeparationCurrentPushes;
        private NativeList<ProjectileJobInputData> _projectileJobInputs;
        private NativeList<ProjectileJobOutputData> _projectileJobOutputs;
        private NativeList<CollisionQueryData> _collisionQueryInputs;
        private NativeList<CollisionCandidateData> _collisionCandidates;
        private NativeParallelMultiHashMap<long, int> _enemySeparationBuckets;
        private NativeParallelMultiHashMap<long, int> _enemyCollisionBuckets;
        private readonly List<AreaCollisionRequestData> _areaCollisionRequests = new(16);
        private readonly List<AreaCollisionHitEventData> _areaCollisionHitEvents = new(32);
        private readonly HashSet<long> _areaCollisionHitDedupKeys = new();
        private int _lastCollisionQueryCount;
        private int _lastProjectileCollisionQueryCount;
        private int _lastAreaCollisionQueryCount;
        private int _lastCollisionCandidateCount;
        private int _lastProjectileCollisionCandidateCount;
        private int _lastAreaCollisionCandidateCount;
        private int _lastResolvedAreaHitCount;
        private float _lastCollisionCellSize;
        private bool _lastCollisionHasEnemyTargets;
    }
}
