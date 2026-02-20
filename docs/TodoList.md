# 3D 类吸血鬼幸存者项目 Todo（GameMain 侧规划）

> 范围说明：本清单基于当前 `Assets/GameMain` 代码现状制定，未涉及 `Assets/GameFramework` 底层实现。

## 0. 当前代码现状（已确认）
- [x] 已有完整流程骨架：`Menu -> Game(Battle/LevelUp/Shop)`，以及基础实体系统（Player/Enemy/Weapon/Drop/UI）。
- [x] 目前仍是传统 `MonoBehaviour + 每实体 OnUpdate` 驱动，暂无 Job System/Burst 实装。
- [x] 已有一个 Instancing Shader：`Assets/GameMain/Materials/Shaders/SimpleInstancedFlash.shader`，但未接入运行时批量渲染管线。
- [x] 未发现代码热更新方案接入（如 HybridCLR/ILRuntime/xLua 等）。

## 1. P0 基线修正与性能基准
- [x] 建立性能基准场景（建议复用 `Game.unity` + 压测参数）：
  - 指标：`1k / 2k / 3k` 敌人时的 FPS、CPU Main Thread、GC Alloc、Draw Calls。
  - 输出：一份基线表格（开发机配置 + Unity Profiler 截图）。
- [x] 修正当前高风险逻辑问题（避免后续优化建立在不稳定行为上）：
  - `ProcedureGame.OnEnter()` 与 `_hudInitialized` 逻辑中有重复初始化状态机风险（`InitGameState()` 被调用两次）。
  - `Player.Enable` setter 未更新 `_enable` 字段，状态切换语义不完整。
  - `PlayerData` 构造中 `MaxHealthBase` 初始化异常（自赋值）。
  - `AIUtility.PerformCollision()` 武器伤害计算参数里疑似把 `DodgeStat` 传成 `DefenseStat`。
- [x] 给关键战斗链路加最小回归测试（PlayMode）：
  - 伤害结算、掉落、回合切换（Battle/LevelUp/Shop）。

**验收标准**
- 基线数据可复现。
- 以上问题修正后，核心流程可稳定连续跑 10 分钟无异常日志。

## 2. P1 Simulation 分层（为 Job/Burst 做结构准备）
- [x] Checkpoint 1：搭建 Simulation 基础骨架（仅新增，不改行为）
  - 新建目录：`Assets/GameMain/Scripts/Simulation`。
  - 新建 `SimulationWorld`，统一持有 `EnemySimData / ProjectileSimData / PickupSimData` 容器。
  - 新建 `EntityBinding`，维护 `EntityId <-> SimulationIndex` 双向映射。
  - 新建 `SimulationTickContext`（至少包含 `deltaTime`、`playerPosition`）。
  - 完成标准：工程可编译，场景运行行为与当前一致（只加结构，不切链路）。

- [x] Checkpoint 2：敌人生命周期接入 Simulation（保持 GameFramework 生命周期不变）
  - 在敌人 `Show/Hide` 时同步注册/反注册到 `SimulationWorld` 与 `EntityBinding`。
  - `EnemyManagerComponent` 继续负责刷怪与实体显隐，不改外部调用方式。
  - 完成标准：敌人数量统计与当前一致，无重复注册、无悬空索引。

- [x] Checkpoint 3：建立 Simulation 主更新入口并接入 Battle 状态
  - 在 `GameStateBattle.OnUpdate` 中增加 `SimulationWorld.Tick(...)` 调用。
  - 先只接“敌人移动/追踪”系统，其他逻辑保持原路径。
  - 增加开关（建议 `UseSimulationMovement`）用于 A/B 对比与回滚。
  - 完成标准：关闭开关与当前行为一致；开启开关后敌人仍能正常追踪玩家。

- [x] Checkpoint 4：迁移敌人核心移动逻辑到 Simulation（去 MonoBehaviour 核心逻辑）
  - 将 `MeleeEnemy/RemoteEnemy` 的目标追踪、移动方向、攻击距离判定迁至 Simulation。
  - `EnemySimData` 至少包含：`position`、`forward`、`speed`、`attackRange`、`targetType`、`state`。
  - `MeleeEnemy/RemoteEnemy.OnUpdate` 仅保留表现层或空实现（不再做核心移动计算）。
  - 完成标准：同等刷怪量下，敌人移动结果与旧逻辑视觉一致，无明显穿模/停滞回归。

- [x] Checkpoint 5：拆分“逻辑输出”与“表现层消费”
  - 逻辑层输出：`position/rotation/state`（必要时含 `isMoving`）。
  - 表现层仅消费并回写 `Transform`，动画/特效/UI 不参与逻辑计算。
  - 明确边界：HPBar、DamageText、Animator 继续由表现层驱动。
  - 完成标准：关闭/开启 Simulation 不影响 UI 事件链（血量、经验、金币、关卡流程）。

