# CodeX TODO

## SimulationWorld 路线收敛

### 当前结论

- 当前 `SimulationWorld` 唯一还成体系的执行路径，就是现有 Burst Job 管线。
- 这条管线已经覆盖：
  - 敌人移动：`EnemyMovementBurstJob`
  - 敌人分离：`BuildEnemySeparationBucketsBurstJob` + `EnemySeparationBurstJob`
  - 投射物移动：`ProjectileMovementBurstJob`
  - 碰撞 broad-phase：`BuildCollisionBucketsBurstJob` + `QueryCollisionCandidatesBurstJob`
- 与之并存的旧路径仍然散落在实体和组件里：
  - `MovementComponent.OnUpdate()` 仍在直接推进位移
  - `EnemySeparationSolverProvider` 仍在负责旧互斥逻辑
  - `EnemyBase` / `MeleeEnemy` / `RemoteEnemy` / `EnemyProjectile` / `NearestTargetSelector` 仍在用 `UseSimulationMovement` 做双路径分支
- 文档中的 `UseJobSimulation` / `UseBurstJobs` 当前只有说明，没有代码实现。
- 因此当前要做的不是“抛弃 Burst 路线”，而是反过来：
  - 保留 `SimulationWorld` 的 Burst 逻辑作为唯一执行路径
  - 删除所有旧组件驱动/旧 fallback 路径
  - 把组件层收敛成 `SimulationWorld` 的输入、注册和表现壳层

### 目标定义

- `SimulationWorld` 成为唯一的运行时仿真执行入口。
- 实体自身不再计算“下一帧位置”，而是只向 `SimulationWorld` 提供输入和配置。
- `MovementComponent` 不再直接改 `Transform`，只负责：
  - 保存移动开关/方向/速度等输入态
  - 向 `SimulationWorld` 注册或同步这些输入
- 实体表现层只消费 `SimulationWorld` 的输出结果，不再自己推进位移或互斥。
- 删除所有“Simulation / 非 Simulation”双路径分支，避免行为在两套逻辑里分叉。

### 必改项

#### 1. Tick 主入口收敛

- 文件：`Assets/GameMain/Scripts/Simulation/SimulationWorld.cs`
- 处理：
  - 保留 `TickSimulationPipeline(in SimulationTickContext context)` 作为唯一执行主入口
  - 删除“开关关闭后直接跳过 SimulationWorld”的旧语义
  - 重定义 `UseSimulationMovement`：
    - 要么删除
    - 要么仅保留为全局停机开关，而不是双路径路由开关
- 备注：
  - `GameStateBattle.OnUpdate` 目前已经把 `SimulationWorld.Tick(...)` 放在战斗主循环里，这个方向是对的

#### 2. 保留并固化 Burst/Job 敌人执行面

- 文件：`Assets/GameMain/Scripts/Simulation/Jobs/SimulationWorld.EnemyJobs.cs`
- 文件：`Assets/GameMain/Scripts/Simulation/JobStruct/EnemyMovementBurstJob.cs`
- 文件：`Assets/GameMain/Scripts/Simulation/JobStruct/EnemySeparationBurstJob.cs`
- 文件：`Assets/GameMain/Scripts/Simulation/JobStruct/BuildEnemySeparationBucketsBurstJob.cs`
- 处理：
  - 保留 Burst Job 调度，明确这就是敌人的唯一移动/互斥路径
  - 把实体侧“追击/停下/朝向目标”的输入全部规范成写入 sim state，而不是各实体自己推动
  - 明确 `_enemies` 是敌人移动和互斥的唯一状态源

#### 3. 保留并固化 Burst/Job 投射物执行面

- 文件：`Assets/GameMain/Scripts/Simulation/Jobs/SimulationWorld.ProjectileJobs.cs`
- 文件：`Assets/GameMain/Scripts/Simulation/JobStruct/ProjectileMovementBurstJob.cs`
- 处理：
  - 保留投射物移动 Job，作为唯一的投射物位置推进路径
  - 删除 `EnemyProjectile.OnUpdate` 中的自驱动移动和寿命推进逻辑
  - `EnemyProjectile` 只负责 show/hide、碰撞体开关、表现同步

#### 4. 保留并固化 Burst/Job 碰撞 broad-phase

