---
name: weapon-development
description: Develop and extend the VampireLike weapon system. Use when creating new weapons, updating weapon state machines, changing target selection/effects/data parsing, or integrating weapon behavior with shop, inventory, and entity flow.
---

# Weapon Development

## Quick Start

1. Read full baseline spec: `./references/WeaponDevelopmentSkill.md`.
2. Confirm change scope:
   - weapon runtime (`WeaponBase`, concrete weapons)
   - state flow (`Idle`, `Check_OutRange`, `Check_InRange`, `Attack`)
   - target selector (`ITargetSelector`, `TargetSelectorType`)
   - effect layer (`IWeaponAttackEffect`)
   - data contract (`DRWeapon`, `WeaponData`, `ParamsData`)
3. Keep behavior compatibility with current gameplay loop, shop flow, inventory flow, and UI/event chain.

## Source Map

- Weapon base:
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/WeaponBase.cs`
- Existing weapons:
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/WeaponKnife/`
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/WeaponHandgun/`
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/WeaponSlash/`
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/WeaponLightning/`
- Selectors:
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/TargetSelector/`
- Attack effects:
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/AttackEffects/`
- Weapon data:
  - `../../Assets/GameMain/Scripts/Entity/EntityData/Weapon/`
- Weapon table:
  - `../../Assets/GameMain/DataTables/Weapon.txt`
- Data row:
  - `../../Assets/GameMain/Scripts/DataTable/DRWeapon.cs`
- Shop integration:
  - `../../Assets/GameMain/Scripts/UI/GameScene/UseCase/ShopFormUseCase.cs`
- Entity show flow:
  - `../../Assets/GameMain/Scripts/Entity/EntityExtension.cs`

## Non-Negotiable Invariants

- Do not duplicate logic already owned by `WeaponBase`.
- Keep state transitions explicit and non-blocking.
- Keep cooldown accumulation valid even when no target is found.
- Keep attack logic and visual effect logic decoupled.
- Parse weapon-specific parameters into strong-typed `ParamsData` at data initialization time.
- Treat `Weapon.txt` `Params` as a JSON object column; empty params must use `{}`.
- Preserve compatibility with shop/inventory/UI refresh flow.

## Change Recipes

### Add a New Weapon

1. Extend `WeaponType` without reordering existing enum values.
2. Add `WeaponXxxData : WeaponData` and `WeaponXxxParamsData` for weapon-specific parameters.
3. Parse `ParamsJson` through `ParseParams<TParams>()` inside the weapon data constructor.
4. Add `WeaponXxx : WeaponBase` and implement only weapon-specific behavior.
5. Build state files under `Weapon/WeaponXxx/` with partial class layout.
6. Register display/data mapping path so `ShowWeapon` and shop purchase can instantiate correctly.

### Add or Update a Target Selector

1. Implement `ITargetSelector`.
2. Update `TargetSelectorType`.
3. Register creation in `WeaponBase.CreateSelector`.
4. Validate target semantics with current-health/runtime-health rules where applicable.

### Add or Update Attack Effect

1. Implement `IWeaponAttackEffect`.
2. Trigger effect from weapon attack state only.
3. Keep damage resolution outside effect code.

### Add or Update Weapon Parameters

1. Update `Weapon.txt` `Params` JSON object.
2. Add or update the corresponding `WeaponXxxParamsData` fields.
3. Keep key names aligned with `ParamsData` property names.
4. Read values from `ParamsData` in weapon initialization, not by manual string parsing in runtime logic.

## Validation Checklist

- Weapon can be shown, attached, and updated without exceptions.
- State machine does not stall across target loss/reacquire.
- Cooldown and range checks match design expectation.
- Damage path and effect path remain decoupled.
- `ParamsData` matches table content and default fallback behavior.
- UI/shop/inventory interactions stay stable after the change.
- Update `./references/WeaponDevelopmentSkill.md` if contracts or recommended patterns changed.