- [x] Checkpoint 6：补齐 Projectile/Pickup 的 Simulation 占位数据通道
  - 在 `SimulationWorld` 中接入 `ProjectileSimData / PickupSimData` 容器与绑定关系。
  - 先不迁移完整行为，只保证创建、回收、索引同步路径可用。
  - 完成标准：投射物/掉落物实体生命周期正常，无索引越界与回收遗漏。

- [x] Checkpoint 7：P1 阶段回归与性能记录
  - 回归用例：战斗 10 分钟、`Battle -> LevelUp -> Shop -> Battle` 循环、掉落吸附与拾取。
  - Profiling 对比：记录 1k/2k/3k 敌人下 Main Thread、GC Alloc、敌人更新耗时。
  - 输出文档：`P1 Simulation 分层设计 + 回滚开关说明 + 对比数据`。
  - 完成标准：核心流程稳定，无新增 Error/Exception；可一键回滚到旧更新路径。

**验收标准**
- 敌人移动/追踪由 Simulation 统一调度，不再逐个 Enemy MonoBehaviour 执行核心逻辑。

## 2.5 P1.5 Simulation 收尾（P2 前置）
- [ ] Checkpoint 1：清理 `TickEnemies` 侧 GC（优先级最高）
  - 目标：将 `TickEnemies GC` 从当前 `27~108 KB` 降到 `< 5 KB / frame`。
  - 重点文件：`Assets/GameMain/Scripts/Utility/EnemySeperator/GridBucketEnemySeparationSolver.cs`。
  - 处理方式：桶容器与临时列表复用（包含 bucket list 复用池），避免每帧重建集合。
  - 完成标准：`2000` 敌人压测下 `TickEnemies GC` 稳定 `< 5 KB / frame`。

- [ ] Checkpoint 2：解耦 Simulation 核心与 `Transform` 运行时依赖
  - 目标：`SimulationWorld.TickEnemies` 不直接读取或写入 `Transform`。
  - 重点文件：`Assets/GameMain/Scripts/Simulation/SimulationWorld.cs`、`Assets/GameMain/Scripts/Utility/EnemySeperator/IEnemySeparationSolver.cs`、`Assets/GameMain/Scripts/Utility/EnemySeperator/EnemySeparationSolverProvider.cs`。
  - 处理方式：互斥求解输入改为纯数据（位置/半径/索引），`Transform` 仅在 Presentation 阶段回写。
  - 完成标准：`TickEnemies` 热路径中不出现 `Transform` 访问。

- [ ] Checkpoint 3：收口 `EntitySync` 职责边界
  - 目标：`EntitySync` 仅处理生命周期映射，不承担运行时移动逻辑。
  - 重点文件：`Assets/GameMain/Scripts/Simulation/SimulationWorld.EntitySync.cs`。
  - 处理方式：保留注册/反注册与初值同步，移除 Tick 过程依赖。
  - 完成标准：`OnShow/OnHide` 逻辑稳定，且不引入运行时分配热点。

- [ ] Checkpoint 4：拆分 Simulation Tick 阶段，为 Job 化铺路
  - 目标：将敌人 Tick 拆分为稳定阶段，便于后续迁移 `IJobParallelFor`。
  - 建议阶段：`BuildInput -> Move/Separation -> StateUpdate -> WriteBack`。
  - 重点文件：`Assets/GameMain/Scripts/Simulation/SimulationWorld.cs`。
  - 完成标准：每阶段有独立 `ProfilerMarker`，可明确观测耗时占比。

- [ ] Checkpoint 5：补最小回归测试（P1.5 重构保护）
  - 目标：确保重构不改变战斗行为。
  - 建议目录：`Assets/Tests/Simulation/`。
  - 用例范围：追踪玩家、攻击距离停下、实体移除后的索引重映射。
  - 完成标准：EditMode/PlayMode 相关用例通过，主流程手测无回归。

- [ ] Checkpoint 6：补充 P1.5 结项文档
  - 输出：`P1.5 收尾说明 + 对比数据 + 回滚开关`。
  - 明确记录：Android 60fps 上限、Profiler 采样配置（Call Stacks 开关状态）、评估以 CPU ms 为主。
  - 完成标准：文档可复现实验结论，并可作为 P2 输入基线。

**验收标准**
- `Movement_Update` 持续维持 `0 ms`（或可忽略占比）。
- `TickEnemies` 在目标敌人数下 GC 与 CPU 耗时均有明显下降，并可复现。
- Simulation 层与表现层边界清晰，可无缝衔接 P2 Job/Burst 改造。

