# Repository Guidelines

## Project Structure & Module Organization
This repository is a Unity `2022.3 LTS` project. Core gameplay code lives in `Assets/GameMain/Scripts` (for example UI, entity, procedure, events, and data-table related logic). Shared runtime/framework code is in `Assets/GameFramework`. Third-party plugins are under `Assets/Plugins` (such as DOTween). Main scenes are in `Assets/GameMain/Scenes` (`Menu.unity`, `Main.unity`, `Game.unity`), and the local launcher flow starts from `Assets/Launcher.unity`.

Data content and tooling are split across `Assets/GameMain/DataTables`, the localized data-table folder at project root, and `Tools/`.

Do not commit generated folders: `Library/`, `Temp/`, `Logs/`, `obj/`.

## Build, Test, and Development Commands
- `Unity -projectPath .`  
  Opens the project in Unity Editor.
- `VampireLike.sln`  
  Open the C# solution in Rider/Visual Studio for code changes.
- Unity Editor: open `Assets/Launcher.unity` and press Play  
  Runs the local game flow end-to-end.
- Unity Editor: `Window > General > Test Runner`  
  Run EditMode/PlayMode tests interactively.
- `Unity -batchmode -projectPath . -runTests -testPlatform editmode -quit -logFile Logs/editmode-tests.log`  
  Executes EditMode tests via CLI and writes logs for CI/local verification.

## Coding Style & Naming Conventions
Use C# with 4-space indentation and K&R braces. Keep one main type per file, and match file name to type name. Use `PascalCase` for public types/members and `camelCase` for locals/parameters. For Inspector exposure, prefer `[SerializeField] private` fields instead of `public` fields.

Always keep `.meta` files when adding or moving Unity assets to preserve GUID references.

## Testing Guidelines
`com.unity.test-framework` is included (NUnit style). Place tests in `Assets/Tests/` or module-local `Tests/` folders. Name test files `*Tests.cs`. Use EditMode tests for pure logic and PlayMode tests for runtime/scene behavior. Update or add tests for gameplay logic and UI controller/use-case changes.

## Commit & Pull Request Guidelines
Recent history favors concise, descriptive commit messages (often Chinese), e.g. `Feature: add launcher scene and update project settings`. Keep commits focused and include module context (`UI`, `Procedure`, `Entity`) when useful.

PRs should include: change summary, affected scenes/modules, test evidence (Test Runner or CLI logs), linked issue/task, and screenshots or short video for UI/visual updates.

## Encoding
Use UTF8 with BOM
