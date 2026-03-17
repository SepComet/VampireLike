# SimulationWorld Architecture Specification

## 文档定位
本文件是 `SimulationWorld` 的架构规范与扩展开发约束。

用途分为两部分：
- 作为当前 `SimulationWorld` 实现的架构总览，说明模块职责、依赖边界、运行链路和数据所有权。
- 作为后续扩展、重构、性能优化和回归修复时的约束文档，防止破坏核心不变量。

文档与实现冲突时，当前分支源码优先；提交前必须同步修正文档。

## 适用范围
- `Assets/GameMain/Scripts/Simulation/*`
- `Assets/GameMain/Scripts/Procedure/Game/GameStateBattle.cs`
- `Assets/GameMain/Scripts/Procedure/Game/ProcedureGame.cs`
- `Assets/GameMain/Scripts/Utility/AIUtility.cs`
- `Assets/Tests/Simulation/EditMode/*`
- `Assets/Tests/Simulation/PlayMode/*`

## 架构目标
- 将战斗中的敌人、投射物、掉落物运行时状态收口到统一仿真容器。
- 将热路径逻辑与 Unity 表现层解耦，避免在仿真阶段直接读写 `Transform`。
- 为 Job/Burst 提供稳定的数据通道、生命周期管理和主线程结算收口点。
- 保持 `SimulationWorld` 对外是单一战斗调度入口，而不是分散的业务入口集合。
- 保证扩展新仿真对象或新碰撞规则时，能够沿着既有管线接入，而不是旁路修改。

## 非目标
- 不负责完整战斗规则定义。伤害公式、碰撞业务语义仍由 `AIUtility` 和实体逻辑承担。
- 不负责实体创建策略。实体创建与隐藏仍由外部流程和 Entity 系统负责。
- 不追求全局 ECS 化。本模块仍以 `SimulationWorld + partial + Native 容器` 为中心组织。
- 不在 Job 中直接驱动表现层、事件系统或 Unity 对象生命周期。

## 外部依赖与系统边界
`SimulationWorld` 处于战斗流程中层，位于 `GameStateBattle` 和具体实体逻辑之间。

上游依赖：
- `GameStateBattle.OnUpdate` 驱动每帧 `Tick`。
- `GameEntry.Event` 提供实体显示/隐藏事件，用于同步仿真容器生命周期。
- `GameEntry.Entity` 提供实体查询、隐藏和表现事件消费。

下游协作：
- `AIUtility` 负责伤害与碰撞业务结算。
- Enemy/Projectile/Drop 实体提供初始化所需运行时数据。
- Presentation 子模块负责把仿真结果写回表现层。

边界要求：
- 外部业务代码不得直接增删 `_enemies`、`_projectiles`、`_pickups`。
- 外部业务代码不得绕过 `SimulationWorld` 直接维护仿真索引。
- `SimulationWorld` 不直接拥有实体生成权，只消费实体生命周期事件。

## 模块结构
`SimulationWorld` 使用 `partial` 拆分，职责按以下边界划分：

- `SimulationWorld.cs`
  - 核心组件入口、主状态容器、基础依赖、Unity 生命周期入口。
- `SimulationWorld.SimEntityState.cs`
  - 敌人、投射物、掉落物的仿真态创建、更新、删除和清空。
- `SimulationWorld.EntitySync.cs`
  - 监听实体 show/hide 事件，将实体生命周期映射到仿真容器。
- `DataChannel/SimulationWorld.JobDataChannel.cs`
  - Native 容器持有、初始化、清理、容量准备、仿真数据到 Job 数据的转换。
- `Jobs/SimulationWorld.EnemyJobs.cs`
  - 每帧仿真主编排、敌人移动与互斥分离 Job 调度。
- `Jobs/SimulationWorld.ProjectileJobs.cs`
  - 投射物移动、寿命处理、越界回收。
- `Jobs/SimulationWorld.CollisionPipeline.cs`
  - 投射物与区域碰撞查询构建、候选筛选、主线程命中结算。
- `SimulationWorld.TargetSelectionSpatialIndex.cs`
  - 敌人目标选择空间索引。
- `Presentation/SimulationWorld.TransformSync.cs`
  - `LateUpdate` 表现写回。
