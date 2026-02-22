# Simulation Development Skill（VampireLike）

## 目标
本文件是 `Simulation` 分层的开发规范与速查手册。  
后续调整敌人移动、补齐投射物/掉落物逻辑、推进 Job/Burst 改造时，优先按本文档执行，避免反复通读全部代码。

## 当前架构总览（P1.5 已落地）
- Simulation 主目录：`Assets/GameMain/Scripts/Simulation/`
- 核心组件：`SimulationWorld`（`GameFrameworkComponent`）
- 数据容器：
  - `List<EnemySimData> _enemies`
  - `List<ProjectileSimData> _projectiles`
  - `List<PickupSimData> _pickups`
- Tick 临时缓冲：
  - `List<EnemyTickWorkItem> _enemyTickWorkItems`
  - `List<EnemySeparationAgent> _enemySeparationAgents`
- 索引绑定：`EntityBinding`（`EntityId <-> SimulationIndex` 双向映射）
- 生命周期同步：`SimulationWorld.EntitySync`（监听实体 Show/Hide 事件）
- 表现层回写：`SimulationWorld.Presentation`（`LateUpdate` 写回 `Transform`）
- Tick 上下文：`SimulationTickContext`（`DeltaTime`、`RealDeltaTime`、`PlayerPosition`）

## 运行时主链路（按帧）
1. `GameEntry.InitCustomComponents()` 获取或自动挂载 `SimulationWorld`  
   文件：`Assets/GameMain/Scripts/Base/GameEntry.Custom.cs`
2. `ProcedureGame.OnEnter()` 清理旧 Simulation 数据  
   文件：`Assets/GameMain/Scripts/Procedure/Game/ProcedureGame.cs`
3. `GameStateBattle.OnUpdate()` 中先执行刷怪，再执行 `SimulationWorld.Tick(...)`  
   文件：`Assets/GameMain/Scripts/Procedure/Game/GameStateBattle.cs`
4. `SimulationWorld.Tick()` 仅在 `UseSimulationMovement == true` 时执行敌人 Tick
5. `SimulationWorld.LateUpdate()` 执行 `Presentation.OnLateUpdate()`，将仿真结果写回敌人 `Transform`

## 生命周期与数据同步设计
`EntitySync` 通过事件驱动保持 Simulation 容器与实体生命周期一致：

| 事件                            | 组名                      | 行为                                                             |
|-------------------------------|-------------------------|----------------------------------------------------------------|
| `ShowEntitySuccessEventArgs`  | `Enemy`                 | `RegisterEnemyLifecycle` + `UpsertEnemy`                       |
| `HideEntityCompleteEventArgs` | `Enemy`                 | `UnregisterEnemyLifecycle` + `RemoveEnemyByEntityId`           |
| `ShowEntitySuccessEventArgs`  | `Drop`                  | `RegisterPickupLifecycle` + `UpsertPickup`                     |
| `HideEntityCompleteEventArgs` | `Drop`                  | `UnregisterPickupLifecycle` + `RemovePickupByEntityId`         |
| `ShowEntitySuccessEventArgs`  | `Bullet` / `Projectile` | `RegisterProjectileLifecycle` + `UpsertProjectile`             |
| `HideEntityCompleteEventArgs` | `Bullet` / `Projectile` | `UnregisterProjectileLifecycle` + `RemoveProjectileByEntityId` |

关键规则：
- 删除容器元素统一使用“末尾覆盖 + `RemoveAt(lastIndex)` + `EntityBinding.RemapIndex`”。
- `Upsert` 语义：`EntityId` 已存在则覆盖，不存在则追加。

## EnemySimData 合约（当前实现）
文件：`Assets/GameMain/Scripts/Simulation/SimData/EnemySimData.cs`

- `EntityId`：实体唯一标识
- `Position / Forward / Rotation`：逻辑输出与表现层写回字段
- `Speed`：来自 `EnemyData.SpeedBase`
- `AttackRange`：当前固定初始化为 `1f`
- `AvoidEnemyOverlap / EnemyBodyRadius / SeparationIterations`：从 `MovementComponent` 读取
- `TargetType / State`：状态扩展预留

当前状态值（`SimulationWorld` 常量）：
- `0`：Idle
- `1`：Chasing
- `2`：InAttackRange

## TickEnemies 当前算法（P1.5 分阶段）
文件：`Assets/GameMain/Scripts/Simulation/SimulationWorld.cs`

`TickEnemies` 入口保持纯数据热路径，不直接读写 `Transform`，按四阶段执行：
1. `BuildInput`
   - 计算到玩家平面距离
   - 产出 `EnemyTickWorkItem`
   - 生成分离输入 `EnemySeparationAgent`
