using System.Collections.Generic;
using CustomDebugger;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Simulation
{
    public sealed partial class SimulationWorld : GameFrameworkComponent
    {
        // Partial layout:
        // - SimulationWorld.cs:                                核心状态、常量和 Unity 生命周期入口点。
        // - SimulationWorld.SimEntityState.cs:                 模拟状态的增删改查和生命周期注册。
        // - SimulationWorld.EntitySync.cs:                     GameFramework 实体 show/hide 事件桥。
        // - SimulationWorld.TargetSelectionSpatialIndex.cs:    最近敌空间索引查询。
        // - Presentation/SimulationWorld.TransformSync.cs:     late-update transform 同步桥。
        // - Presentation/SimulationWorld.HitPresentation.cs:   投射物命中事件表现桥。
        // - DataChannel/SimulationWorld.JobDataChannel.cs:     本地 通道/缓冲区 持有者和数据的相互转换。
        // - Jobs/SimulationWorld.EnemyJobs.cs:                 模拟通道 编排 + 敌人移动/分离 顺序执行
        // - Jobs/SimulationWorld.ProjectileJobs.cs:            投射物移动与回收
        // - Jobs/SimulationWorld.CollisionPipeline.cs:         碰撞请求的构造、过滤、求解流水线
        // - JobStruct/*.cs:                                    burst job 内核和面向 job 的数据结构
        private const float DefaultAttackRange = 1f;
        private const int EnemyStateIdle = 0;
        private const int EnemyStateChasing = 1;
        private const int EnemyStateInAttackRange = 2;
        private const int ProjectileStateActive = 0;
        private const int ProjectileStateExpired = 1;

        [Header("模拟世界全局设置")] [Tooltip("是否启用世界模拟")] [SerializeField]
        private bool _useSimulationMovement = true;

        private EntitySync _entitySync;
        private TransformSync _transformSync;
        private HitPresentation _hitPresentation;

        private readonly List<EnemySimData> _enemies = new List<EnemySimData>();
        private readonly List<ProjectileSimData> _projectiles = new List<ProjectileSimData>();
        private readonly List<PickupSimData> _pickups = new List<PickupSimData>();
        private readonly List<int> _projectileRecycleEntityIds = new List<int>();
        private readonly HashSet<int> _projectileResolvedEntityIds = new HashSet<int>();

        private EntityBinding EnemyBinding { get; } = new EntityBinding();
        private EntityBinding ProjectileBinding { get; } = new EntityBinding();
        private EntityBinding PickupBinding { get; } = new EntityBinding();

        public IReadOnlyList<EnemySimData> Enemies => _enemies;
        public IReadOnlyList<ProjectileSimData> Projectiles => _projectiles;
        public IReadOnlyList<PickupSimData> Pickups => _pickups;
        public bool UseSimulationMovement => _useSimulationMovement;

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _entitySync = new EntitySync(this);
            _transformSync = new TransformSync(this);
            _hitPresentation = new HitPresentation(this);
            InitializeJobDataChannels();
        }

        private void Start()
        {
            _entitySync?.OnStart();
            _hitPresentation?.OnStart();
        }

        public void Tick(in SimulationTickContext context)
        {
            if (!_useSimulationMovement)
            {
                return;
            }

            using (CustomProfilerMarker.TickEnemies.Auto())
            {
                TickSimulationPipeline(in context);
            }
        }

        private void OnDestroy()
        {
            _hitPresentation?.OnDestroy();
            _entitySync?.OnDestroy();
            _entitySync = null;
            _transformSync = null;
            _hitPresentation = null;
            DisposeJobDataChannels();
        }

        private void LateUpdate()
        {
            _transformSync?.OnLateUpdate();
        }

        #endregion
    }
}