using System.Collections.Generic;
using CustomDebugger;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Simulation
{
    public sealed partial class SimulationWorld : GameFrameworkComponent
    {
        // Partial layout:
        // - SimulationWorld.cs:                                鏍稿績鐘舵€併€佸父閲忓拰 Unity 鐢熷懡鍛ㄦ湡鍏ュ彛鐐广€?
        // - SimulationWorld.RuntimeModules.cs:                杩愯鏃跺煙瀵硅薄銆侀厤缃拰鐘舵€佷唬鐞嗐€?
        // - SimulationWorld.SimEntityState.cs:                 妯℃嫙鐘舵€佺殑澧炲垹鏀规煡鍜岀敓鍛藉懆鏈熸敞鍐屻€?
        // - SimulationWorld.EntityToSimData.cs:               Unity 瀹炰綋鍒?sim data 鐨勫垵濮嬪寲閫傞厤銆?
        // - SimulationWorld.EntitySync.cs:                     GameFramework 瀹炰綋 show/hide 浜嬩欢妗ャ€?
        // - SimulationWorld.TargetSelectionSpatialIndex.cs:    鏈€杩戞晫绌洪棿绱㈠紩鏌ヨ銆?
        // - Presentation/SimulationWorld.TransformSync.cs:     late-update transform 鍚屾妗ャ€?
        // - Presentation/SimulationWorld.HitPresentation.cs:   鎶曞皠鐗╁懡涓簨浠惰〃鐜版ˉ銆?
        // - DataChannel/SimulationWorld.JobDataChannel.cs:     Job 閫氶亾鍏变韩瀛楁銆佸父閲忓拰杩愯鏃剁姸鎬併€?
        // - DataChannel/SimulationWorld.JobDataLifecycle.cs:   Native 閫氶亾鍒濆鍖栥€佹竻鐞嗗拰 clear銆?
        // - DataChannel/SimulationWorld.JobDataConversion.cs:  sim/job 鏁版嵁杞崲涓庤緭鍏ヨ緭鍑虹紦鍐插噯澶囥€?
        // - DataChannel/SimulationWorld.CollisionTransient.cs: 纰版挒涓存椂閫氶亾鍜岃繍琛屾椂缁熻銆?
        // - DataChannel/SimulationWorld.EnemySeparationTemporal.cs: 鏁屼汉鍒嗙鐨勫抚闂翠复鏃剁姸鎬併€?
        // - DataChannel/SimulationWorld.JobOutputCommit.cs:    Job 杈撳嚭鍥炲啓涓诲鍣ㄣ€?
        // - Jobs/SimulationWorld.EnemyJobs.cs:                 妯℃嫙閫氶亾 缂栨帓 + 鏁屼汉绉诲姩/鍒嗙 椤哄簭鎵ц
        // - Jobs/SimulationWorld.ProjectileJobs.cs:            鎶曞皠鐗╃Щ鍔ㄤ笌鍥炴敹
        // - Jobs/SimulationWorld.CollisionPipeline.cs:         纰版挒绠＄嚎鍏变韩閰嶇疆鍜岀姸鎬?
        // - Jobs/SimulationWorld.CollisionRequests.cs:         area/sector 璇锋眰缂撳啿
        // - Jobs/SimulationWorld.CollisionBroadPhase.cs:       broad-phase 鍊欓€夋瀯寤哄拰 Job 璋冨害
        // - Jobs/SimulationWorld.CollisionResolve.cs:          涓荤嚎绋嬪懡涓粨绠椾笌 area settle
        // - Jobs/SimulationWorld.CollisionPresentation.cs:     鍛戒腑琛ㄧ幇浜嬩欢鍜屽疄浣?impact 瑙ｆ瀽
        // - JobStruct/*.cs:                                    burst job 鍐呮牳鍜岄潰鍚?job 鐨勬暟鎹粨鏋?
        private const float DefaultAttackRange = 1f;
        private const int EnemyStateIdle = 0;
        private const int EnemyStateChasing = 1;
        private const int EnemyStateInAttackRange = 2;
        private const int ProjectileStateActive = 0;
        private const int ProjectileStateExpired = 1;

        private EntitySync _entitySync;
        private TransformSync _transformSync;
        private HitPresentation _hitPresentation;

        public IReadOnlyList<EnemySimData> Enemies => _enemies;
        public IReadOnlyList<ProjectileSimData> Projectiles => _projectiles;
        public IReadOnlyList<PickupSimData> Pickups => _pickups;

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
            Vector3 playerPosition = ResolvePlayerPositionForTick(in context);
            SimulationTickContext resolvedContext =
                new SimulationTickContext(context.DeltaTime, context.RealDeltaTime, playerPosition);
            using (CustomProfilerMarker.TickEnemies.Auto())
            {
                TickSimulationPipeline(in resolvedContext);
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

