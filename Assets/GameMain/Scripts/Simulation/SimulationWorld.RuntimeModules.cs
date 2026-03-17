using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        [SerializeField] private CollisionPipelineSettings _collisionPipelineSettings = new();
        [SerializeField] private TargetSelectionSettings _targetSelectionSettings = new();

        private readonly SimulationStateStore _simulationState = new();
        private readonly JobDataRuntimeState _jobDataRuntime = new();
        private readonly CollisionPipelineRuntimeState _collisionPipelineRuntime = new();
        private readonly TargetSelectionRuntimeState _targetSelectionRuntime = new();

        private List<EnemySimData> _enemies => _simulationState.Enemies;
        private List<ProjectileSimData> _projectiles => _simulationState.Projectiles;
        private List<PickupSimData> _pickups => _simulationState.Pickups;
        private List<int> _projectileRecycleEntityIds => _simulationState.ProjectileRecycleEntityIds;
        private HashSet<int> _projectileResolvedEntityIds => _collisionPipelineRuntime.ProjectileResolvedEntityIds;

        private EntityBinding EnemyBinding => _simulationState.EnemyBinding;
        private EntityBinding ProjectileBinding => _simulationState.ProjectileBinding;
        private EntityBinding PickupBinding => _simulationState.PickupBinding;

        private ref NativeList<EnemyJobInputData> _enemyJobInputs => ref _jobDataRuntime.EnemyJobInputs;
        private ref NativeList<EnemyJobOutputData> _enemyJobOutputs => ref _jobDataRuntime.EnemyJobOutputs;
        private ref NativeList<EnemyJobOutputData> _enemyJobSeparationOutputs => ref _jobDataRuntime.EnemyJobSeparationOutputs;
        private ref NativeList<float2> _enemySeparationPreviousPushes => ref _jobDataRuntime.EnemySeparationPreviousPushes;
        private ref NativeList<float2> _enemySeparationCurrentPushes => ref _jobDataRuntime.EnemySeparationCurrentPushes;
        private ref NativeList<ProjectileJobInputData> _projectileJobInputs => ref _jobDataRuntime.ProjectileJobInputs;
        private ref NativeList<ProjectileJobOutputData> _projectileJobOutputs => ref _jobDataRuntime.ProjectileJobOutputs;
        private ref NativeList<CollisionCandidateData> _collisionCandidates => ref _jobDataRuntime.CollisionCandidates;
        private ref NativeParallelMultiHashMap<long, int> _enemySeparationBuckets => ref _jobDataRuntime.EnemySeparationBuckets;
        private ref NativeParallelMultiHashMap<long, int> _enemyCollisionBuckets => ref _jobDataRuntime.EnemyCollisionBuckets;
        private List<AreaCollisionHitEventData> _areaCollisionHitEvents => _jobDataRuntime.AreaCollisionHitEvents;
        private HashSet<long> _areaCollisionHitDedupKeys => _jobDataRuntime.AreaCollisionHitDedupKeys;
        private ref int _lastCollisionQueryCount => ref _jobDataRuntime.LastCollisionQueryCount;
        private ref int _lastProjectileCollisionQueryCount => ref _jobDataRuntime.LastProjectileCollisionQueryCount;
        private ref int _lastAreaCollisionQueryCount => ref _jobDataRuntime.LastAreaCollisionQueryCount;
        private ref int _lastCollisionCandidateCount => ref _jobDataRuntime.LastCollisionCandidateCount;
        private ref int _lastProjectileCollisionCandidateCount => ref _jobDataRuntime.LastProjectileCollisionCandidateCount;
        private ref int _lastAreaCollisionCandidateCount => ref _jobDataRuntime.LastAreaCollisionCandidateCount;
        private ref int _lastResolvedAreaHitCount => ref _jobDataRuntime.LastResolvedAreaHitCount;
        private ref float _lastCollisionCellSize => ref _jobDataRuntime.LastCollisionCellSize;
        private ref bool _lastCollisionHasEnemyTargets => ref _jobDataRuntime.LastCollisionHasEnemyTargets;

        private ref JobHandle _collisionCandidateQueryHandle => ref _collisionPipelineRuntime.CollisionCandidateQueryHandle;
        private ref bool _collisionCandidateQueryScheduled => ref _collisionPipelineRuntime.CollisionCandidateQueryScheduled;

        private ref NativeParallelMultiHashMap<long, int> _enemyTargetBuckets => ref _targetSelectionRuntime.EnemyTargetBuckets;
        private ref bool _enemyTargetBucketsDirty => ref _targetSelectionRuntime.EnemyTargetBucketsDirty;

        private float _projectileCollisionQueryRadius => _collisionPipelineSettings.ProjectileCollisionQueryRadius;
        private int _projectileMaxCandidatesPerQuery => _collisionPipelineSettings.ProjectileMaxCandidatesPerQuery;
        private float _projectileCollisionCellSize => _collisionPipelineSettings.ProjectileCollisionCellSize;
        private bool _dispatchProjectileHitPresentationEvent => _collisionPipelineSettings.DispatchProjectileHitPresentationEvent;
        private bool _dispatchProjectileHitMarkerEvent => _collisionPipelineSettings.DispatchProjectileHitMarkerEvent;
        private bool _dispatchProjectileHitEffectEvent => _collisionPipelineSettings.DispatchProjectileHitEffectEvent;
        private int _projectileHitPresentationEffectTypeId => _collisionPipelineSettings.ProjectileHitPresentationEffectTypeId;
        private float _targetSelectionCellSize => _targetSelectionSettings.CellSize;

        [Serializable]
        private sealed class CollisionPipelineSettings
        {
            [Header("Projectile Collision Query")]
            [Tooltip("Projectile broad-phase collision query radius.")]
            public float ProjectileCollisionQueryRadius = 0.35f;

            [Tooltip("Maximum retained candidates per projectile query.")]
            public int ProjectileMaxCandidatesPerQuery = 1;

            [Tooltip("Broad-phase bucket cell size. <=0 derives from query radius.")]
            public float ProjectileCollisionCellSize = 0f;

            [Header("Projectile Hit Event Dispatch")]
            [Tooltip("Dispatch projectile hit presentation event.")]
            public bool DispatchProjectileHitPresentationEvent = true;

            [Tooltip("Request hit marker when projectile hits.")]
            public bool DispatchProjectileHitMarkerEvent = true;

            [Tooltip("Request hit effect when projectile hits.")]
            public bool DispatchProjectileHitEffectEvent = true;

            [Tooltip("Default hit effect entity type id in presentation event. 0 means not specified.")]
            public int ProjectileHitPresentationEffectTypeId;
        }

        [Serializable]
        private sealed class TargetSelectionSettings
        {
            [Header("Target Selection")]
            [Tooltip("Spatial hash cell size for nearest-enemy queries.")]
            public float CellSize = 2f;
        }

        private sealed class SimulationStateStore
        {
            public readonly List<EnemySimData> Enemies = new();
            public readonly List<ProjectileSimData> Projectiles = new();
            public readonly List<PickupSimData> Pickups = new();
            public readonly List<int> ProjectileRecycleEntityIds = new();
            public readonly EntityBinding EnemyBinding = new();
            public readonly EntityBinding ProjectileBinding = new();
            public readonly EntityBinding PickupBinding = new();
        }

        private sealed class JobDataRuntimeState
        {
            public NativeList<EnemyJobInputData> EnemyJobInputs;
            public NativeList<EnemyJobOutputData> EnemyJobOutputs;
            public NativeList<EnemyJobOutputData> EnemyJobSeparationOutputs;
            public NativeList<float2> EnemySeparationPreviousPushes;
            public NativeList<float2> EnemySeparationCurrentPushes;
            public NativeList<ProjectileJobInputData> ProjectileJobInputs;
            public NativeList<ProjectileJobOutputData> ProjectileJobOutputs;
            public NativeList<CollisionCandidateData> CollisionCandidates;
            public NativeParallelMultiHashMap<long, int> EnemySeparationBuckets;
            public NativeParallelMultiHashMap<long, int> EnemyCollisionBuckets;
            public readonly List<AreaCollisionHitEventData> AreaCollisionHitEvents = new(32);
            public readonly HashSet<long> AreaCollisionHitDedupKeys = new();
            public int LastCollisionQueryCount;
            public int LastProjectileCollisionQueryCount;
            public int LastAreaCollisionQueryCount;
            public int LastCollisionCandidateCount;
            public int LastProjectileCollisionCandidateCount;
            public int LastAreaCollisionCandidateCount;
            public int LastResolvedAreaHitCount;
            public float LastCollisionCellSize;
            public bool LastCollisionHasEnemyTargets;
        }

        private sealed class CollisionPipelineRuntimeState
        {
            public JobHandle CollisionCandidateQueryHandle;
            public bool CollisionCandidateQueryScheduled;
            public readonly HashSet<int> ProjectileResolvedEntityIds = new();
        }

        private sealed class TargetSelectionRuntimeState
        {
            public NativeParallelMultiHashMap<long, int> EnemyTargetBuckets;
            public bool EnemyTargetBucketsDirty = true;
        }
    }
}
