## ADDED Requirements

### Requirement: SimulationWorld SHALL be the sole battle simulation executor
The battle runtime MUST execute movement, projectile stepping, collision broad-phase, and related simulation state updates through `SimulationWorld`, and it MUST NOT route these responsibilities through an alternative non-`SimulationWorld` runtime path.

#### Scenario: Battle tick advances through SimulationWorld
- **WHEN** the battle update loop advances a gameplay frame
- **THEN** `SimulationWorld` executes the simulation pipeline for that frame as the authoritative runtime update path

#### Scenario: Legacy routing switch does not select an alternate executor
- **WHEN** runtime configuration related to simulation movement is evaluated
- **THEN** it does not select or re-enable a separate legacy movement execution path

### Requirement: Runtime entities SHALL submit input and consume simulation output only
Enemy entities, the player entity, projectile entities, and `MovementComponent` MUST submit movement or behavior input into `SimulationWorld` state and MUST consume position, facing, hit, or lifecycle results from simulation output, rather than computing world-space advancement independently.

#### Scenario: MovementComponent no longer advances transforms directly
- **WHEN** movement input, speed, or separation parameters change on an entity
- **THEN** the component synchronizes those values to `SimulationWorld` state without directly moving the entity transform

#### Scenario: Entity presentation follows simulation output
- **WHEN** simulation state is committed for enemies or projectiles
- **THEN** entity presentation updates consume the committed output to refresh transforms, facing, visibility, or removal state

### Requirement: Spatial queries SHALL use SimulationWorld-owned data and services
Target selection, projectile hits, area hits, and sector hits MUST use `SimulationWorld` spatial indexing and query services, and MUST NOT fall back to legacy entity-side traversal or solver-specific query paths.

#### Scenario: Target selection uses simulation spatial data
- **WHEN** weapon or AI logic requests nearby or nearest targets
- **THEN** the query resolves against `SimulationWorld` maintained spatial data for the current frame

#### Scenario: Projectile and area hit evaluation stay on simulation path
- **WHEN** projectile, area, or sector hit logic runs during combat
- **THEN** hit candidates are produced from `SimulationWorld` collision and query capabilities instead of a fallback entity-side path

### Requirement: Runtime surfaces SHALL reflect the single-path architecture
Runtime debug surfaces, automated tests, and architecture documents MUST reflect that the project supports one authoritative `SimulationWorld` execution path rather than dual-path behavior.

#### Scenario: Debug panel omits legacy solver controls
- **WHEN** runtime simulation debugging is displayed
- **THEN** it shows current `SimulationWorld` metrics without exposing legacy solver switching or dual-path controls

#### Scenario: Regression tests validate observable single-path behavior
- **WHEN** simulation regression coverage is maintained
- **THEN** tests validate observable outcomes such as movement, projectile lifetime, hit results, and hide/remove lifecycle instead of asserting private compatibility fields for legacy paths
