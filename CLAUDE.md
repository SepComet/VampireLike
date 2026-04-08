# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**VampireLike** is a Unity 2022.3 LTS action survival game project using a `GameMain + GameFramework` layered architecture. The project uses C# with 4-space indentation, K&R braces, and UTF-8 with BOM encoding.

## Build & Development Commands

- Open in Unity Editor: `Unity -projectPath .`
- Open solution in IDE: `VampireLike.sln`
- Run from Launcher scene: Open `Assets/Launcher.unity` in Editor and press Play
- Run tests via Unity Editor: `Window > General > Test Runner`
- Run EditMode tests via CLI: `Unity -batchmode -projectPath . -runTests -testPlatform editmode -quit -logFile Logs/editmode-tests.log`

## Architecture

### Code Organization

- `Assets/GameMain/Scripts/` - Game business code (UI, Entity, Procedure, Events, DataTable)
- `Assets/GameFramework/` - Shared framework/runtime and editor extensions
- `Assets/Plugins/` - Third-party plugins (e.g., DOTween)
- `Assets/Launcher.unity` - Entry point scene

### GameFramework (Base Framework)

The framework provides modular components accessible via `GameEntry.<ComponentName>`:
- `GameEntry.Event` - Event system (subscribe/unsubscribe pattern)
- `GameEntry.Entity` - Entity management (show/hide/instantiate pooled objects)
- `GameEntry.UI` - UI form management
- `GameEntry.DataTable` - Data table loading
- `GameEntry.Procedure` - Finite state machine procedures
- `GameEntry.Resource` - Resource loading
- Other components: Config, Scene, Sound, Setting, ObjectPool, Fsm, etc.

GameEntry is partially generated. Custom components are initialized in `GameEntry.Custom.cs`.

### UI Architecture (Controller/UseCase/View Pattern)

UI forms follow a layered pattern in `Assets/GameMain/Scripts/UI/`:
- **Controller** - Inherits `UIFormControllerCommonBase<Context, Form>`. Handles input events, subscribes to game events, manages UI lifecycle
- **UseCase** - Business logic layer (e.g., `ShopFormUseCase`). Pure logic without Unity dependencies
- **View** - MonoBehaviour UGuiForm. Handles presentation only

Context objects (e.g., `ShopFormContext`) carry display data to the View layer.

### Entity System

Entities are game objects managed by `GameEntry.Entity`:
- `Assets/GameMain/Scripts/Entity/EntityLogic/` - Entity classes (Player, Enemy, Weapon, Coin, Exp, Effect)
- `Assets/GameMain/Scripts/Entity/EntityData/` - Entity data objects (spawn configuration)
- Weapon attack effects implement `IWeaponAttackEffect` interface
- Target selectors implement `ITargetSelector` (NearestTargetSelector, LowestHealthTargetSelector, HighestHealthTargetSelector)

### Simulation System (ECS-like)

`Assets/GameMain/Scripts/Simulation/SimulationWorld.cs` provides a data-oriented simulation layer:
- Separates simulation data (`*SimData` structs) from presentation (MonoBehaviour entities)
- Uses `SimulationWorld.Tick()` to advance simulation state
- Data flows through: JobDataChannel -> Jobs (enemy/projectile/collision logic) -> OutputCommit -> Presentation sync
- Enemy separation uses spatial indexing and temporal avoidance
- Tests in `Assets/Tests/Simulation/` validate simulation behavior

### Component Pattern

Gameplay components in `Assets/GameMain/Scripts/Components/`:
- `MovementComponent` - Handles movement with direction/speed
- `AttackComponent` - Attack logic
- `HealthComponent` - HP management
- `StatComponent` - Stats with modifiers (additive/multiplicative)
- `AbsorbComponent` - Pickup absorption
- `InputComponent` - Player input
- `BackpackComponent` - Inventory

Components use `OnInit()` initialization and `OnUpdate()` per-frame logic.

### Procedure System

Procedures in `Assets/GameMain/Scripts/Procedure/` manage game flow state machine:
- `ProcedureStartMenu` - Menu scene
- `ProcedureStressTest` - Stress test mode
- Procedures transition via `GameEntry.Procedure`

### Data Tables

Data tables in `Assets/GameMain/Scripts/DataTable/` (DR prefix = Data Row):
- DRBullet, DREnemy, DREntity, DRGoods, DRLevel, DRLevelRarity, DRLevelUpReward, DRMusic, DRProp, DRRole, DRScene, DRSound, DRUIForm, DRUISound, DRWeapon
- Loaded via `GameEntry.DataTable`

### Events

Custom events in `Assets/GameMain/Scripts/CustomEvent/` follow `*EventArgs` naming. Subscribe via `GameEntry.Event.Subscribe(EventArgs.EventId, handler)`.

### Enum Definitions

Enums in `Assets/GameMain/Scripts/Definition/Enum/`: BulletType, CampType, EnemyType, GoodsType, ItemRarity, RelationType, StatType, UIFormType, WeaponType

## Coding Conventions

- C# with 4-space indentation, K&R braces
- One main type per file, file name matches type name
- `PascalCase` for public types/members, `camelCase` for locals/parameters
- `[SerializeField] private` for Inspector exposure
- Always preserve `.meta` files for Unity asset GUID references

## Testing

Tests use `com.unity.test-framework` (NUnit). Place tests in:
- `Assets/Tests/` - General tests
- `Assets/Tests/Simulation/EditMode/` - EditMode simulation tests
- `Assets/Tests/Simulation/PlayMode/` - PlayMode simulation tests
- Name test files `*Tests.cs`

Run via Unity Test Runner (`Window > General > Test Runner`) or CLI.

## Ignore (Do Not Commit)

`Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`