## 3. P2 Job System + Burst 落地（核心性能阶段）
- [ ] 引入并锁定依赖版本（Unity 2022.3 对应）：
  - `com.unity.collections`
  - `com.unity.jobs`
  - `com.unity.burst`
  - `com.unity.mathematics`
- [ ] 第一批 Job 化模块（优先级从高到低）：
  1. 敌人移动与朝向更新（`IJobParallelFor`）。
  2. 目标选择加速（空间哈希/网格分桶，减少全量最近邻搜索）。
  3. 投射物批量移动与寿命回收。
  4. AOE/碰撞候选筛选（先 broad phase，后精算）。
- [ ] Burst 编译策略：
  - 热路径 Job 全部 `[BurstCompile]`。
  - 禁止在 Job 内使用托管分配、虚调用、LINQ。
- [ ] 主线程仅做：输入采样、状态切换、UI同步、实体显隐。

**验收标准**
- 在 3k 敌人规模下，CPU Main Thread 明显下降（目标 >= 30%）。
- Profiler 中战斗帧 GC Alloc 接近 0（持续帧）。

## 4. P3 GPU Instancing 渲染管线（与 Job 并行推进）
- [ ] 先做“低风险版”批处理：
  - 同 Mesh/Material 的敌人分组，使用 `Graphics.DrawMeshInstanced`（每批最多 1023）。
- [ ] 再升级“高上限版”：
  - 使用 `Graphics.DrawMeshInstancedIndirect` + `ComputeBuffer` 管理实例矩阵/颜色/状态。
- [ ] 建立 `InstanceRendererComponent`：
  - 输入：Simulation 输出的 transform/state。
  - 输出：按 enemy archetype 的批量绘制。
- [ ] 将受击闪白、稀有度颜色等通过 `MaterialPropertyBlock` 或实例化属性下发（复用现有 Instanced Shader 思路）。
- [ ] 与现有碰撞体系解耦：
  - 逻辑碰撞走 Simulation，渲染不再依赖每敌人独立 GameObject Renderer。

**验收标准**
- 5k 敌人规模 Draw Calls 显著下降。
- 渲染主线程耗时可控，且视觉行为（受击、朝向、死亡）与逻辑一致。

## 5. P4 代码热更新（建议 HybridCLR）
- [ ] 技术选型定稿：建议 `HybridCLR`（Unity 2022 + C# 生态兼容更自然）。
- [ ] Assembly 拆分：
  - `Main`：启动、资源更新、基础桥接（不可热更）。
  - `Hotfix`：玩法规则、数值公式、技能与敌人行为树（可热更）。
- [ ] 运行时加载流程：
  - 启动时通过现有资源更新流程拉取热更 DLL（与版本号绑定）。
  - 加载 AOT metadata + Hotfix DLL，反射启动 `HotfixEntry`。
- [ ] 建立热更边界规范：
  - Hotfix 不直接依赖编辑器代码。
  - 跨域调用统一走接口/Facade（避免大量反射散落）。
- [ ] 回滚机制：
  - DLL 校验失败时回退上一个稳定版本。

**验收标准**
- 不发整包即可替换一条技能逻辑并在设备上生效。
- 热更失败可自动回退，启动不中断。

## 6. P5 玩法目标对齐（与技术栈并行）
- [ ] 武器系统补完：
  - 自动攻击、多武器并存、同武器升级/进化。
  - `Shop` 武器购买流程补完（当前已有 TODO）。
- [ ] 敌人系统扩展：
  - 近战/远程/精英/首领模板化，支持波次参数化。
- [ ] 关卡节奏：
  - `DRLevel` 扩展为“时间轴+事件波次+奖励节点”。
- [ ] 数值可调试工具：
  - 实时查看 Dps、受击、击杀效率、掉落速率。

**验收标准**
- 一局 10~20 分钟循环可闭环，且关卡难度曲线平滑。

## 7. 推荐执行顺序（避免返工）
- [ ] 里程碑 A：`P0 -> P1`（稳定结构 + 可观测）
- [ ] 里程碑 B：`P2`（CPU 性能突破）
- [ ] 里程碑 C：`P3`（渲染性能突破）
- [ ] 里程碑 D：`P4`（线上快速迭代能力）
- [ ] 里程碑 E：`P5`（内容量与可玩性扩展）

## 8. 交付物清单（每阶段都要有）
- [ ] 设计文档（接口、数据结构、生命周期）。
- [ ] 回归用例（至少战斗、关卡切换、商店、升级）。
- [ ] Profiling 对比（改造前后同场景同参数）。
- [ ] 风险与回滚说明（特别是热更新与渲染链路）。
