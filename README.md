# VampireLike

## 这是一个学习项目

VampireLike 是一个基于 **Unity 2022.3 LTS** 的动作生存类项目，旨在实践以下技术要点：

- **GameFramework 框架的组件化设计** — 理解如何通过 `GameEntry.<Component>` 解耦各模块（事件 Entity/UI/Procedure/Resource 等）
- **UI 架构的分层设计** — 实践 Controller / UseCase / View 三层分离，理解"context"作为数据桥梁的作用
- **数据驱动设计** — 通过 DataTable（DRBullet、DREnemy、DRWeapon 等）实现配置与代码的分离
- **数据导向（Data-Oriented）模拟系统** — `SimulationWorld` 展示了如何将 ECS 思路应用于 Unity：SimData 结构体 → Job 并行处理 → 结果提交 → 表现层同步
- **Entity-Component 模式** — 实体（Player/Enemy/Weapon）与数据（EntityData）分离，配合对象池实现高效Spawn/Despawn
- **技能系统设计** — StatModifier（加减/乘算）的组合、AttackComponent 的技能释放流程
- **Target Selector 模式** — 武器通过 `ITargetSelector` 接口支持多种目标选择策略（最近/最低血量/最高血量）

## 项目结构

```
Assets/
├── GameFramework/          # 通用框架（来自 GameFramework.cn）
│   └── Scripts/Runtime/   # Event/Entity/UI/Procedure/Resource 等组件
├── GameMain/               # 游戏业务代码
│   ├── Scripts/
│   │   ├── Base/           # GameEntry 入口
│   │   ├── Components/      # 游戏组件（Movement/Attack/Health/Stat...）
│   │   ├── CustomEvent/     # 自定义事件
│   │   ├── DataTable/       # DR* 数据行定义
│   │   ├── Definition/      # 枚举、结构体、常量
│   │   ├── Entity/          # 实体逻辑与数据
│   │   ├── Procedure/       # 流程状态机
│   │   ├── Simulation/       # 数据导向模拟层
│   │   ├── UI/              # UI（Controller/UseCase/View 分离）
│   │   └── CustomComponent/  # 自定义功能组件
│   ├── Scenes/             # Menu/Main/Game 场景
│   └── DataTables/          # 数据表资源
├── Launcher.unity          # 启动场景
├── Plugins/                # 第三方插件（DOTween）
└── Tests/                  # 单元测试
```

## 技术栈

- Unity 2022.3.62f3c1 LTS
- Input System（`com.unity.inputsystem`）
- Universal Render Pipeline（`com.unity.render-pipelines.universal`）
- TextMeshPro（`com.unity.textmeshpro`）
- DOTween（动画）
- Unity Test Framework（NUnit）
- Newtonsoft.Json（配置序列化）

## 运行方式

1. Unity Hub 打开项目根目录
2. 确认 Unity 版本为 `2022.3.62f3c1`
3. 打开 `Assets/Launcher.unity` 并 Play

CLI 方式：
```powershell
Unity -projectPath .
```

## 代码规范

- C# 4 空格缩进，K&R 大括号风格
- 单文件单主类型，文件名与类型名一致
- `PascalCase` 公有成员，`camelCase` 局部变量
- Inspector 暴露用 `[SerializeField] private`
- 保持 `.meta` 文件与资源同步

## 测试

```powershell
# EditMode 测试
Unity -batchmode -projectPath . -runTests -testPlatform editmode -quit -logFile Logs/editmode-tests.log
```

测试目录：`Assets/Tests/`、`Assets/Tests/Simulation/EditMode/`、`Assets/Tests/Simulation/PlayMode/`

## 协作

- 提交信息使用简短祈使句，建议包含模块上下文（如 `UI: Fix shop item refresh logic`）
- PR 包含变更摘要、测试说明、关联任务/Issue
- UI/视觉改动附上截图或录屏
