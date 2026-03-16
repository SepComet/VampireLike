# UI 五层架构设计规范（RawData / Controller / View / Context / UseCase）

## 1. 适用范围

- 适用目录：`Assets/GameMain/Scripts/UI/*`
- 重点对象：采用五层拆分的 UI 模块（`MenuScene`、`GameScene`、`General` 下的分层 UI）
- 本文不展开 Unity GameFramework 底层实现细节，仅约束项目内 UI 代码组织与协作方式

## 2. 架构总览

UI 模块采用“输入数据 -> 业务编排 -> 展示数据 -> 渲染表现”的分层方式，核心链路如下：

1. 外部流程（Procedure/GameState）创建并绑定 UseCase
2. 通过 `GameEntry.UIRouter` 打开指定 UI
3. Controller 从 UseCase 取 RawData，并转换为 Context
4. View 使用 Context 渲染
5. View 通过事件回传交互，Controller 处理后驱动 UseCase 更新，再刷新 View

简化关系图：

```text
Procedure/GameState
   -> UIRouter
      -> Controller <-> UseCase
      -> Context -> View
View --(CustomEvent)--> Controller
```

## 3. 五层职责定义

### 3.1 RawData 层

职责：承载“业务原始数据”，作为 UseCase 到 Controller 的传输模型。

约束：

- 命名：`XXXFormRawData`
- 只描述数据，不包含 UI 渲染行为
- 可保留领域对象或数据表对象（例如 `DRLevelUpReward`、`WeaponBase`）
- 不依赖具体 View 组件

参考：

- `Assets/GameMain/Scripts/UI/GameScene/RawData/ShopFormRawData.cs`
- `Assets/GameMain/Scripts/UI/GameScene/RawData/LevelUpFormRawData.cs`
- `Assets/GameMain/Scripts/UI/MenuScene/RawData/SelectRoleFormRawData.cs`

### 3.2 UseCase 层

职责：封装 UI 对应业务用例，负责业务规则、状态推进、数据生成。

约束：

- 实现 `IUIUseCase`
- 命名：`XXXFormUseCase`
- 对外提供 `CreateInitialModel / TryRefresh / Select / Confirm` 等语义化方法
- 返回 RawData（或结果对象），不直接操作具体 View

参考：

- `Assets/GameMain/Scripts/UI/GameScene/UseCase/ShopFormUseCase.cs`
- `Assets/GameMain/Scripts/UI/GameScene/UseCase/LevelUpFormUseCase.cs`
- `Assets/GameMain/Scripts/UI/MenuScene/UseCase/SelectRoleFormUseCase.cs`

### 3.3 Controller 层

职责：UI 编排层，连接 UseCase 与 View，管理 UI 生命周期、事件订阅、数据转换。

约束：

- 继承 `UIFormControllerCommonBase<TContext, TForm>`
- 命名：`XXXFormController`
- 通过 `BindUseCase(IUIUseCase)` 注入用例并做类型校验
- `OpenUI(object userData = null)` 支持：`Context`、`RawData`、`null`
- 负责 RawData -> Context 的转换（常见 `BuildContext`）
- 在 `SubscribeCustomEvents / UnsubscribeCustomEvents` 成对管理事件
- 可做局部刷新（避免整窗重建）

参考：

- `Assets/GameMain/Scripts/UI/GameScene/Controller/ShopFormController.cs`
- `Assets/GameMain/Scripts/UI/GameScene/Controller/LevelUpFormController.cs`
- `Assets/GameMain/Scripts/UI/MenuScene/Controller/SelectRoleFormController.cs`

### 3.4 Context 层

职责：承载“可直接驱动 UI 展示”的上下文数据。

约束：

- 继承 `UIContext`
- 命名：`XXXFormContext` 或 `XXXItemContext`
- 字段以展示友好为目标（标题、描述、图标、稀有度、列表等）
- 允许组合子 Context（例如列表区 + 条目）

参考：

- `Assets/GameMain/Scripts/UI/GameScene/Context/ShopFormContext.cs`
- `Assets/GameMain/Scripts/UI/GameScene/Context/DisplayListAreaContext.cs`
- `Assets/GameMain/Scripts/UI/MenuScene/Context/SelectRoleFormContext.cs`