- 文件：`Assets/GameMain/Scripts/Simulation/Jobs/SimulationWorld.CollisionBroadPhase.cs`
- 文件：`Assets/GameMain/Scripts/Simulation/JobStruct/QueryCollisionCandidatesBurstJob.cs`
- 处理：
  - 保留现有碰撞分桶和候选查询 Job
  - 把 area/sector/projectile 命中统一视为 `SimulationWorld` 能力，不再给实体侧保留另一套 fallback 查询逻辑
  - 确认武器系统全部通过 `SimulationWorld` 提供的查询/结算能力工作

#### 5. Native 通道与 Job 数据层保留，但去掉兼容性残留

- 文件：`Assets/GameMain/Scripts/Simulation/DataChannel/SimulationWorld.JobDataChannel.cs`
- 文件：`Assets/GameMain/Scripts/Simulation/DataChannel/SimulationWorld.JobDataLifecycle.cs`
- 文件：`Assets/GameMain/Scripts/Simulation/DataChannel/SimulationWorld.JobDataConversion.cs`
- 文件：`Assets/GameMain/Scripts/Simulation/DataChannel/SimulationWorld.JobOutputCommit.cs`
- 文件：`Assets/GameMain/Scripts/Simulation/JobStruct/*`
- 处理：
  - 保留 Native 通道和 sim/job 转换层
  - 清理“只为旧测试和旧双路径兼容保留”的字段与注释
  - 把 job output 回写 `_enemies/_projectiles` 明确为正式架构，而不是过渡方案
- 备注：
  - 当前 `_collisionQueryInputs` 和 `_areaCollisionRequests` 还被测试直接反射，后续需要改测试而不是继续迁就反射

#### 6. Presentation 回写链路收敛

- 文件：`Assets/GameMain/Scripts/Simulation/Presentation/SimulationWorld.TransformSync.cs`
- 文件：`Assets/GameMain/Scripts/Simulation/Presentation/SimulationWorld.HitPresentation.cs`
- 处理：
  - 保留 `TransformSync` 从 `_enemies/_projectiles` 回写实体表现
  - 删除实体自身 `OnUpdate` 中对位置和朝向的直接推进
  - 明确表现层只消费 sim 输出，不再写回 sim 逻辑状态

#### 7. 生命周期注册与数据容器职责重定义

- 文件：`Assets/GameMain/Scripts/Simulation/SimulationWorld.EntitySync.cs`
- 文件：`Assets/GameMain/Scripts/Simulation/SimulationWorld.EntityToSimData.cs`
- 文件：`Assets/GameMain/Scripts/Simulation/SimulationWorld.SimEntityState.cs`
- 文件：`Assets/GameMain/Scripts/Simulation/SimulationWorld.RuntimeModules.cs`
- 处理：
  - 明确 `SimulationWorld` 持有 `_enemies/_projectiles/_pickups` 作为正式运行时状态
  - 补“组件输入 -> sim state”同步接口，替代实体自己推进逻辑
  - 将注册/反注册、初始数据灌入、索引维护继续收拢在 `SimulationWorld`

#### 8. 战斗入口与 GameEntry 依赖固化

- 文件：`Assets/GameMain/Scripts/Procedure/Game/GameStateBattle.cs`
- 文件：`Assets/GameMain/Scripts/Procedure/Game/ProcedureGame.cs`
- 文件：`Assets/GameMain/Scripts/Base/GameEntry.Custom.cs`
- 处理：
  - 保持 `SimulationWorld` 是战斗主循环核心组件
  - `ClearSimulationState()` 继续保留，但要确认只清 sim 状态，不引入双路径 reset 语义
  - `GameEntry` 自动挂载 `SimulationWorld` 是合理的，应继续保留

### 需要一起改的外围依赖

#### 9. MovementComponent 收敛为输入/注册层

- 文件：`Assets/GameMain/Scripts/Components/MovementComponent.cs`
- 文件：`Assets/GameMain/Scripts/Utility/EnemySeperator/EnemySeparationSolverProvider.cs`
- 处理：
  - 删除 `MovementComponent.OnUpdate()` 中直接改 `Transform.position` 的逻辑
  - 删除对 `EnemySeparationSolverProvider.Resolve/Register/Unregister` 的运行时依赖
  - `MovementComponent` 仅保留：
    - 速度/方向/移动开关
    - 互斥半径/迭代参数
    - 向 `SimulationWorld` 注册、反注册、同步输入