- `Presentation/SimulationWorld.HitPresentation.cs`
  - 命中事件的表现消费桥。

## 核心数据所有权
### 主容器
- `_enemies`
- `_projectiles`
- `_pickups`

这些容器是真实仿真态所有者。Job 输入输出缓冲只是当前帧的镜像通道，不是持久源数据。

### 绑定关系
- `EntityBinding` 维护 `EntityId <-> SimulationIndex` 双向映射。
- 容器删除使用 `swap-back`。
- 发生尾元素覆盖时，必须同步 `RemapIndex`。
- 删除完成后再 `Unbind`，避免索引悬挂。

### Native 容器
- Job 通道一律使用 `Allocator.Persistent`。
- 生命周期由 `InitializeJobDataChannels` / `DisposeJobDataChannels` 集中管理。
- 帧间复用时使用 `Clear`，不允许用临时重建替代正常复用。
- Job 数据与主容器数据之间的转换必须集中在 `JobDataChannel` 侧完成。

## 生命周期模型
### 实体进入仿真
统一由 `EntitySync` 监听实体显示事件后触发：
- Enemy group -> `RegisterEnemyLifecycle`
- Drop group -> `RegisterPickupLifecycle`
- Bullet / Projectile / EnemyProjectile group -> `RegisterProjectileLifecycle`

### 实体退出仿真
统一由 `EntitySync` 监听实体隐藏事件后触发：
- Enemy -> `UnregisterEnemyLifecycle`
- Drop -> `UnregisterPickupLifecycle`
- Projectile 相关 group -> `UnregisterProjectileLifecycle`

### 清场
`ClearSimulationState` 负责：
- 清空主容器
- 清空投射物回收与结算缓存
- 清空区域碰撞请求与命中缓存
- 清空 Job 通道
- 清空全部 `EntityBinding`

## 运行时执行链路
### 帧级入口
1. `GameStateBattle.OnUpdate`
2. `_enemyManager.OnUpdate(...)`
3. `SimulationWorld.Tick(...)`
4. `SimulationWorld.LateUpdate()`

### Tick 总流程
`SimulationWorld.Tick` 是战斗仿真的唯一主入口。

约束：
- 当 `UseSimulationMovement == false` 时，直接返回。
- `Tick` 只负责逻辑仿真与结算，不直接写 `Transform`。

### 每帧仿真管线
当前实现的标准顺序为：
1. Early Return
   - `DeltaTime <= 0` 时只清理碰撞临时通道和统计。
2. BuildInput
   - 将 `_enemies` / `_projectiles` 同步为 Job 输入。
   - 准备敌人输出、投射物输出、碰撞查询缓冲。
3. StateUpdate
   - 调度敌人移动 Job。
   - 调度投射物移动 Job。
4. Schedule
   - 按需调度敌人互斥分离 Job。
   - 合并敌人与投射物 Job 依赖。
5. Complete
   - 等待本帧仿真 Job 完成。
6. Collision
   - 构建碰撞查询。
   - 构建敌人碰撞桶。
   - 生成候选并统计。
7. WriteBack
   - 把输出写回主容器。
   - 在主线程结算碰撞与伤害。
   - 回收失效投射物。

### LateUpdate
`LateUpdate` 只做表现写回，不做逻辑判定：
- 敌人位置/朝向写回
- 投射物位置/朝向写回

## 线程模型与边界
### Job/Burst 允许做的事
- 读取 Job 输入缓冲
- 写入 Job 输出缓冲
- 写入 NativeHashMap / NativeList 等碰撞与分桶数据
- 执行纯数据计算

### Job/Burst 禁止做的事
- 读写 `Transform`
- 操作 GameObject / Entity 生命周期
- 调用事件系统
- 直接调用 `AIUtility.PerformCollision`
- 进行托管分配、LINQ、装箱

### 主线程必须做的事
- 应用输出到仿真主容器
- 命中结算与伤害计算
- 投射物失效回收
- 命中表现事件派发
- `LateUpdate` 表现写回

## 子系统约束
### 敌人仿真
固定接入点：
- BuildInput
- Movement
- Separation
- WriteBack

