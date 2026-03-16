# Simulation Development Skill (VampireLike)

## 目标
本文件是 SimulationWorld 的正式设计说明和扩展开发规范。
后续在 Simulation 相关模块做功能扩展、性能优化、回归修复时，统一按本规范执行。

## 适用范围
- Assets/GameMain/Scripts/Simulation/*
- Assets/GameMain/Scripts/Procedure/Game/GameStateBattle.cs
- Assets/GameMain/Scripts/Procedure/Game/ProcedureGame.cs
- Assets/GameMain/Scripts/Utility/AIUtility.cs
- Assets/Tests/Simulation/EditMode/*
- Assets/Tests/Simulation/PlayMode/*

当前状态：P2 Job/Burst 主体已完成，SimulationWorld 已是战斗核心调度层。

## 模块分层
SimulationWorld 使用 partial 拆分，职责如下：

- SimulationWorld.cs
  - 开关、主容器、绑定、主 Tick 入口。
- SimulationWorld.EntitySync.cs
  - 实体 Show/Hide 到仿真容器的生命周期同步。
- SimulationWorld.EnemyJobs.cs
  - 敌人移动和互斥分离的 Job/Burst 链路。
- SimulationWorld.ProjectileJobs.cs
  - 投射物移动、寿命、碰撞候选、主线程结算。
- SimulationWorld.JobDataChannel.cs
  - Native 容器、拷贝转换、容量管理、运行时统计。
- SimulationWorld.TargetSelectionSpatialIndex.cs
  - 目标选择空间索引（最近敌人查询）。
- SimulationWorld.Presentation.cs
  - 表现层写回（Transform）和命中表现事件消费。

## 运行时执行链路
1. GameStateBattle.OnUpdate
- 先执行 EnemyManager.OnUpdate
- 再执行 SimulationWorld.Tick

2. SimulationWorld.Tick
- UseSimulationMovement = false：直接返回（完全回退旧链路）
- UseSimulationMovement = true 且 UseJobSimulation = false：走主线程敌人仿真
- UseSimulationMovement = true 且 UseJobSimulation = true：走 Job/Burst 总链路

3. SimulationWorld.LateUpdate
- 调用 Presentation.OnLateUpdate
- 统一写回 Enemy/Projectile 表现

## 核心数据契约
### 主容器
- List<EnemySimData> _enemies
- List<ProjectileSimData> _projectiles
- List<PickupSimData> _pickups

### 绑定关系
- EntityBinding 维护 EntityId <-> SimulationIndex 双向映射。
- 删除必须使用 swap-back：
  - 尾元素覆盖删除位
  - RemapIndex
  - RemoveAt(last)

### Job 通道
- EnemyJobInput/Output
- ProjectileJobInput/Output
- CollisionQuery/CollisionCandidate
- NativeParallelMultiHashMap（互斥桶、碰撞桶、目标桶）

统一规则：
- Allocator.Persistent 分配
- Initialize/Dispose 集中管理
- Clear 只清容器，不破坏生命周期

## 不可破坏的设计约束
### 生命周期单入口
仿真容器增删只能由 EntitySync 驱动。
禁止在 Enemy/Weapon/Projectile 业务代码中直接改仿真容器。

### 逻辑与表现边界
- Simulation 只产出逻辑数据，不直接写 Transform。
- Transform 写回只能在 Presentation。
- 命中表现通过事件缓冲在主线程提交。

### 开关和回滚
- UseSimulationMovement：总开关，支持一键回滚。
- UseJobSimulation：Job 开关，支持 P1.5/ P2 对照。
- UseBurstJobs：Burst 开关。

### 生效时机约束（重要）
- SetUseSimulationMovement / SetUseJobSimulation 在 Battle 中会被忽略。
- 这两个开关不支持战斗内热切换，只允许战斗外修改生效。

## 敌人和投射物执行模型
### 敌人（Job）
固定阶段：
- BuildInput
- Move
- Separation
- Commit

约束：
- 热路径禁止 LINQ 和托管分配
- 不读写 Transform
- 阶段必须可独立 Profile

### 投射物（Job）
包含：
- 移动更新
- 寿命和越界回收
- Broad Phase 候选构建
- 主线程命中结算和回收

## 碰撞和伤害结算规范
### Broad Phase
候选由 _collisionQueryInputs + _enemyCollisionBuckets 计算。
MaxTargets 必须覆盖“玩家候选 + 敌人候选”的总量。

### Area Query 快照语义
- 入队时记录 SourceWasActiveAtQueryTime。
- 结算按快照判定来源有效性，避免查询后状态变化导致误判。

### 主线程结算
- Projectile 命中：按 ImpactData + AIUtility.CalcDamageHP。
- Area 命中：调用 AIUtility.PerformCollision(target, source, true)。

### 伤害公式约束
AIUtility.CalcDamageHP：
- 闪避使用 dodgeStat.Value（加算语义），不使用 Percent。
- 攻击： (attack + AttackStat.Value) * AttackStat.Percent
- 防御： (damage - DefenseStat.Value) / DefenseStat.Percent
- 最终伤害最小值为 1。

## 已修复问题（纳入长期约束）
1. 闪避语义修正：使用 Value 而非 Percent。
2. UseSimulationMovement / UseJobSimulation 战斗内禁止热切换。
3. MaxTargets 统计覆盖玩家候选，避免超额候选。
4. Area 查询引入来源活跃快照并按快照结算。

后续改动若触碰这些路径，必须保持行为不回退。

## 扩展开发 SOP
### Step 0：定义模式和回滚
- 明确功能在哪条路径生效（Simulation / Job / Burst）。
- 明确开关关闭后的回退行为。

### Step 1：扩数据
- 先改 SimData 和 JobData。
- 再改 CreateInitialSimData 与转换函数。

### Step 2：接生命周期
- 只在 EntitySync 增加注册/反注册。
- 保持 group 到容器映射清晰。

### Step 3：接执行阶段
- 优先放入现有阶段（Build/Schedule/Commit）。
- 新阶段必须补 ProfilerMarker。

### Step 4：接结算和表现
- 逻辑结算收口到主线程。
- 表现写回放在 Presentation。

### Step 5：补测试
EditMode 和 PlayMode 同步补回归，至少覆盖：
- 行为正确性
- 开关路径
- 索引稳定性
- 新增边界条件

### Step 6：更新文档
- 更新本文件。
- 需要性能结论时，同步更新 docs/P2 Job System + Burst 落地.md。

## 测试命令
- PlayMode
  - Unity -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode-test-results.xml -logFile Logs/playmode-tests.log
- EditMode
  - Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-test-results.xml -logFile Logs/editmode-tests.log

## 关键回归用例（必须保留）
- TickEnemies_MatchesOutput_WhenBurstJobsToggled
- TryGetNearestEnemyEntityId_SelectsNearestBucketCandidate_WhenJobSimulationEnabled
- TickProjectiles_LimitsCandidatesToMaxTargets_IncludingPlayerCandidate
- SetUseSimulationAndJob_AreIgnored_WhenBattleStateIsActive
- EnqueueAreaQuery_CapturesInactiveSourceSnapshot_WhenSourceEntityUnavailable

对应文件：
- Assets/Tests/Simulation/EditMode/SimulationWorldTickTests.cs
- Assets/Tests/Simulation/PlayMode/SimulationWorldPlayModeTests.cs

## P2 验收口径
- 3k 敌人下 Main Thread 明显下降（目标 >= 30%）。
- 战斗持续帧 GC Alloc 接近 0。
- Battle -> LevelUp -> Shop -> Battle 循环稳定。

## 提交前门禁清单
- 关闭 UseSimulationMovement 是否完全回退旧链路。
- EntityBinding 是否保持双向一致，删除后是否正确 remap。
- Job 容器是否无泄漏（Persistent 都可 Dispose）。
- 是否引入新热路径 GC（LINQ、临时集合、装箱）。
- 是否破坏“战斗内不热切换 UseSimulationMovement/UseJobSimulation”。
- 是否同步更新测试和本设计文档。
