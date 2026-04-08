# Repository Guidelines

## Project Structure & Module Organization
`Assets/GameMain/Scripts` holds gameplay code, organized by domain such as `Components`, `Entity`, `Procedure`, `Simulation`, `UI`, and `Editor`. Scenes live in `Assets/GameMain/Scenes`, and `Assets/Launcher.unity` is the normal local entry point. Shared framework code is under `Assets/GameFramework`; third-party code and packages live in `Assets/Plugins` and `Packages/`. Automated tests are in `Assets/Tests/Simulation/{EditMode,PlayMode}`. Design notes and change proposals are tracked in `docs/` and `openspec/`. GameFramework build and resource configs live in `Assets/GameMain/Configs`.

## Build, Test, and Development Commands
- `Unity.exe -projectPath .` opens the project in Unity `2022.3.62f3c1`.
- Open `VampireLike.sln` in Rider or Visual Studio for C# editing.
- Open `Assets/Launcher.unity` and press Play to run the main loop locally.
- `Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-test-results.xml -logFile Logs/editmode-tests.log -quit` runs EditMode tests.
- `Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode-test-results.xml -logFile Logs/playmode-tests.log -quit` runs PlayMode tests.

## Coding Style & Naming Conventions
Use C# with 4-space indentation and K&R braces. Keep one main type per file, and match file names to type names. Use `PascalCase` for public types and members, `camelCase` for locals and parameters, and `[SerializeField] private` for Inspector-facing fields. Match nearby namespaces and folder boundaries (`Simulation`, `Entity`, `UI`, etc.). Read and edit text files using `UTF-8`, and use `LF` line endings. No repo-local linter or `.editorconfig` is committed, so use Rider/VS formatting and follow surrounding code. When adding or moving Unity assets, always commit the paired `.meta` files.

## Testing Guidelines
Tests use Unity Test Framework with NUnit. Name files `*Tests.cs`. Put pure logic coverage in EditMode tests and runtime or scene behavior in PlayMode tests. There is no fixed coverage percentage, but changes to simulation, combat, procedures, or UI flow should ship with new or updated tests in `Assets/Tests`.

## Commit & Pull Request Guidelines
Recent history favors short, imperative commit subjects such as `fix test sample`, `Update NearestTargetSelector.cs`, and `Cleanup 1`. Keep commits focused and add module context when helpful, for example `Simulation: tighten collision query pruning`. PRs should include a behavior summary, linked task or issue, tests run, and screenshots or video for UI or scene changes.

## Repository Hygiene
Do not commit generated Unity output such as `Library/`, `Temp/`, `Logs/`, `obj/`, generated `.csproj`, or `.sln` files. Review changes to `Assets/GameMain/Configs/*.xml` and `Assets/StreamingAssets` carefully because they affect packaging and runtime resources.
