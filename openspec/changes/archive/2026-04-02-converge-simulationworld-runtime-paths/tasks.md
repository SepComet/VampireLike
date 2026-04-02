## 1. Battle Runtime Convergence

- [x] 1.1 Update `SimulationWorld` and battle entry points so the Burst/Job simulation pipeline is the only runtime execution path.
- [x] 1.2 Remove or redefine `UseSimulationMovement` and related semantics so it can no longer route combat through a legacy non-simulation path.
- [x] 1.3 Confirm `ClearSimulationState()` and battle bootstrap/reset flows only clear simulation-owned state without reintroducing dual-path behavior.

## 2. Entity and Component Path Cleanup

- [x] 2.1 Refactor `MovementComponent` into an input/register/sync layer and remove direct transform advancement plus `EnemySeparationSolverProvider` runtime dependence.
- [x] 2.2 Remove enemy and player calls that self-advance movement or branch on `UseSimulationMovement`, replacing them with simulation input submission and presentation consumption.
- [x] 2.3 Remove projectile self-driven movement/lifetime logic and make projectile presentation follow `SimulationWorld` output only.

## 3. Query, Debug, and Regression Alignment

- [x] 3.1 Route target selection and hit queries exclusively through `SimulationWorld` spatial data and remove legacy fallback traversal paths.
- [x] 3.2 Clean the runtime debug panel so it exposes current `SimulationWorld` metrics without legacy solver or dual-path controls.
- [x] 3.3 Rewrite simulation regression tests to validate observable behavior such as movement, projectile lifetime, hit results, and hide/remove lifecycle.

## 4. Documentation and Verification

- [x] 4.1 Update simulation architecture documents and todo notes to describe the single authoritative `SimulationWorld` path and remove stale switch descriptions.
- [x] 4.2 Run targeted verification for battle movement, projectile flow, query behavior, and updated tests to confirm no legacy execution path remains.