2. `Move/Separation`
   - 计算追踪位移与朝向
   - 通过 `EnemySeparationSolverProvider.ResolveSimulation(...)` 做互斥求解
3. `StateUpdate`
   - 按距离与可追逐状态更新 `Idle/Chasing/InAttackRange`
4. `WriteBack`
   - 回写 `EnemySimData`（`Position/Forward/Rotation/State`）

## 互斥求解器双通道（Legacy + Simulation）
文件：
- `Assets/GameMain/Scripts/Utility/EnemySeperator/IEnemySeparationSolver.cs`
- `Assets/GameMain/Scripts/Utility/EnemySeperator/EnemySeparationSolverProvider.cs`
- `Assets/GameMain/Scripts/Utility/EnemySeperator/GridBucketEnemySeparationSolver.cs`

说明：
- `SetSimulationAgents/ResolveSimulation`：供 Simulation 纯数据路径调用。
- `Register/Unregister/Resolve(Transform, ...)`：保留旧路径兼容与回滚能力。
- `GridBucketEnemySeparationSolver` 已加入桶列表复用池（`_bucketListPool`）以降低 GC。

## 表现层回写（Presentation）规则
文件：`Assets/GameMain/Scripts/Simulation/SimulationWorld.Presentation.cs`

- 仅在 `UseSimulationMovement == true` 时执行
- 遍历 `EnemyManager.Enemies`，按 `EntityId` 查找 `EnemySimData`
- 写回顺序：
  - 始终写回 `position`
  - 优先使用 `rotation`
  - 若 `rotation` 无效，则回退到 `forward`

## 与旧移动系统的关系（重要）
- `MeleeEnemy` / `RemoteEnemy` 在 `OnUpdate` 开头门控：
  - 开启 Simulation：直接 `return`
  - 关闭 Simulation：走旧 `MovementComponent`
- 回滚能力来自同一构建内的 `UseSimulationMovement` A/B 开关。

## Projectile / Pickup 现状
- `ProjectileSimData`、`PickupSimData` 已具备容器、绑定与生命周期同步通道
- 当前仍未接入独立 Tick 行为，仅完成“创建/回收/索引同步”占位目标

## P1.5 实测基线（P2 输入）
基线文档：`docs/P1.5 Simulation-Supplement.md`

关键结论：
- `TickEnemies GC` 在 `500/1000/1500/2000` 敌人数下均为 `0 KB`
- `GC Allocated In Frame` 从 P1 的 `29.5~109.7 KB` 降至 `2.1 KB`
- `TickEnemies` 热路径耗时（四阶段合计）对比 P1 降幅约 `22.8%~26.8%`
- Android 端评估以 CPU `ms` 为主，`fps` 受 60 上限影响

## 自动化回归（P1.5 已补）
目录：
    - `Assets/Tests/Simulation/EditMode/SimulationWorldTickTests.cs`
    - `Assets/Tests/Simulation/PlayMode/SimulationWorldPlayModeTests.cs`

覆盖点：
- 敌人追踪玩家
- 进入攻击距离后停止移动
- 实体移除后的索引 remap 稳定性

## 后续扩展规范（必须遵守）
1. 先扩数据，再扩行为  
先在 `SimData` 增字段，再改 `EntitySync` 初始化与 `Tick` 逻辑，最后改表现层消费。

2. 保留 A/B 路径  
任何迁移都必须可在同一构建内通过开关回退到旧路径。

3. 生命周期只走 EntitySync  
禁止在敌人业务代码中手动改写 Simulation 容器，避免双写导致索引错乱。

4. 维持“逻辑输出 / 表现消费”边界  
Simulation 只产出逻辑结果，不直接触发 UI、特效、音频事件。

5. 删除策略统一用 swap-back  
所有 Simulation 容器删除都必须 remap 索引，严禁 `RemoveAt(i)` 直接删中间项。

6. 热路径禁用托管分配  
`TickEnemies`、互斥求解、阶段化循环里禁止 LINQ/临时集合扩张。

## P2 前的已知技术债
- `AttackRange` 目前固定值 `1f`，尚未由配置化数值驱动
- `EnemySimData.TargetType/State` 语义仍偏轻量，未形成完整状态机合约
- Projectile/Pickup 尚未迁移真实 Tick 行为

## 提交前检查清单
- 是否保持了 `UseSimulationMovement` 关闭时行为不变
- 是否保持了 `EntityId <-> SimulationIndex` 一致性（含移除 remap）
- 是否避免在 Tick 热路径引入新 GC
- 是否将新字段接入了 `EntitySync -> Tick -> Presentation` 全链路
- 是否补充了最小回归验证（至少 Battle 循环、敌人移除、索引稳定性）
- 是否同步更新本 Skill 文档与 `docs/P1.5 Simulation-Supplement.md`
