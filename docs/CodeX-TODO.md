# CodeX TODO

## GPU Instancing 改造路线

### 目标定义

- `SimulationWorld` 继续作为敌人 `position / rotation / state` 的唯一真相源。
- 新增 `InstanceRendererComponent` 作为 Presentation 侧批量渲染组件，不把批次构建和 GPU Buffer 管理塞回 `SimulationWorld`。
- 先做 `Graphics.DrawMeshInstanced` 低风险版，确认链路跑通后，再决定是否升级到 `DrawMeshInstancedIndirect`。
- P3 首阶段只处理“敌人批量渲染”，不同时扩大到投射物、掉落物和所有特效。

### 架构边界

- `SimulationWorld`
  - 只负责逻辑 Tick、生命周期同步、状态容器与碰撞结算。
  - 不负责 `Mesh / Material / MaterialPropertyBlock / ComputeBuffer`。
- `InstanceRendererComponent`
  - 只负责收集可渲染实例、按 `Mesh + Material + Shadow + Layer + RenderState` 分组、构建 batch、发起 draw call。
  - 只消费 `SimulationWorld` 输出，不反写 sim 状态。
- `TransformSync`
  - P3 第一阶段继续保留，用于 HPBar、受击特效、挂点和现有表现依赖。
  - 不要求一开始就把所有敌人 `Transform` 写回移除，否则风险过高。
- 敌人 GameObject
  - 第一阶段继续保留，用于碰撞、生命周期、事件桥和表现挂点。
  - 仅逐步移除“每敌人独立 MeshRenderer”这条渲染路径。

### 组件规划

1. 新增批量渲染组件骨架
   - 建议文件：
     - `Assets/GameMain/Scripts/CustomComponent/InstanceRendering/InstanceRendererComponent.cs`
     - `Assets/GameMain/Scripts/CustomComponent/InstanceRendering/EnemyInstanceRegistry.cs`
     - `Assets/GameMain/Scripts/CustomComponent/InstanceRendering/EnemyInstanceBatch.cs`
   - 建议职责：
     - `InstanceRendererComponent`：帧级调度、统一提交 draw call。
     - `EnemyInstanceRegistry`：维护 `EntityId -> RenderBinding`，缓存 mesh/material/archetype。
     - `EnemyInstanceBatch`：缓存每组矩阵、颜色、闪白参数和批次拆分结果。

2. 接入 `GameEntry`
   - 文件：`Assets/GameMain/Scripts/Base/GameEntry.Custom.cs`
   - 处理：
     - 增加 `GameEntry.InstanceRenderer`
     - 与 `SimulationWorld` 一样在启动时自动获取或挂载组件

### 第一阶段：低风险版 `DrawMeshInstanced`

1. 建立渲染注册表
   - 注册时机：敌人 show 时缓存渲染资源引用，hide 时释放注册。
   - 注册内容：
     - `EntityId`
     - `Mesh`
     - `Material`
     - 阴影/Layer/子类型信息
     - 受击闪白、稀有度色等实例化属性默认值
   - 约束：
     - 注册表只缓存渲染资源和 archetype，不缓存位置真相数据。

2. 从 `SimulationWorld` 读取实例数据
   - 读取来源：`_enemies` 或对外只读查询接口。
   - 每帧收集：
     - `Matrix4x4`
     - 朝向/缩放
     - `flashAmount`
     - `baseColor`
     - 其他实例化参数
   - 约束：
     - 由 sim 输出生成当前帧渲染数据，不从敌人 `Transform` 反推逻辑状态。

3. 按批次分组并提交
   - 分组键至少包含：
     - `Mesh`
     - `Material`
     - `ShadowCastingMode`
     - `ReceiveShadows`
     - `Layer`
     - 需要的渲染状态位
   - 提交方式：
     - 每批最多 `1023` 实例
     - 使用 `Graphics.DrawMeshInstanced`
     - 通过 `MaterialPropertyBlock` 下发实例颜色和闪白参数

4. 接入现有 Instanced Shader
   - 文件：`Assets/GameMain/Materials/Shaders/SimpleInstancedFlash.shader`
   - 处理：
     - 复用现有 `_BaseColor / _FlashColor / _FlashAmount` 实例化属性
     - 确认敌人材质启用 instancing
     - 不在第一阶段同时改复杂动画或多 Pass 材质

5. 替换敌人独立 Renderer 路径
   - 第一阶段做法：
     - 保留敌人实体和挂点
     - 禁用敌人身上的 `MeshRenderer/SkinnedMeshRenderer`
     - 改由 `InstanceRendererComponent` 统一绘制
   - 约束：
     - 碰撞、伤害、掉落、状态切换仍完全走现有逻辑链路

### 第二阶段：高上限版 `DrawMeshInstancedIndirect`

1. 升级批次数据结构
   - 将 `Matrix4x4[]` 和实例属性数组迁移到 `ComputeBuffer` / `GraphicsBuffer`
   - 建立按 archetype 持久复用的 buffer，避免每帧重建

2. 升级提交方式
   - 使用 `Graphics.DrawMeshInstancedIndirect`
   - 为实例数量、剔除结果和参数下发建立独立 buffer

3. 逐步加入可选优化
   - 视锥剔除
   - 距离分层
   - LOD
   - 大规模敌人下的批次复用和 buffer 容量管理

4. 进入该阶段的前提
   - `DrawMeshInstanced` 版本已验证视觉正确
   - `5k` 规模下 draw call 或主线程提交成本仍是瓶颈
   - 否则不要过早把复杂度拉到 `Indirect`

### 风险控制

- 不要把 GPU batch 容器放进 `SimulationWorld` 主状态里。
- 不要让碰撞、索引或目标选择依赖 `InstanceRendererComponent`。
- 不要一上来就移除 `TransformSync`，先保住现有 HPBar/特效/挂点行为。
- 不要在第一阶段同时处理敌人、投射物、掉落物三类 instancing。

### 验证口径

1. 功能回归
   - 敌人朝向、受击闪白、死亡隐藏、精英/普通配色与当前一致
   - `Battle -> LevelUp -> Shop -> Battle` 循环不丢渲染实例

2. Profiling 指标
   - `0.5k / 1k / 1.5k / 2k / 5k` 敌人
   - `Draw Calls`
   - `SetPass Calls`
   - `Main Thread`
   - Render Thread / GPU 时间

3. 阶段验收
   - 第一阶段验收：
     - 先证明 `DrawMeshInstanced` 跑通且视觉一致
     - Draw Calls 相比当前路径明显下降
   - 第二阶段验收：
     - `5k` 敌人规模下主线程提交和渲染耗时继续下降
     - buffer 生命周期稳定，无泄漏、无错绘、无实例残留
