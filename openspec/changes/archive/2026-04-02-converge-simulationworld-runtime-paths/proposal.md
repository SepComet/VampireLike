## Why

`SimulationWorld` 当前已经以 Burst Job 管线承担主要仿真职责，但运行时仍残留组件自驱动、实体 fallback 和旧调试/测试路径，导致移动、碰撞和生命周期逻辑在两套语义之间分叉。继续维护这些旧路径只会放大行为漂移和测试脆弱性，因此需要把运行时彻底收敛到单一路径。

## What Changes

- 将 `SimulationWorld` Burst/Job 管线固化为战斗中的唯一仿真执行入口。
- 删除 `UseSimulationMovement` 一类双路径路由语义，以及实体、组件中的自驱动移动和 fallback 查询逻辑。
- 将 `MovementComponent`、敌人/投射物实体、目标选择器收敛为输入同步、注册管理和表现消费层。
- 清理 `EnemySeparationSolverProvider` 及其调试面板入口，移除旧互斥 solver 的运行时依赖。
- 重建测试与文档，使其面向外部可观察行为，而不是旧私有字段和 Native 通道反射。

## Capabilities

### New Capabilities
- `simulationworld-runtime-convergence`: Define `SimulationWorld` as the sole runtime simulation path for movement, projectile stepping, collision queries, presentation sync, and related battle integration.

### Modified Capabilities

## Impact

- Affected code: `Assets/GameMain/Scripts/Simulation/**`, `Assets/GameMain/Scripts/Components/MovementComponent.cs`, `Assets/GameMain/Scripts/Entity/EntityLogic/**`, `Assets/GameMain/Scripts/Procedure/Game/**`, `Assets/GameMain/Scripts/CustomComponent/DebugPanel/RuntimeDebugPanelComponent.cs`, `Assets/Tests/Simulation/**`, `docs/P1.5 Simulation-Supplement.md`, `docs/P2 Job System + Burst 落地.md`, `docs/TodoList.md`.
- Runtime impact: battle update order, enemy/player/projectile movement, collision candidate queries, target selection, presentation write-back, and simulation lifecycle reset behavior.
- Breaking impact: removes legacy runtime branches and debug affordances that assume a non-`SimulationWorld` execution path exists.
