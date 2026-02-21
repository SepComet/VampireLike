# Weapon Development Skill（VampireLike）

## 目标
本文件是 `Entity.Weapon` 体系的开发规范与速查手册。
后续新增武器或扩展机制时，优先按本文档执行，避免重复通读历史上下文。

## 当前架构总览
- 武器运行时入口：`WeaponBase`（`Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/WeaponBase.cs`）
- 武器具体实现：`WeaponKnife`、`WeaponHandgun`、`WeaponSlash`
- 目标选择策略：`ITargetSelector` + `TargetSelectorType`
- 攻击可视化：`IWeaponAttackEffect`
- 数据入口：`DRWeapon` -> `WeaponData`（及其子类）
- 实体生成：`EntityExtension.ShowWeapon`

## WeaponBase 统一职责（已上收）
`WeaponBase` 负责以下通用逻辑，子类不要重复实现：
- 生命周期模板：`OnShow/OnHide/OnAttachTo/OnDetachFrom`
- 状态机管理：`RegisterState`、`EnsureStatesBuilt`、`TransitionTo`
- 通用运行字段：`_target`、`_currAttackTimer`、`_sqrRange`
- 启用门控：`OnUpdate` 中统一 `if (!_isEnabled) return`
- 目标选择入口：`SelectTarget`、`SetTargetSelector`、`CreateSelector`
- 距离判定：`IsInRange`（基于 XZ 平面距离）
- 玩家攻击属性订阅：`BindAttackStatFromOwner` / `ReleaseAttackStatSubscription`

## 状态机约定
统一状态枚举：
- `Idle`
- `Check_OutRange`
- `Check_InRange`
- `Attack`
- `Disabled`（可选）

推荐流程：
1. `Idle`：查找目标，计时器持续累积。
2. `Check_OutRange`：有目标但不在攻击范围，继续转向并累积计时。
3. `Check_InRange`：在攻击范围内，若 `currAttackTimer >= Cooldown` 切 `Attack`。
4. `Attack`：执行攻击，重置计时器，结束后回 `Check_InRange`。

关键规则：
- 即使没有目标，也允许蓄力（计时器持续走）。
- 一旦进入 `Check_InRange` 且冷却已满，应立即触发攻击。

## 3D 场景下的距离/朝向原则
- 射程与目标筛选：优先使用 XZ 平面距离（忽略 Y），调用 `AIUtility.GetSqrMagnitudeXZ`。
- 视觉朝向与弹道：可按武器设计决定是否使用完整 3D 向量。
  - `WeaponHandgun`：允许俯仰瞄准，逻辑上用射线命中对象判定伤害。
  - 近战地面范围类（Knife/Slash）：伤害检测建议投影到地面（XZ）再判定。

## 目标选择策略规范
接口：`ITargetSelector.SelectTarget(WeaponBase weapon, IEnumerable<EntityBase> candidates, float maxSqrRange)`

现有策略：
- `NearestTargetSelector`
- `HighestHealthTargetSelector`
- `LowestHealthTargetSelector`

语义约定：
- `HighestHealth` / `LowestHealth` 必须按“当前血量”筛选。
- 当前实现读取 `HealthComponent.CurrentHealth`。

扩展策略步骤：
1. 新建 selector 类并实现 `ITargetSelector`。
2. 更新 `TargetSelectorType` 枚举。
3. 在 `WeaponBase.CreateSelector` 中注册。
4. 武器在 `OnWeaponShow` 或构造阶段选择策略。

## 攻击可视化效果规范
接口：`IWeaponAttackEffect.Play(WeaponBase weapon, Vector3 position, EntityBase target, float radius)`

约定：
- 可视化逻辑与伤害逻辑解耦。
- 武器类只负责触发 `Play`，不把可视化细节塞回武器核心逻辑。
- 当前阶段允许临时对象创建；后续若有性能压力再统一对象池化。

## 数据层规范（DRWeapon / WeaponData）
`DRWeapon` 提供通用字段：
- `Attack`、`Cooldown`、`AttackRange`、`AttackSoundId`
- `Pramas`（字典，Key 建议统一转小写）
- `Modifiers`

约定：
- 参数解析尽量在数据层/初始化阶段完成。
- 武器逻辑层读取 `WeaponData` 的强类型结果，不要散落 `Parse`。
- 解析时必须容错：优先 `TryParse` + 默认值，避免运行时异常。

## 新增武器标准流程
1. 定义枚举
   - 更新 `WeaponType`（保持递增值，避免重排已有值）。

2. 建立数据类
   - 新建 `WeaponXxxData : WeaponData`。
   - 如果有独有参数，提供强类型字段或统一初始化逻辑。

3. 建立行为类
   - 新建 `WeaponXxx : WeaponBase`。
   - 只实现差异化逻辑：`BuildStates`、`Check`、`Attack`、特有检测/动画。

4. 建立状态类
   - 推荐放在 `Weapon/WeaponXxx/` 目录，采用 partial 组织：
     - `WeaponXxx.cs`
     - `WeaponXxx.IdleState.cs`
     - `WeaponXxx.CheckOutRangeState.cs`
     - `WeaponXxx.CheckInRangeState.cs`
     - `WeaponXxx.AttackState.cs`

5. 建立可视化（可选）
   - 新建 `WeaponXxxAttackEffect : IWeaponAttackEffect`，在武器中组合调用。

6. 若为远程武器，建立子弹实体
   - `BulletXxx : Bullet`
   - `BulletXxxData : BulletData`
   - 通过武器赋予伤害/阵营参数。
   - 自动销毁应走对象池回收。

7. 接入实体展示与数据表
   - 确保 `DRWeapon` / `DREntity` 配置齐全。
   - `EntityExtension.ShowWeapon` 可正确映射到 `Entity.Weapon.WeaponXxx`。

8. 联动系统
   - 背包、商店购买/出售、UI 展示、事件流刷新。

## 代码检查清单（提交前）
  - 是否重复实现了 `WeaponBase` 已有通用逻辑。
    - 状态流转是否会卡死或漏转场。
    - 冷却计时是否在无目标时仍正常累积。
    - 目标失效（null/Unavailable/死亡）是否安全处理。
    - 命中检测是否符合武器设计（地面范围/射线/扇形）。
    - 数据参数是否全部容错解析。
    - 可视化是否与伤害逻辑解耦。
    - 是否正确订阅/解绑攻击属性（或复用基类方法）。

## 已知注意点
  - 目录中存在 `Pramas` 命名拼写历史包袱，保持兼容即可。
    - 文档中的“规范”优先级高于历史实现；历史实现若偏离，按本规范逐步收敛。
