# CodeX TODO

## Weapon 现状梳理

### 1. 数据层结构

- `Assets/GameMain/DataTables/Weapon.txt`
  - 武器基础数据表。
  - 当前 `Params` 列已经切换为标准 JSON 对象。
  - 约束：
    - 空参数统一写 `{}`
    - 不再兼容 `[]`
    - key 名应与对应 `ParamsData` 属性名一致

- `Assets/GameMain/Scripts/DataTable/DRWeapon.cs`
  - 负责解析武器表基础字段。
  - 保留两份参数视图：
    - `ParamsJson`
      - 原始 JSON 字符串，供 `WeaponData` 强类型反序列化使用。
    - `Pramas`
      - 由 `ParamsJson` 转成的 `Dictionary<string, string>`。
      - 仅用于描述/UI 等弱类型读取场景。

- `Assets/GameMain/Scripts/Entity/EntityData/Weapon/WeaponData.cs`
  - 保留所有武器共用字段：
    - `Attack`
    - `Cooldown`
    - `AttackRange`
    - `Price`
    - `Rarity`
    - `Modifiers`
    - `ParamsJson`
    - `Params`
  - 提供 `ParseParams<TParams>()` 作为武器子类的强类型参数解析入口。

- `Assets/GameMain/Scripts/Entity/EntityData/Weapon/*`
  - 每个具体武器子类持有自己的 `ParamsData`：
    - `WeaponKnifeData -> WeaponKnifeParamsData`
    - `WeaponHandgunData -> WeaponHandgunParamsData`
    - `WeaponSlashData -> WeaponSlashParamsData`
    - `WeaponLightningData -> WeaponLightningParamsData`

### 2. 逻辑层结构

- `Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/WeaponBase.cs`
  - 武器统一基类。
  - 负责：
    - 生命周期
    - 状态机切换
    - 选敌
    - Simulation area/sector query 接入
    - 绑定玩家攻击属性

- 当前已有四种武器实现模板：
  - `WeaponKnife`
    - 近身前刺 + 圆形范围命中
  - `WeaponHandgun`
    - 单次 Raycast 瞬发命中
  - `WeaponSlash`
    - 扇形范围命中
  - `WeaponLightning`
    - 锁定目标点 + 落点范围打击

- 参数读取方式
  - 已改为在具体武器中直接读取对应 `ParamsData`
  - 不再在武器逻辑中手动解析字符串参数

### 3. 当前已接通的参数

- `WeaponKnifeParamsData`
  - `HitRadius`

- `WeaponHandgunParamsData`
  - 暂无字段，待扩展

- `WeaponSlashParamsData`
  - `SectorAngle`

- `WeaponLightningParamsData`
  - `HitRadius`
  - `HoverHeight`

### 4. 当前结构的优点

- 公共字段仍集中在 `WeaponData`，不会把通用逻辑拆散
- 武器专属参数已经强类型化，初始化更稳定
- 数据表可读性比旧的 KV 字符串格式更高
- 新武器扩展时，可以复用：
  - 表
  - `DRWeapon`
  - `WeaponData`
  - `WeaponBase`
  - AttackEffect
  - Simulation area/sector query

### 5. 当前结构的限制

- 仍然不是“纯配置驱动行为”
  - 行为差异主要还在具体武器类里
- `Handgun` 参数化程度还不够
  - 目前还不适合直接高效派生霰弹枪、狙击枪、 burst 枪
- `Pramas` 命名拼写仍保留旧名字
  - 目前为了兼容存量调用，暂不改

## Weapon 扩展计划

### P0: 稳定当前数据链路

- 检查 `Weapon.txt` 中全部行：
  - `Params` 必须都是 JSON 对象
  - 空参数统一为 `{}`
- 检查后续新增字段时：
  - 数据表 key 与 `ParamsData` 属性名保持一致
- 补一轮基础验证：
  - 商店描述
  - 玩家初始武器生成
  - 商店购买武器生成

### P1: 先把 Handgun 参数化做完整

目标：
- 把 `WeaponHandgun` 做成远程枪械母版，而不是只有一把“手枪”

建议新增参数：
- `PelletCount`
- `SpreadAngle`
- `PenetrationCount`
- `FireOriginOffsetX`
- `FireOriginOffsetY`
- `FireOriginOffsetZ`
- `HitMarkerSize`
- `HitMarkerYOffset`
- `HitMarkerDuration`

落地后可直接派生：
- 手枪
- 霰弹枪
- 狙击枪
- 三连发手炮

### P2: 优先做低成本高收益的新武器

优先顺序建议：

1. 长枪 / 刺剑
- 基于 `WeaponKnife`
- 重点调：
  - 前刺距离
  - 命中半径
  - 冷却

2. 大剑 / 半月斩
- 基于 `WeaponSlash`
- 重点调：
  - `SectorAngle`
  - 攻击范围
  - 动画时长

3. 战锤 / 震地锤
- 基于 `WeaponLightning` 或 `WeaponKnife`
- 重点调：
  - 落点半径
  - 前摇
  - 低频高伤

4. 霰弹枪
- 基于参数化后的 `WeaponHandgun`
- 重点调：
  - 散射
  - 多 pellet
  - 近距离爆发

5. 狙击枪
- 基于参数化后的 `WeaponHandgun`
- 重点调：
  - 单发高伤
  - 超远射程
  - 慢冷却

6. 陨石杖 / 圣光柱
- 基于 `WeaponLightning`
- 重点调：
  - `HoverHeight`
  - 爆炸半径
  - 冷却

### P3: 中成本扩展

1. 链式闪电
- 在首目标命中后，继续寻找附近目标
- 需要新增：
  - 连锁次数
  - 连锁半径
  - 每跳衰减

2. 穿透弹 / 火球
- 复用现有 projectile/simulation 基础
- 需要明确：
  - 穿透次数
  - 命中后是否爆炸

3. 地雷 / 陷阱
- 本质是延时触发 area hit
- 需要新增：
  - 布置后触发时机
  - 持续时间
  - 触发半径

4. 回旋镖
- 需要双阶段投射物状态
- 成本高于普通枪械/范围武器

### P4: 暂缓项

以下方向暂不建议优先投入：

- 持续激光
- 喷火器
- 环绕飞剑
- 常驻法球
- 冰冻/中毒/击退等状态驱动武器流派

原因：
- 当前武器框架核心仍是“单次攻击结算”
- 持续伤害与异常状态还没有形成统一挂点

## 新武器接入步骤模板

1. 在 `Weapon.txt` 新增一行
- 配好基础字段
- `Params` 写 JSON 对象

2. 新增 `WeaponType`
- 在 `Assets/GameMain/Scripts/Definition/Enum/WeaponType.cs`

3. 新增武器数据子类
- 新建 `WeaponXXXData`
- 新建 `WeaponXXXParamsData`
- 在构造里调用 `ParseParams<TParams>()`

4. 新增武器逻辑类
- 继承 `WeaponBase`
- 接入状态机
- 读取 `ParamsData`

5. 接入生成入口
- 玩家初始武器
- 商店购买武器
- 其他掉落/奖励入口

6. 验证点
- 武器生成正确
- 参数生效正确
- 描述文本正确
- Simulation 模式和非 Simulation 模式都能命中
