## Context

`SimulationWorld` 的运行时收敛已经完成，但代码和文档层面仍留有三类误导性遗留：`SimulationWorld.UseSimulationMovement` 这类恒真属性、`EnemyBase.IsSimulationMovementEnabled()` 这类恒真帮助方法，以及 `EnemySeparationSolverProvider` / `IEnemySeparationSolver` 与两个 legacy solver 实现。这些残留类型不再参与真实运行时调度，却继续把当前架构表述成“单路径之上还保留一套可切换兼容层”。

本次变更是一次尾部收口，不重新设计仿真数据流，也不扩展新的 solver 能力；目标是让代码表面与已经落地的运行时事实保持一致。

## Goals / Non-Goals

**Goals:**
- 删除仍对外暴露旧路径语义的兼容属性和帮助方法。
- 删除不再被运行时使用的 enemy separation provider/interface/legacy solver 类型。
- 让测试和文档表达与当前单一路径实现一致。
- 把影响收敛在 `SimulationWorld`、enemy runtime、legacy solver 文件和对应回归覆盖内。

**Non-Goals:**
- 不在本次 change 中处理 Unity 场景序列化残留字段。
- 不重做 `SimulationWorld` 的敌人分离算法或数据结构。
- 不引入新的运行时调试面板、配置项或回滚开关。

## Decisions

### Remove compatibility members instead of renaming them
直接删除 `UseSimulationMovement` 和 `IsSimulationMovementEnabled()`，而不是把它们改名为新的恒真语义成员。原因是这些成员的唯一历史价值就是表达“可选择是否启用 SimulationWorld”，继续保留只会延长错误心智模型。替代方案是保留只读属性并在注释里声明恒真，但这仍会让调用方继续围绕“是否启用”写分支，因此不采用。

### Delete legacy solver types rather than keep them as dead abstractions
`EnemySeparationSolverProvider` 与 `IEnemySeparationSolver` 已不再承载运行时能力，继续保留会产生“还有第二套 enemy separation 入口”的假象。本次直接删除 provider、interface 以及两个实现类，而不是把 provider 改成内部空壳。替代方案是保留文件供历史参考，但仓库历史已经足够承担这个角色，不需要源码继续占位。

### Tighten regression coverage around absence of legacy entry points
回归重点不是验证某个字段恒真，而是验证调用面已经不再依赖这些兼容入口。因此测试和文档只覆盖单路径可观察行为，并显式移除对旧壳层 API 的引用。替代方案是增加“成员不存在”的反射测试，但那类测试脆弱且价值低，不采用。

## Risks / Trade-offs

- [外部代码仍引用这些壳层成员] → 在实现前用全文检索清理调用点，并通过编译验证所有受影响程序集。
- [删除 legacy solver 文件后，文档或测试仍残留旧名称] → 同步更新 `docs/` 和 `Assets/Tests/Simulation/` 中的直接引用。
- [未来有人希望恢复独立 enemy separation 实验入口] → 若确实需要，应以新的 `SimulationWorld` 内部实验点重新设计，而不是恢复旧 provider 抽象。

## Migration Plan

1. 删除 `UseSimulationMovement` 与 `IsSimulationMovementEnabled()` 及其剩余引用。
2. 删除 `EnemySeparationSolverProvider`、`IEnemySeparationSolver` 与 legacy solver 实现文件。
3. 更新受影响测试与文档，使其不再依赖这些符号。
4. 通过编译与相关仿真测试验证仓库仍在单路径语义下工作。

无需运行时迁移或数据迁移；这是源码级收口。回滚方式仅为恢复该 change 的提交，不提供运行时开关回退。

## Open Questions

- None.
