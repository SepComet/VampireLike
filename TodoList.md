# 3D 类吸血鬼幸存者项目 Todo（GameMain 侧规划）

> 范围说明：本清单基于当前 `Assets/GameMain` 代码现状制定，未涉及 `Assets/GameFramework` 底层实现。

## 0. 当前代码现状（已确认）
- [x] 已有完整流程骨架：`Menu -> Game(Battle/LevelUp/Shop)`，以及基础实体系统（Player/Enemy/Weapon/Drop/UI）。
- [x] 目前仍是传统 `MonoBehaviour + 每实体 OnUpdate` 驱动，暂无 Job System/Burst 实装。
- [x] 已有一个 Instancing Shader：`Assets/GameMain/Materials/Shaders/SimpleInstancedFlash.shader`，但未接入运行时批量渲染管线。
- [x] 未发现代码热更新方案接入（如 HybridCLR/ILRuntime/xLua 等）。

## 1. P0 基线修正与性能基准
- [ ] 建立性能基准场景（建议复用 `Game.unity` + 压测参数）：
  - 指标：`1k / 3k / 5k` 敌人时的 FPS、CPU Main Thread、GC Alloc、Draw Calls。
  - 输出：一份基线表格（开发机配置 + Unity Profiler 截图）。
- [x] 修正当前高风险逻辑问题（避免后续优化建立在不稳定行为上）：
  - `ProcedureGame.OnEnter()` 与 `_hudInitialized` 逻辑中有重复初始化状态机风险（`InitGameState()` 被调用两次）。
  - `Player.Enable` setter 未更新 `_enable` 字段，状态切换语义不完整。
  - `PlayerData` 构造中 `MaxHealthBase` 初始化异常（自赋值）。
  - `AIUtility.PerformCollision()` 武器伤害计算参数里疑似把 `DodgeStat` 传成 `DefenseStat`。
- [ ] 给关键战斗链路加最小回归测试（PlayMode）：
  - 伤害结算、掉落、回合切换（Battle/LevelUp/Shop）。

**验收标准**
- 基线数据可复现。
- 以上问题修正后，核心流程可稳定连续跑 10 分钟无异常日志。

## 2. P1 Simulation 分层（为 Job/Burst 做结构准备）
- [ ] 新建 `Simulation` 层（建议目录：`Assets/GameMain/Scripts/Simulation`）：
  - `SimulationWorld`：统一持有敌人/投射物/掉落物的纯数据容器。
  - `EnemySimData / ProjectileSimData / PickupSimData`：结构化、连续内存友好的数据定义。
  - `EntityBinding`：维护 `EntityId <-> SimulationIndex` 映射。
- [ ] 将“逻辑计算”和“表现层（Transform/Animator/特效/UI）”拆离：
  - 逻辑层输出 position/rotation/state。
  - 表现层只消费结果做显示。
- [ ] 先保持现有 GameFramework 实体生命周期不变，仅替换更新路径。

**验收标准**
- 敌人移动/追踪由 Simulation 统一调度，不再逐个 Enemy MonoBehaviour 执行核心逻辑。

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
- [ ] Profiling 对比（改造前后同场景同参数）。
- [ ] 回归用例（至少战斗、关卡切换、商店、升级）。
- [ ] 风险与回滚说明（特别是热更新与渲染链路）。
