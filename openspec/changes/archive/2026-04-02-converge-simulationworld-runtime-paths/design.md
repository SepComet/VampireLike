## Context

当前战斗运行时已经把 `SimulationWorld.Tick(...)` 接入主循环，并由 Burst Job 管线承担敌人移动、敌人分离、投射物推进与碰撞 broad-phase 的主体计算。但组件与实体层仍保留旧路径，包括 `MovementComponent.OnUpdate()` 直接改位移、`EnemyProjectile.OnUpdate()` 自驱动、`NearestTargetSelector` fallback 查询，以及 `EnemySeparationSolverProvider` 的旧互斥职责。这使运行时行为、调试入口和测试方式都围绕双路径假设展开。

本次变更横跨 `SimulationWorld`、实体/组件、战斗入口、调试面板、测试和文档，属于一次架构收敛，而不是单点修补。约束是：必须保留现有 Burst/Job 管线与 `_enemies/_projectiles/_pickups` 作为正式状态源，避免重新引入另一套仿真框架。

## Goals / Non-Goals

**Goals:**
- 让 `SimulationWorld` 成为战斗中的唯一仿真执行入口。
- 把实体与组件职责收敛为输入提交、注册同步和表现消费。
- 删除旧双路径分支、旧互斥 solver 依赖和与之绑定的调试入口。
- 将测试改为验证可观察行为，而不是依赖私有字段反射或 Native 通道细节。
- 同步文档，使其准确反映单一路径架构。

**Non-Goals:**
- 不重写现有 Burst/Job 算法本身。
- 不在本次变更中扩展新武器或新仿真能力。
- 不恢复或保留可切换的 P1.5 / 非 `SimulationWorld` 执行路径。
- 不以兼容旧测试为目标保留多余字段或调试钩子。

## Decisions

### 1. 保留现有 Burst Job 管线，删除双路径路由语义
- 决策：以 `SimulationWorld.TickSimulationPipeline(in SimulationTickContext context)` 作为唯一执行面，移除 `UseSimulationMovement` 的运行时路由职责；如果保留该字段，也只能作为全局停机开关而不是分支入口。
- 原因：现有可工作的完整路径只有 Burst Job 管线，继续维护组件驱动 fallback 没有等价能力，只会增加漂移。
- 备选方案：恢复旧路径并保留切换开关。否决原因是旧路径已不完整，恢复成本高且会持续制造测试矩阵和调试噪音。

### 2. 以 sim state 作为唯一真相源，实体侧只提交输入与消费输出
- 决策：`_enemies`、`_projectiles`、`_pickups` 继续作为正式运行时状态源；`MovementComponent`、敌人、玩家、投射物只维护输入态、注册态和表现同步所需数据，不再直接推进世界位置或互斥。
- 原因：状态集中后，Job 输入/输出和 presentation write-back 才能形成闭环，避免实体私自改写坐标导致状态撕裂。
- 备选方案：保留实体局部自驱动，再由 `SimulationWorld` 尽量同步。否决原因是会持续产生写冲突和追责困难。

### 3. 保留 `SimulationWorld` 查询/结算能力，移除实体 fallback 查询
- 决策：碰撞 broad-phase、area/sector/projectile 命中查询统一由 `SimulationWorld` 提供；目标选择器与武器逻辑直接依赖该能力，不再回退到遍历或组件侧查询。
- 原因：查询能力只有与仿真状态源共用同一空间索引时才一致，fallback 查询会导致命中范围与真实运行时不同步。
- 备选方案：保留 fallback 作为调试或兜底。否决原因是这会掩盖真实问题，并让生产行为与测试行为不一致。

### 4. 测试与调试面板跟随单一路径重建
- 决策：移除旧 solver 切换 UI 和依赖私有字段的测试模式，改为验证敌人移动、投射物生命周期、范围命中、实体 hide/remove 等外部行为，并仅展示当前 `SimulationWorld` 的指标。
- 原因：单一路径架构下，私有 Native 通道结构属于实现细节，不应成为长期契约。
- 备选方案：继续保留反射测试以降低短期改动量。否决原因是会固化错误抽象边界。

## Risks / Trade-offs

- [风险] 删除旧分支后，部分依赖 `MovementComponent.OnUpdate()` 或 projectile 自驱动的实体可能短期失效。 → 缓解：优先收敛战斗入口和输入同步接口，再逐类替换敌人、玩家、投射物的调用点。
- [风险] 测试从白盒切到黑盒后，定位 Native 通道回归会更慢。 → 缓解：保留必要运行时指标与日志，但不把私有字段暴露为长期契约。
- [风险] 文档和代码阶段性不同步会误导后续开发。 → 缓解：将文档同步列为同一 change 的完成条件，而不是后续补做。
- [权衡] 放弃双路径意味着失去旧逻辑的快速兜底。 → 缓解：通过更稳定的单路径测试覆盖和可观测指标替代兜底分支。

## Migration Plan

1. 先收敛战斗主入口与 `SimulationWorld` tick 语义，明确单一路径。
2. 再将 `MovementComponent`、敌人、玩家、投射物改为输入/注册/表现职责，移除旧 solver 与 fallback 查询。
3. 最后清理调试面板、重建测试、更新文档，确保仓库不再暴露旧路径语义。
4. 回滚策略仅限于回退整个 change；不设计运行时开关回滚。

## Open Questions

- `UseSimulationMovement` 是否完全删除，还是保留为仅用于全局停机/诊断的只读配置，需要在实现前最终确认。
- 玩家位移是否已经有完整的 `SimulationWorld` 输入同步接口；若没有，需要先补足最小接口再移除 `MovementComponent.OnUpdate()` 调用。
