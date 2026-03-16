---
name: simulation-development
description: Maintain and extend VampireLike SimulationWorld (P2 baseline). Use for Simulation data contracts, lifecycle sync, Job/Burst pipeline, collision settlement, and rollback-safe runtime switches.
---

# Simulation Development

## Quick Start

1. Read the design spec first: `./references/SimulationDevelopmentSkill.md`.
2. If performance conclusions change, sync evidence to `../../docs/P2 Job System + Burst 落地.md`.
3. Classify change scope before coding:
   - `SimData/JobData` contracts
   - lifecycle sync (`SimulationWorld.EntitySync`)
   - Job/Burst execution pipeline (`SimulationWorld.EnemyJobs`, `SimulationWorld.ProjectileJobs`)
   - collision query/settlement semantics
   - presentation write-back (`SimulationWorld.Presentation`)
4. Decide rollback behavior up front:
   - `UseSimulationMovement` off path
   - `UseJobSimulation` off path
5. Add/adjust both EditMode and PlayMode regression tests.

## Source Map

- Simulation core: `../../Assets/GameMain/Scripts/Simulation/SimulationWorld.cs`
- Job data channel: `../../Assets/GameMain/Scripts/Simulation/SimulationWorld.JobDataChannel.cs`
- Enemy jobs: `../../Assets/GameMain/Scripts/Simulation/SimulationWorld.EnemyJobs.cs`
- Projectile jobs: `../../Assets/GameMain/Scripts/Simulation/SimulationWorld.ProjectileJobs.cs`
- Lifecycle sync: `../../Assets/GameMain/Scripts/Simulation/SimulationWorld.EntitySync.cs`
- Presentation sync: `../../Assets/GameMain/Scripts/Simulation/SimulationWorld.Presentation.cs`
- Target selection index: `../../Assets/GameMain/Scripts/Simulation/SimulationWorld.TargetSelectionSpatialIndex.cs`
- Tick context: `../../Assets/GameMain/Scripts/Simulation/SimulationTickContext.cs`
- Index binding: `../../Assets/GameMain/Scripts/Simulation/EntityBinding.cs`
- Battle entry: `../../Assets/GameMain/Scripts/Procedure/Game/GameStateBattle.cs`
- Battle state gate: `../../Assets/GameMain/Scripts/Procedure/Game/ProcedureGame.cs`
- Damage/collision utility: `../../Assets/GameMain/Scripts/Utility/AIUtility.cs`
- Global component init: `../../Assets/GameMain/Scripts/Base/GameEntry.Custom.cs`
- Enemy old path gate:
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Enemy/MeleeEnemy.cs`
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Enemy/RemoteEnemy.cs`
- Regression tests:
  - `../../Assets/Tests/Simulation/EditMode/SimulationWorldTickTests.cs`
  - `../../Assets/Tests/Simulation/PlayMode/SimulationWorldPlayModeTests.cs`

## Non-Negotiable Invariants

- Maintain `EntityId <-> SimulationIndex` consistency.
- Use swap-back removal (`move last -> remove last -> remap index`).
- Keep lifecycle registration/removal inside `EntitySync` event flow; do not double-write containers from gameplay code.
- Keep logic/presentation boundary:
  - Simulation computes logical outputs.
  - Presentation writes back `Transform`.
- Keep A/B rollback path (`UseSimulationMovement`/`UseJobSimulation`).
- `SetUseSimulationMovement` and `SetUseJobSimulation` must not hot-switch during `Battle`.
- Keep area query snapshot semantics (`SourceWasActiveAtQueryTime`) intact.
- Keep dodge semantics using `Value` (additive), not `Percent`.
- Avoid new managed allocations in Tick hot paths.

## Change Recipes

### Add or Change SimData Fields

1. Update target structs in `Simulation/SimData/` and Job channel structs.
2. Populate defaults in `Create*InitialSimData` / lifecycle registration path.
3. Apply runtime updates in simulation stages.
4. Consume visual fields in `Presentation` only.
5. Ensure backward compatibility when `UseSimulationMovement` is off.

### Extend Job/Burst Pipeline

1. Keep deterministic stage ownership (Build/Schedule/Complete/Commit).
2. Preserve state semantics; avoid UI/audio/effect side effects in simulation loops.
3. Keep `ProfilerMarker` coverage for new or changed stages.
4. Keep hot paths data-driven (no direct `Transform` reads/writes).

### Modify Collision / Area Query Behavior

1. Treat broad phase candidate generation and main-thread settlement as separate steps.
2. Preserve `MaxTargets` semantics across player + enemy candidates.
3. If adding query metadata, flow it through:
   - request buffer -> collision query input -> candidate -> settlement.
4. Keep area-source snapshot behavior and avoid runtime-state race regressions.

### Add or Adjust Runtime Switches

1. Define exact effective timing (`Battle` or out-of-`Battle`) before implementation.
2. For high-risk switches, enforce out-of-battle-only changes.
3. Provide clear warning logs for ignored runtime switch attempts.

## Validation Checklist

- `UseSimulationMovement = false` and `true` both run correctly.
- `UseJobSimulation = false` and `true` both run correctly under simulation mode.
- No duplicate registration or stale index after entity hide/destroy.
- Battle loop remains stable (`Battle -> LevelUp -> Shop -> Battle`).
- No new per-frame GC spikes in hot paths.
- Main flow has no new Error/Exception logs.
- Keep these regression tests green in both EditMode and PlayMode:
  - `TickProjectiles_LimitsCandidatesToMaxTargets_IncludingPlayerCandidate`
  - `SetUseSimulationAndJob_AreIgnored_WhenBattleStateIsActive`
  - `EnqueueAreaQuery_CapturesInactiveSourceSnapshot_WhenSourceEntityUnavailable`
- Update `./references/SimulationDevelopmentSkill.md` when contracts, boundaries, or rules change.
