# VampireLike

`VampireLike` 是一个基于 **Unity 2022.3 LTS** 的 2D/3D（按当前资源配置）动作生存类项目，使用 `GameMain + GameFramework` 分层组织代码与资源。

## 开发环境

- Unity Editor: `2022.3.62f3c1`
- .NET/C#: Unity 默认编译链
- 推荐 IDE:
  - Rider
  - Visual Studio
  - VS Code

## 快速开始

1. 使用 Unity Hub 打开项目根目录。
2. 确认 Unity 版本为 `2022.3.62f3c1`（或兼容的 2022.3 LTS 版本）。
3. 进入后等待包与资源导入完成。
4. 在 Editor 中打开场景并运行：
   - `Assets/GameMain/Scenes/Menu.unity`
   - `Assets/GameMain/Scenes/Main.unity`
   - `Assets/GameMain/Scenes/Game.unity`

可选命令行启动（请替换为本机 Unity 路径）：

```powershell
Unity -projectPath .
```

## 项目结构

主要目录说明：

- `Assets/GameMain/`: 游戏业务代码、场景和内容资源。
- `Assets/GameFramework/`: 通用框架与编辑器扩展。
- `Assets/Plugins/`: 第三方插件（如 DOTween）。
- `Assets/Resources/`: 运行时通过 Resources 加载的资源。
- `Assets/StreamingAssets/`: 原样打包到客户端的数据。
- `Json/`、`数据表/`: 配置与数据表。
- `Tools/`: 本地工具脚本和处理流程。

请避免直接修改自动生成目录：

- `Library/`
- `Temp/`
- `Logs/`
- `obj/`

## 代码规范

- C# 使用 4 空格缩进。
- 大括号与声明同行（K&R 风格）。
- 单文件单主类型，文件名与类型名一致。
- 公有类型/成员使用 `PascalCase`。
- 局部变量/参数使用 `camelCase`。
- 需要在 Inspector 暴露的私有字段使用 `[SerializeField]`。

## 测试

项目已包含 `com.unity.test-framework`。

建议约定：

- 测试目录：`Assets/Tests/` 或 `Assets/<Module>/Tests/`
- 测试文件命名：`*Tests.cs`
- 使用 NUnit `[Test]` 编写用例
- 通过 Unity Test Runner 运行：`Window > General > Test Runner`

## 常用依赖（Packages）

当前项目使用的关键包包括：

- `com.unity.inputsystem`
- `com.unity.render-pipelines.universal`
- `com.unity.textmeshpro`
- `com.unity.ugui`
- `com.unity.nuget.newtonsoft-json`
- `com.unity.test-framework`

完整依赖见 `Packages/manifest.json`。

## 协作与提交流程（建议）

- 提交信息使用简短祈使句，例如：`UI: Fix shop item refresh logic`
- PR 建议包含：
  - 变更摘要
  - 测试说明
  - 关联任务/Issue
  - UI 改动截图或录屏

## 配置注意事项

- 修改依赖时，请同时关注：
  - `Packages/manifest.json`
  - `ProjectSettings/`
- 大体积二进制资源需与对应 `.meta` 一并提交。

## 许可证

当前仓库未提供许可证文件。若需开源或外发，请先补充 `LICENSE`。
