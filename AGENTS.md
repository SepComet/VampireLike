# Repository Guidelines

## Project Structure & Module Organization
This is a Unity project (Unity 2022.3.62f3c1). Core game code and assets live under `Assets/`:
- `Assets/GameMain/` for game-specific scripts, scenes, and content.
- `Assets/GameFramework/` for shared framework code and editor tooling.
- `Assets/Plugins/` for third-party integrations (e.g., DOTween).
- `Assets/Resources/` and `Assets/StreamingAssets/` for runtime-loaded data.
- `Json/` and `数据表/` for data files and tables used by the game.
- `Tools/` for local utilities or pipelines.

Avoid editing generated folders: `Library/`, `Temp/`, `Logs/`, `obj/`, `ProjectSettings/` (unless configuration changes are intentional).

## Build, Test, and Development Commands
Primary workflow is through the Unity Editor:
- Open the project in Unity Hub, then press Play to run locally.
- Optional CLI launch: `Unity -projectPath .` (use your local Unity editor path).
- The solution file `VampireLike.sln` supports IDE navigation (Rider/VS/VS Code).

## Coding Style & Naming Conventions
- C# style: 4-space indentation, braces on the same line, one public type per file.
- Match filename to main type (e.g., `PlayerController.cs` defines `PlayerController`).
- Use `PascalCase` for public types/members, `camelCase` for locals/parameters.
- Prefer `SerializeField` for private Unity fields that must be editable in the Inspector.

## Testing Guidelines
`com.unity.test-framework` is included, but there is no dedicated `Assets/**/Tests` directory yet. When adding tests:
- Place under `Assets/Tests/` or `Assets/<Module>/Tests/`.
- Name files `*Tests.cs` and use NUnit-style `[Test]` methods.
- Run via Unity Test Runner (Window > General > Test Runner).

## Commit & Pull Request Guidelines
This repository does not include Git history, so no commit convention is enforced. Recommended default:
- Short, imperative subject (e.g., `Add enemy spawn tuning`).
- Include scope tags when helpful (e.g., `UI: Fix pause menu layout`).

For pull requests, include:
- A concise summary and testing notes.
- Linked issues or tasks when applicable.
- Screenshots or short clips for UI/visual changes.

## Configuration Tips
- Keep `Packages/manifest.json` and `ProjectSettings/` in sync when changing dependencies or project settings.
- Large binary assets should stay in `Assets/` with `.meta` files committed alongside.
