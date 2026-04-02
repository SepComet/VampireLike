# Weapon Development Skill（VampireLike）

## 目标
本文件是 `Entity.Weapon` 体系的开发规范与速查手册。
后续新增武器、扩展参数、调整状态机或联动商店/背包时，优先按本文档执行，避免重复通读历史上下文。

## 当前架构总览
- 武器运行时入口：`WeaponBase`（`Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/WeaponBase.cs`）
- 武器具体实现：`WeaponKnife`、`WeaponHandgun`、`WeaponSlash`、`WeaponLightning`
- 目标选择策略：`ITargetSelector` + `TargetSelectorType`
- 攻击可视化：`IWeaponAttackEffect`
- 数据入口：`DRWeapon` -> `WeaponData`（及其子类）
- 实体生成：`EntityExtension.ShowWeapon`
- 商店接入：`ShopFormUseCase.CreateWeaponData`

## WeaponBase 统一职责（已上收）
`WeaponBase` 负责以下通用逻辑，子类不要重复实现：
- 生命周期模板：`OnShow/OnHide/OnAttachTo/OnDetachFrom`
- 状态机管理：`RegisterState`、`EnsureStatesBuilt`、`TransitionTo`
- 通用运行字段：`_target`、`_currAttackTimer`、`_sqrRange`
- 启用门控：`OnUpdate` 中统一 `if (!_isEnabled) return`
- 目标选择入口：`SelectTarget`、`SetTargetSelector`、`CreateSelector`
- 距离判定：`IsInRange`（基于 XZ 平面距离）
- Simulation 命中请求：`TryQueueAreaCollisionQuery`、`TryQueueSectorCollisionQuery`
- 玩家攻击属性订阅：`BindAttackStatFromOwner` / `ReleaseAttackStatSubscription`

约束：
- 不要在子类里重复实现 `WeaponBase` 已有能力。
- 行为差异优先收敛在：`BuildStates`、`Check`、`Attack`、少量专属辅助方法。

## 当前武器模板分类
### 1. `WeaponKnife`
- 模式：近身前刺 + 圆形范围命中
- 典型参数：`HitRadius`
- 适合派生：长枪、刺剑、短矛、震地锤近身版

### 2. `WeaponHandgun`
- 模式：单次 `Raycast` 瞬发命中
- 当前参数化程度较低，仍适合作为远程枪械母版继续扩展
- 适合派生：手枪、霰弹枪、狙击枪、三连发枪

### 3. `WeaponSlash`
- 模式：扇形范围命中
- 典型参数：`SectorAngle`
- 适合派生：大剑、斧头、半月斩、横扫类武器

### 4. `WeaponLightning`
- 模式：锁定目标点 + 落点范围打击
- 典型参数：`HitRadius`、`HoverHeight`
- 适合派生：闪电、陨石杖、圣光柱、空袭类武器

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
- 攻击过程中若需要多帧动画，使用 `_isAttacking` 控制状态退出时机。

## 3D 场景下的距离/朝向原则
- 射程与目标筛选：优先使用 XZ 平面距离（忽略 Y），调用 `AIUtility.GetSqrMagnitudeXZ`。
- 视觉朝向与弹道：可按武器设计决定是否使用完整 3D 向量。
  - `WeaponHandgun`：允许俯仰瞄准，逻辑上用射线命中对象判定伤害。
  - 近战地面范围类（Knife/Slash）：伤害检测建议投影到地面（XZ）再判定。
  - 落点类（Lightning）：锁点可取目标当前位置，但范围判定仍建议按地面距离收口。

## 目标选择策略规范
接口：`ITargetSelector.SelectTarget(WeaponBase weapon, IEnumerable<EntityBase> candidates, float maxSqrRange)`

现有策略：
- `NearestTargetSelector`
- `HighestHealthTargetSelector`
- `LowestHealthTargetSelector`

语义约定：
- `NearestTargetSelector` 在 Simulation 启用时优先走空间索引。
- `HighestHealth` / `LowestHealth` 当前基于运行时生命值选择目标。
- 若新增新策略，必须明确“按当前值”还是“按最大值”筛选，避免语义漂移。

扩展策略步骤：
1. 新建 selector 类并实现 `ITargetSelector`。
2. 更新 `TargetSelectorType` 枚举。
3. 在 `WeaponBase.CreateSelector` 中注册。
4. 武器在 `OnWeaponShow` 或初始化阶段选择策略。

## 攻击可视化效果规范
接口：`IWeaponAttackEffect.Play(WeaponBase weapon, Vector3 position, EntityBase target, float radius)`

约定：
- 可视化逻辑与伤害逻辑解耦。
- 武器类只负责触发 `Play`，不把可视化细节塞回武器核心逻辑。
- 当前阶段允许临时对象创建；后续若有性能压力再统一对象池化。
- 命中特效、范围预警、扇形描边都属于 effect 层，不属于伤害层。

## 数据层规范（DRWeapon / WeaponData）
### `DRWeapon`
提供通用字段：
- `Attack`
- `Cooldown`
- `AttackRange`
- `AttackSoundId`
- `ParamsJson`
- `Pramas`
- `Modifiers`

