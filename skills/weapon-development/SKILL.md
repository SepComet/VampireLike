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
   - data contract (`DRWeapon`, `WeaponData`)
3. Keep behavior compatibility with current gameplay loop and UI/event chain.

## Source Map

- Weapon base: `../../Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/WeaponBase.cs`
- Existing weapons:
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/WeaponKnife/`
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/WeaponHandgun/`
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/WeaponSlash.cs`
- Selectors:
  - `../../Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/TargetSelector/`
- Weapon data:
  - `../../Assets/GameMain/Scripts/Entity/EntityData/Weapon/`
- Entity show flow:
  - `../../Assets/GameMain/Scripts/Entity/EntityExtension.cs`

## Non-Negotiable Invariants

- Do not duplicate logic already owned by `WeaponBase`.
- Keep state transitions explicit and non-blocking.
- Keep cooldown accumulation valid even when no target is found.
- Keep attack logic and visual effect logic decoupled.
- Use safe parsing (`TryParse` + defaults) for runtime weapon parameters.
- Preserve compatibility with shop/inventory/UI refresh flow.

## Change Recipes

### Add a New Weapon

1. Extend `WeaponType` without reordering existing enum values.
2. Add `WeaponXxxData : WeaponData` for strong-typed fields.
3. Add `WeaponXxx : WeaponBase` and implement only weapon-specific behavior.
4. Build state files under `Weapon/WeaponXxx/` with partial class layout.
5. Register display/data mapping path so `ShowWeapon` can instantiate correctly.

### Add or Update a Target Selector

1. Implement `ITargetSelector`.
2. Update `TargetSelectorType`.
3. Register creation in `WeaponBase.CreateSelector`.
4. Validate target semantics with current-health rules where applicable.

### Add or Update Attack Effect

1. Implement `IWeaponAttackEffect`.
2. Trigger effect from weapon attack state only.
3. Keep damage resolution outside effect code.

## Validation Checklist

- Weapon can be shown, attached, and updated without exceptions.
- State machine does not stall across target loss/reacquire.
- Cooldown and range checks match design expectation.
- Damage path and effect path remain decoupled.
- UI/shop/inventory interactions stay stable after the change.
- Update `./references/WeaponDevelopmentSkill.md` if contracts changed.