约束：
- 敌人状态必须以 `EnemySimData` 为中心流动。
- 互斥与移动结果必须先写入输出缓冲，再统一提交。
- 与目标选择相关的空间索引脏标记必须在主容器变更时维护。

### 投射物仿真
固定接入点：
- BuildInput
- Movement
- Collision Query
- Resolve
- Recycle

约束：
- 投射物生命周期状态必须由 `ProjectileSimData.Active` 和 `State` 共同表达。
- 投射物实际隐藏与移除只能在主线程回收阶段完成。

### 碰撞管线
职责：
- 构建投射物查询和区域查询
- 生成 broad-phase 候选
- 在主线程做最终业务结算

约束：
- Broad-phase 只能筛候选，不能替代最终命中判定。
- Area Query 必须保留 `SourceWasActiveAtQueryTime` 快照语义。
- 候选去重与区域命中去重只能在主线程收口。

### 目标选择空间索引
职责：
- 提供按位置查询最近敌人的能力。

约束：
- 仅在仿真启用时对外提供结果。
- 敌人主容器变更后必须标记脏。
- 索引是缓存，不是源数据；源数据仍是 `_enemies`。

### Presentation
职责：
- 将仿真层结果写回表现层。
- 消费命中表现事件。

约束：
- Presentation 只消费仿真结果，不反向修改仿真逻辑状态。
- 任何新增表现都应接在 `Presentation` 子模块，而不是接回 Job 或业务结算热路径。

## 不可破坏的不变量
### 生命周期单入口
仿真容器的增删必须只经过 `EntitySync` 和 `SimEntityState`。

### 数据单一事实来源
主容器是持续态事实来源，Job 缓冲只是帧级副本。

### 逻辑与表现分离
逻辑阶段不写 `Transform`，表现阶段不做业务结算。

### 索引一致性
任何 `swap-back` 删除都必须同步 remap，否则视为架构级错误。

### 主线程结算收口
伤害、事件派发、实体隐藏和回收必须回到主线程。

### 空间索引与碰撞桶是缓存
它们可以重建，不可被外部业务当作持久数据依赖。

## 扩展开发流程
### Step 0：判定接入位置
先判断新需求属于：
- 新仿真态字段
- 新执行阶段逻辑
- 新碰撞查询类型
- 新表现桥接

不要一开始就直接改 `Tick` 主流程。

### Step 1：扩状态
先补 `SimData`、必要的 Job 输入输出结构和转换逻辑。

### Step 2：接生命周期
如果是新实体类型，先定义 show/hide 到仿真态的映射，再进入执行阶段。

### Step 3：接执行管线
优先复用已有阶段；只有确实无法收纳时才新增阶段，并补可观测的 profiler 标记。

### Step 4：接主线程结算
需要业务判定、伤害、事件派发、实体隐藏时，一律回主线程收口。

### Step 5：接表现
视觉写回、命中反馈、临时特效都放在 `Presentation` 侧。

### Step 6：补测试
至少覆盖：
- 正常行为
- 空容器和边界条件
- 删除后的索引稳定性
- 碰撞去重或快照语义
- 与旧路径一致的关键行为

### Step 7：更新文档
修改模块边界、数据契约、不变量或执行阶段时，必须同步更新本文件。

## 回归关注点
- `ClearSimulationState` 是否把主容器、缓存和 binding 一并清干净。
- 删除路径是否保持 `swap-back + remap` 一致。
- Job Native 容器是否有泄漏或容量管理回退。
- 是否在热路径引入托管分配。
- 是否让表现逻辑重新侵入仿真逻辑。
- 是否破坏 Area Query 快照语义。
- 是否破坏碰撞候选与命中去重。

## 测试建议
至少保留并持续扩展以下类型的测试：
- Tick 行为正确性
- 主线程与 Job 管线一致性
- 最近敌查询正确性
- 投射物候选上限与玩家候选覆盖
- Area Query 快照语义
- 清场和 Battle 循环稳定性

## 维护原则
如果未来需要继续扩展为多模式仿真开关、更多 Job 管线层级或新的仿真对象类型，应优先维护以下三点：
- `Tick` 仍然只有一个主入口
- 仿真态生命周期仍然只有一个注册/反注册入口
- 主线程结算与表现写回边界不被打穿
