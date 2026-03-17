---
name: simulation-development
description: Maintain, review, refactor, and extend VampireLike SimulationWorld architecture. Use when working on SimulationWorld data ownership, entity lifecycle sync, tick pipeline orchestration, Job/Burst data channels, projectile or area collision settlement, target-selection indexing, presentation write-back, or Simulation regression tests and architecture documentation that must preserve core invariants.
---

# Simulation Development

1. Read `./references/SimulationDevelopmentSkill.md` first.
2. Treat current code as the source of truth when the reference and implementation diverge.
3. Load only the source files needed for the task from the map below.
4. Keep architecture changes and behavior changes explicit; do not hide them inside unrelated edits.

## Source Map

- Core entry: `../../Assets/GameMain/Scripts/Simulation/SimulationWorld.cs`
- Sim state lifecycle: `../../Assets/GameMain/Scripts/Simulation/SimulationWorld.SimEntityState.cs`
- Entity lifecycle bridge: `../../Assets/GameMain/Scripts/Simulation/SimulationWorld.EntitySync.cs`
- Job data channel: `../../Assets/GameMain/Scripts/Simulation/DataChannel/SimulationWorld.JobDataChannel.cs`
- Enemy pipeline: `../../Assets/GameMain/Scripts/Simulation/Jobs/SimulationWorld.EnemyJobs.cs`
- Projectile pipeline: `../../Assets/GameMain/Scripts/Simulation/Jobs/SimulationWorld.ProjectileJobs.cs`
- Collision pipeline: `../../Assets/GameMain/Scripts/Simulation/Jobs/SimulationWorld.CollisionPipeline.cs`
- Target selection index: `../../Assets/GameMain/Scripts/Simulation/SimulationWorld.TargetSelectionSpatialIndex.cs`
- Transform write-back: `../../Assets/GameMain/Scripts/Simulation/Presentation/SimulationWorld.TransformSync.cs`
- Hit presentation bridge: `../../Assets/GameMain/Scripts/Simulation/Presentation/SimulationWorld.HitPresentation.cs`
- Tick context: `../../Assets/GameMain/Scripts/Simulation/SimulationTickContext.cs`
- Entity index binding: `../../Assets/GameMain/Scripts/Simulation/EntityBinding.cs`
- Battle update entry: `../../Assets/GameMain/Scripts/Procedure/Game/GameStateBattle.cs`
- Procedure-level cleanup: `../../Assets/GameMain/Scripts/Procedure/Game/ProcedureGame.cs`
- Damage and collision utility: `../../Assets/GameMain/Scripts/Utility/AIUtility.cs`
- Regression tests:
  - `../../Assets/Tests/Simulation/EditMode/SimulationWorldTickTests.cs`
  - `../../Assets/Tests/Simulation/PlayMode/SimulationWorldPlayModeTests.cs`

## Workflow

1. Classify the change before editing:
   - simulation state contract
   - entity lifecycle mapping
   - tick pipeline stage
   - collision or area query semantics
   - presentation write-back
   - test or architecture doc maintenance
2. Preserve the main boundaries:
   - `Tick` remains the only simulation logic entry
   - lifecycle registration and removal remain centralized
   - logic does not write `Transform`
   - damage, event dispatch, entity hiding, and recycle stay on the main thread
3. Extend data first when behavior depends on new state:
   - update `SimData`
   - update job input/output structs
   - update conversion and initialization paths
4. Reuse an existing pipeline stage before adding a new one.
5. Update `./references/SimulationDevelopmentSkill.md` when module boundaries, invariants, or execution flow change.
6. Add or adjust Simulation tests for every behavior change.

## Non-Negotiable Invariants

- Keep `_enemies`, `_projectiles`, and `_pickups` as the persistent source of truth.
- Keep `EntityBinding` consistent with container indices.
- Use swap-back removal with remap before unbind.
- Drive container add/remove only through lifecycle sync and sim state helpers.
- Keep target-selection buckets and collision buckets as rebuildable caches, not persistent business state.
- Keep area query snapshot semantics intact.
- Avoid managed allocations and LINQ in hot paths.

## Change Guidance

### Extend Simulation State

1. Add fields to the relevant sim data and job structs.
2. Populate defaults in the lifecycle registration path.
3. Flow the data through the execution stage that owns it.
4. Consume presentation-only values in Presentation code, not in simulation jobs.

### Extend Lifecycle Mapping

1. Add the entity group mapping in `SimulationWorld.EntitySync.cs`.
2. Register and unregister through dedicated sim state helpers.
3. Preserve clear ownership over which container the entity enters.

### Extend Tick Pipeline

1. Place logic inside the smallest existing stage that fits.
2. Keep job work data-oriented and side-effect free.
3. Apply outputs back to sim state before any presentation write-back.

### Extend Collision Behavior

1. Separate broad-phase candidate generation from final settlement.
2. Preserve dedup and snapshot behavior on the main thread.
3. Route gameplay effects through the existing main-thread settlement path.

### Extend Presentation

1. Read simulation output only after logic settlement is complete.
2. Do not mutate simulation state from presentation code.

## Validation

- Verify index stability after removal paths.
- Verify clear/reset paths leave no stale bindings or transient buffers.
- Verify behavior under the relevant Simulation tests.
- Verify the reference doc still matches the code after architectural edits.