约定：
- `Params` 列现在使用标准 JSON 对象。
- 空参数统一写 `{}`。
- 不再兼容 `[]`。
- `Pramas` 保留为描述/UI 的兼容字典视图，不再作为武器逻辑主读取入口。

### `WeaponData`
提供公共武器字段与通用解析入口：
- 公共战斗字段：`Attack`、`Cooldown`、`AttackRange`
- 公共展示字段：`Title`、`IconAssetName`、`Rarity`、`Price`
- 参数入口：`ParamsJson`、`Params`
- 通用强类型解析：`ParseParams<TParams>()`

约定：
- 参数解析优先在数据层完成。
- 武器逻辑层读取强类型 `ParamsData`，不要散落字符串 `Parse`。
- 只有描述/UI 等弱类型场景才继续读取 `Params` 字典。

### 具体武器数据子类
每种武器数据子类都应持有自己的 `ParamsData`：
- `WeaponKnifeData -> WeaponKnifeParamsData`
- `WeaponHandgunData -> WeaponHandgunParamsData`
- `WeaponSlashData -> WeaponSlashParamsData`
- `WeaponLightningData -> WeaponLightningParamsData`

当前已接通字段：
- `WeaponKnifeParamsData`
  - `HitRadius`
- `WeaponHandgunParamsData`
  - 暂无字段
- `WeaponSlashParamsData`
  - `SectorAngle`
- `WeaponLightningParamsData`
  - `HitRadius`
  - `HoverHeight`

JSON 约束：
- key 名应与 `ParamsData` 属性名一致。
- 统一使用 JSON 对象，不要再使用自定义 KV 串。
- 不建议在 `ParamsJson` 中使用制表符和跨行内容，避免 txt 表分列出错。

## 新增武器标准流程
1. 定义枚举
   - 更新 `WeaponType`，保持递增值，避免重排已有值。

2. 建立数据类
   - 新建 `WeaponXxxData : WeaponData`。
   - 新建 `WeaponXxxParamsData`。
   - 在构造阶段调用 `ParseParams<TParams>()`，把参数初始化为强类型字段。

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

6. 接入实体展示与数据表
   - 确保 `DRWeapon` / `DREntity` 配置齐全。
   - `EntityExtension.ShowWeapon` 可正确映射到 `Entity.Weapon.WeaponXxx`。
   - 商店/背包创建入口能正确构造 `WeaponXxxData`。

7. 联动系统
   - 背包、商店购买/出售、UI 展示、事件流刷新。

8. 验证点
   - 武器能正确生成、附着、更新。
   - `ParamsData` 与数据表一致。
   - 描述文本仍正确展示。
   - Simulation 模式和非 Simulation 模式都能命中。

## 扩展优先级建议
### 第一批：低成本高收益
- 长枪 / 刺剑
  - 基于 `WeaponKnife`
- 大剑 / 半月斩
  - 基于 `WeaponSlash`
- 战锤 / 震地锤
  - 基于 `WeaponLightning` 或 `WeaponKnife`
- 霰弹枪
  - 基于参数化后的 `WeaponHandgun`
- 狙击枪
  - 基于参数化后的 `WeaponHandgun`
- 陨石杖 / 圣光柱
  - 基于 `WeaponLightning`

### 第二批：中成本扩展
- 链式闪电
- 穿透弹 / 火球
- 地雷 / 陷阱
- 回旋镖

### 暂缓项
- 持续激光
- 喷火器
- 环绕飞剑
- 常驻法球
- 状态异常驱动的复杂武器流派

原因：
- 当前武器主流程仍是单次攻击结算。
- 持续伤害、异常状态和常驻 orbit 行为尚未形成统一挂点。

## `WeaponHandgun` 后续建议
当前 `WeaponHandgun` 参数化仍偏弱，建议作为下一阶段母版优先扩展。

建议新增字段：
- `PelletCount`
- `SpreadAngle`
- `PenetrationCount`
- `FireOriginOffsetX`
- `FireOriginOffsetY`
- `FireOriginOffsetZ`
- `HitMarkerSize`
- `HitMarkerYOffset`
- `HitMarkerDuration`

完成后可快速派生：
- 手枪
- 霰弹枪
- 狙击枪
- 三连发枪

## 代码检查清单（提交前）
- 是否重复实现了 `WeaponBase` 已有通用逻辑。
- 状态流转是否会卡死或漏转场。
- 冷却计时是否在无目标时仍正常累积。
- 目标失效（null/Unavailable/死亡）是否安全处理。
- 命中检测是否符合武器设计（地面范围/射线/扇形/落点）。
- 数据参数是否全部收敛到 `ParamsData`。
- 可视化是否与伤害逻辑解耦。
- 是否正确订阅/解绑攻击属性（或复用基类方法）。
- 商店、背包、武器描述是否仍保持兼容。

## 已知注意点
- 目录中存在 `Pramas` 命名拼写历史包袱，当前保持兼容即可。
- 文档中的“规范”优先级高于历史实现；历史实现若偏离，按本规范逐步收敛。
- 当前更推荐“公共 `WeaponData` + 专属 `ParamsData`”结构，不建议把每种武器做成一套完全独立的数据总线。
