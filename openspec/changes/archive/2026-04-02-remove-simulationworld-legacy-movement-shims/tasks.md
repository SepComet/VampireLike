## 1. Runtime API cleanup

- [x] 1.1 Remove `SimulationWorld.UseSimulationMovement` and any remaining runtime call sites that branch on that compatibility property.
- [x] 1.2 Remove `EnemyBase.IsSimulationMovementEnabled()` and update enemy runtime code to rely directly on single-path `SimulationWorld` behavior.

## 2. Legacy solver removal

- [x] 2.1 Delete `EnemySeparationSolverProvider`, `IEnemySeparationSolver`, and the legacy solver implementations from `Assets/GameMain/Scripts/Utility/EnemySeperator/`.
- [x] 2.2 Clean up any compile-time references, comments, or documentation text that still mention the removed legacy solver provider abstractions.

## 3. Regression and documentation alignment

- [x] 3.1 Update simulation/runtime tests so they no longer reference removed compatibility members and still cover observable single-path behavior.
- [x] 3.2 Update architecture and roadmap docs to state that no compatibility movement switch or legacy enemy separation provider remains in the runtime codebase.
- [x] 3.3 Run a build and targeted verification for the affected simulation/runtime surface.