- 备注：
  - 这部分是你刚说的核心：位置计算归 `SimulationWorld`，`MovementComponent` 只负责注册和输入

#### 10. 实体侧双路径和自驱动移动清理

- 文件：`Assets/GameMain/Scripts/Entity/EntityLogic/Enemy/EnemyBase.cs`
- 文件：`Assets/GameMain/Scripts/Entity/EntityLogic/Enemy/MeleeEnemy.cs`
- 文件：`Assets/GameMain/Scripts/Entity/EntityLogic/Enemy/RemoteEnemy.cs`
- 文件：`Assets/GameMain/Scripts/Entity/EntityLogic/Enemy/EnemyProjectile.cs`
- 文件：`Assets/GameMain/Scripts/Entity/EntityLogic/Player.cs`
- 文件：`Assets/GameMain/Scripts/Entity/EntityLogic/Weapon/TargetSelector/NearestTargetSelector.cs`
- 处理：
  - 删除 `UseSimulationMovement` 分支判断
  - 删除 `MeleeEnemy` / `RemoteEnemy` 中对 `_movementComponent.OnUpdate()` 的调用
  - 删除 `Player` 中通过 `MovementComponent.OnUpdate()` 推进玩家位置的逻辑，改为只提交输入方向
  - 删除 `EnemyProjectile` 中“Simulation 驱动 or 自驱动”双模式
  - `NearestTargetSelector` 直接依赖 `SimulationWorld` 空间索引，不再 fallback 到遍历分支

#### 11. Debug 面板与旧互斥调试项清理

- 文件：`Assets/GameMain/Scripts/CustomComponent/DebugPanel/RuntimeDebugPanelComponent.cs`
- 处理：
  - 保留 `SimulationWorld` 的碰撞和 broad-phase 指标
  - 删除 `EnemySeparationSolverProvider` 的旧 solver 切换 UI 和相关文案
  - 面板上不再出现“双路径”或“旧 solver”调试入口

#### 12. 测试整体重建

- 文件：`Assets/Tests/Simulation/EditMode/SimulationWorldTickTests.cs`
- 文件：`Assets/Tests/Simulation/PlayMode/SimulationWorldPlayModeTests.cs`
- 处理：
- 当前测试强依赖：
    - `_useSimulationMovement`
    - `_collisionQueryInputs`
    - `_areaCollisionRequests`
    - Job 通道与碰撞候选计数
  - 一旦抛弃现有 Burst/Job 路线，这批测试大概率需要整体重写
- 建议：
  - 不要继续维护“反射私有字段 + 验证 Native 通道”的测试模式
  - 改为验证外部可观察行为：
    - 敌人移动
    - 投射物生命周期
    - 范围命中结果
    - 实体 hide/remove 生命周期

#### 13. 文档同步

- 文件：`docs/P1.5 Simulation-Supplement.md`
- 文件：`docs/P2 Job System + Burst 落地.md`
- 文件：`docs/TodoList.md`
- 处理：
  - 标明当前代码已不再保留 P1.5 的 `SimulationWorld` 执行路径
  - 删除或修正文档里关于 `SetUseSimulationMovement(bool)`、`UseJobSimulation`、`UseBurstJobs` 的失真描述

### 推荐实施顺序

1. 先确认唯一执行路径
- `SimulationWorld` Burst 管线作为唯一运行时执行路径
- 实体和组件只保留输入、注册、表现职责

2. 再处理战斗入口
- 先改 `GameStateBattle` / `GameEntry` / `ProcedureGame`
- 让运行时明确依赖当前 Burst 管线，不再保留双路径语义

3. 再清理旧组件驱动路径
- 先收敛 `MovementComponent`
- 再删敌人/玩家/投射物实体里的自驱动移动
- 再删旧 fallback 查询和旧互斥 solver

4. 最后重建测试和文档
- 先让行为稳定
- 再补新的回归测试和文档

### 当前建议

- 不建议试图“恢复 P1.5 开关”。
  - 当前代码里并没有一条完整可切换的 P1.5 `SimulationWorld` 路径，恢复开关只会制造更多伪分支。
- 更合理的做法是直接承认现状：
  - 保留 Burst 化 `SimulationWorld` 作为唯一执行路径
  - 删掉所有组件自驱动和 fallback 分支
- 下一步应按上面的收敛顺序推进，不要再尝试维护“双路径可切换”。

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
