## Why

`SimulationWorld` 已经成为唯一运行时执行路径，但仓库里仍保留 `UseSimulationMovement`、`EnemyBase.IsSimulationMovementEnabled()` 以及 `EnemySeparationSolverProvider` / `IEnemySeparationSolver` 这类旧路径壳层。它们不再承载真实运行时能力，却继续制造双路径仍可恢复的错误信号，也增加后续维护和阅读成本。

## What Changes

- 删除 `SimulationWorld.UseSimulationMovement` 这类恒真兼容属性，改为直接暴露单路径语义。
- 删除 `EnemyBase.IsSimulationMovementEnabled()` 及其调用点，去除敌人运行时代码里残留的旧路径判断壳层。
- 删除 `EnemySeparationSolverProvider`、`IEnemySeparationSolver` 及其 legacy solver 实现，明确敌人间分离仅由 `SimulationWorld` 负责。
- 更新测试与文档，确保回归覆盖和架构说明不再引用这些兼容壳层或 legacy solver 接口。

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `simulationworld-runtime-convergence`: 收紧单路径运行时要求，明确不得保留可被误解为旧路径开关、能力接口或 solver 提供器的兼容壳层。

## Impact

- Affected code: `Assets/GameMain/Scripts/Simulation`, `Assets/GameMain/Scripts/Entity/EntityLogic/Enemy`, `Assets/GameMain/Scripts/Utility/EnemySeperator`, and related tests/docs.
- APIs: removes compatibility-facing members that still imply legacy movement routing or solver substitution.
- Systems: clarifies that enemy separation and movement execution stay exclusively on the `SimulationWorld` path.
