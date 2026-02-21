---
name: simulation-development
description: Maintain and extend the VampireLike Simulation layer. Use when modifying `Assets/GameMain/Scripts/Simulation` or related runtime paths (`GameStateBattle`, enemy movement gate, entity lifecycle sync, separation solver), including P1.5 cleanup and P2 Job/Burst preparation.
---

# Simulation Development

## Quick Start

1. Read baseline design doc: `./references/SimulationDevelopmentSkill.md`.
2. Read latest measured baseline: `../../docs/P1.5 Simulation-Supplement.md`.
3. Confirm your change scope is one or more of:
   - `SimData` contracts (`EnemySimData`, `ProjectileSimData`, `PickupSimData`)
   - lifecycle sync (`SimulationWorld.EntitySync`)
   - per-frame simulation (`SimulationWorld.Tick`, `TickEnemies`)
   - presentation write-back (`SimulationWorld.Presentation`)
   - enemy separation solver integration (`EnemySeparationSolverProvider`)
4. Keep rollback path available through `UseSimulationMovement`.

## Source Map

- Simulation core: `../../Assets/GameMain/Scripts/Simulation/SimulationWorld.cs`
- Lifecycle sync: `../../Assets/GameMain/Scripts/Simulation/SimulationWorld.EntitySync.cs`
- Presentation sync: `../../Assets/GameMain/Scripts/Simulation/SimulationWorld.Presentation.cs`
- Tick context: `../../Assets/GameMain/Scripts/Simulation/SimulationTickContext.cs`
- Index binding: `../../Assets/GameMain/Scripts/Simulation/EntityBinding.cs`
- Battle entry: `../../Assets/GameMain/Scripts/Procedure/Game/GameStateBattle.cs`
- Global component init: `../../Assets/GameMain/Scripts/Base/GameEntry.Custom.cs`
- Enemy old path gate:
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Enemy/MeleeEnemy.cs`
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Enemy/RemoteEnemy.cs`
- Separation solver:
  - `../../Assets/GameMain/Scripts/Utility/EnemySeperator/IEnemySeparationSolver.cs`
  - `../../Assets/GameMain/Scripts/Utility/EnemySeperator/EnemySeparationSolverProvider.cs`
  - `../../Assets/GameMain/Scripts/Utility/EnemySeperator/GridBucketEnemySeparationSolver.cs`
- P1.5 baseline doc:
  - `../../docs/P1.5 Simulation-Supplement.md`

## Non-Negotiable Invariants

- Maintain `EntityId <-> SimulationIndex` consistency.
- Use swap-back removal (`move last -> remove last -> remap index`).
- Keep lifecycle registration/removal inside `EntitySync` event flow; do not double-write containers from gameplay code.
- Keep logic/presentation boundary:
  - Simulation computes logical outputs.
  - Presentation writes back `Transform`.
- Keep A/B rollback path:
  - `UseSimulationMovement == false` must preserve old behavior path.
- Avoid new managed allocations in Tick hot paths.

## Change Recipes

### Add or Change SimData Fields

1. Update target struct in `Simulation/SimData/`.
2. Populate default/initial values in `EntitySync` create methods.
3. Apply runtime updates in `Tick` phase.
4. Consume outputs in `Presentation` only if visual write-back is needed.
5. Ensure backward compatibility when `UseSimulationMovement` is off.

### Extend Enemy Tick Behavior

1. Keep deterministic stage order (`BuildInput -> Move/Separation -> StateUpdate -> WriteBack`).
2. Preserve state semantics and avoid direct UI/event side effects in Tick.
3. Keep `ProfilerMarker` coverage for each stage.
4. Keep Tick hot path data-driven (no direct `Transform` read/write).

### Implement Projectile/Pickup Tick (from placeholder to real behavior)

1. Keep lifecycle path unchanged (`EntitySync` handles add/remove).
2. Add dedicated tick methods in `SimulationWorld` for each data type.
3. Keep outputs in data containers; write visuals in presentation phase.
4. Ensure removal path and binding remap rules are identical to enemy path.

### Refactor Toward Job/Burst

1. Prioritize `Move/Separation` stage parallelization.
2. Keep `ProfilerMarker` and stage boundaries stable for P1.5/P2 comparison.
3. Leave transform write-back to presentation-only stage.
4. Keep managed allocations and virtual dispatch out of core loops.

## Validation Checklist

- `UseSimulationMovement = false` and `true` both run correctly.
- No duplicate registration or stale index after entity hide/destroy.
- Battle loop remains stable (`Battle -> LevelUp -> Shop -> Battle`).
- No new per-frame GC spikes in `TickEnemies`.
- Main flow has no new Error/Exception logs.
- Update `./references/SimulationDevelopmentSkill.md` and `../../docs/P1.5 Simulation-Supplement.md` if contracts, boundaries, or baseline data changed.