### 3.5 View 层

职责：纯表现层，负责控件绑定、显示刷新、交互事件抛出。

约束：

- Form 类继承 `UGuiForm`，子组件通常继承 `MonoBehaviour`
- 命名：`XXXForm` / `XXXItem` / `XXXArea`
- 提供 `RefreshUI(Context)`、`OnInit(Context)`、`OnReset()` 等渲染入口
- 用户交互通过 `GameEntry.Event.Fire(...)` 通知 Controller
- 不承载业务规则（计算、流程推进、数据筛选应在 UseCase）

参考：

- `Assets/GameMain/Scripts/UI/GameScene/View/ShopForm.cs`
- `Assets/GameMain/Scripts/UI/GameScene/View/DisplayListArea.cs`
- `Assets/GameMain/Scripts/UI/MenuScene/View/SelectRoleForm.cs`

## 4. 标准交互流程

### 4.1 初始化与绑定

1. Procedure/GameState 创建 UseCase
2. 调用 `GameEntry.UIRouter.BindUIUseCase(UIFormType.X, useCase)`

示例：

- `Assets/GameMain/Scripts/Procedure/Game/GameStateShop.cs`
- `Assets/GameMain/Scripts/Procedure/Game/GameStateLevelUp.cs`
- `Assets/GameMain/Scripts/Procedure/ProcedureStartMenu.cs`

### 4.2 打开 UI

1. 调用 `GameEntry.UIRouter.OpenUI(UIFormType.X)`
2. Controller 从 UseCase 取 RawData（或接收外部 RawData/Context）
3. Controller 构建 Context 后打开/刷新 Form
4. View 在 `OnOpen` 中校验 Context 类型并执行 `RefreshUI`

### 4.3 用户交互到刷新

1. View 触发事件（如购买、刷新、选择）
2. Controller 监听事件并调用 UseCase
3. UseCase 返回新数据或操作结果
4. Controller 更新 Context 并刷新全部或局部 UI

### 4.4 关闭 UI

1. 调用 `GameEntry.UIRouter.CloseUI(UIFormType.X)`
2. Controller 解除事件订阅并关闭窗体
3. View `OnClose` 清理本地状态

## 5. 目录与命名规范

- 目录：`UI/<SceneDomain>/RawData|UseCase|Controller|Context|View`
- 五层同名前缀保持一致：`ShopForm*`、`LevelUpForm*`、`SelectRoleForm*`
- 子组件上下文命名：`RoleItemContext`、`DisplayItemContext`、`LevelUpRewardItemContext`
- 新增 UI Form 时优先建立完整五层；仅纯静态展示可降级为 View-only

## 6. 依赖方向约束

允许依赖：

- `UseCase -> RawData / 领域对象`
- `Controller -> UseCase + RawData + Context + View + Event`
- `View -> Context + Event`

禁止依赖：

- `View -> UseCase`
- `View -> 领域状态修改`
- `Context/RawData -> View`

## 7. 新增一个五层 UI 的落地步骤

1. 在目标场景目录创建 `RawData / UseCase / Context / Controller / View` 对应类型
2. 在 UseCase 中实现模型创建与交互方法
3. 在 Controller 中实现 `BindUseCase`、`OpenUI`、`BuildContext`、事件订阅
4. 在 View 中实现 `RefreshUI` 和交互事件抛出
5. 在对应 Procedure/GameState 里完成 UseCase 绑定与 Open/Close 调用
6. 自测三条主链路：首次打开、交互刷新、关闭重开

## 8. 项目当前实践说明

- `ShopForm`、`LevelUpForm`、`SelectRoleForm` 是当前五层模式的主要样板
- `DialogForm` 也有 Controller/Context/RawData，但 UseCase 为可选
- `HudForm`、`StartMenuForm` 当前为轻用例场景，可不强制 UseCase
- `SettingForm`、`AboutForm` 属于历史直连型 UI，不属于五层完整样板

---

如后续需要统一重构，建议优先把历史直连型 UI（如 `SettingForm`）迁移到五层模板，以降低 UI 逻辑耦合度。
