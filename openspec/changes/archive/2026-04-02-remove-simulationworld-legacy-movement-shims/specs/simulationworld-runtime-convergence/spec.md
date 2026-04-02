## MODIFIED Requirements

### Requirement: SimulationWorld SHALL be the sole battle simulation executor
The battle runtime MUST execute movement, projectile stepping, collision broad-phase, and related simulation state updates through `SimulationWorld`, and it MUST NOT route these responsibilities through an alternative non-`SimulationWorld` runtime path or expose compatibility switches that imply such a runtime path still exists.

#### Scenario: Battle tick advances through SimulationWorld
- **WHEN** the battle update loop advances a gameplay frame
- **THEN** `SimulationWorld` executes the simulation pipeline for that frame as the authoritative runtime update path

#### Scenario: Legacy routing switch does not select an alternate executor
- **WHEN** runtime configuration related to simulation movement is evaluated
- **THEN** it does not select or re-enable a separate legacy movement execution path

#### Scenario: Runtime API does not expose legacy movement enablement shims
- **WHEN** gameplay runtime code integrates with movement simulation
- **THEN** it does not depend on compatibility members whose purpose is to report whether `SimulationWorld` movement is enabled

### Requirement: Runtime surfaces SHALL reflect the single-path architecture
Runtime debug surfaces, automated tests, architecture documents, and compatibility-facing runtime APIs MUST reflect that the project supports one authoritative `SimulationWorld` execution path rather than dual-path behavior, and MUST NOT preserve legacy solver provider abstractions that imply an alternate runtime separation path is still supported.

#### Scenario: Debug panel omits legacy solver controls
- **WHEN** runtime simulation debugging is displayed
- **THEN** it shows current `SimulationWorld` metrics without exposing legacy solver switching or dual-path controls

#### Scenario: Regression tests validate observable single-path behavior
- **WHEN** simulation regression coverage is maintained
- **THEN** tests validate observable outcomes such as movement, projectile lifetime, hit results, and hide/remove lifecycle instead of asserting private compatibility fields for legacy paths

#### Scenario: Runtime codebase omits legacy solver provider abstractions
- **WHEN** enemy separation behavior is implemented or documented
- **THEN** it is described as `SimulationWorld`-owned runtime behavior without referencing `EnemySeparationSolverProvider`, `IEnemySeparationSolver`, or equivalent legacy provider abstractions
