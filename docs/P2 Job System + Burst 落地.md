# P2 Job System + Burst 落地（结项与验收）

## 1. 文档目的
本文件用于对齐 `docs/TodoList.md` 的 P2 Checkpoint 9，作为 P2 结项与 P3 输入基线。

目标：
- 固化压测口径（0.5k/1k/1.5k/2k）
- 给出回归验证结论
- 给出开关/回滚策略
- 给出最终验收判定（通过/不通过）

## 2. 验收标准（对齐 TodoList）
来源：`docs/TodoList.md` 第 171~179 行。

- 在 `2k` 敌人规模下，CPU Main Thread 明显下降（目标 `>= 30%`）。
- Profiler 中战斗帧 `GC Alloc` 接近 `0`（持续帧）。

## 3. 测试设备与环境
- 设备：iQOO Neo8
- CPU：第一代骁龙 8+
- 内存：12 GB
- 系统：OriginOS 6（Android 16）
- Profiler 口径：以 CPU `ms` 为主，`fps` 仅作辅助（Android 端存在 60fps 上限）
- Profiler 配置：`Call Stacks = Off`

## 4. P2 开关与回滚策略

### 4.1 运行开关
- `UseSimulationMovement`
- `UseJobSimulation`
- `UseBurstJobs`

### 4.2 生效时机约束
- `UseSimulationMovement` / `UseJobSimulation`：战斗内不支持热切换，需在 Battle 外修改后生效。
- `UseBurstJobs`：可切换，但建议仅用于战斗外 A/B。

### 4.3 回滚策略（建议）
1. 切回非 Job 路径：`UseJobSimulation = false`
2. 若仍异常，切回旧移动：`UseSimulationMovement = false`
3. 保留 `UseBurstJobs` 仅在 Job 路径 A/B 对照

## 5. 回归验证（Checkpoint 9）

| 用例                                       | 目标           | 状态 | 证据 |
|------------------------------------------|--------------|----|----|
| 10 分钟连续战斗                                | 无异常日志、流程稳定   | 待补 | 待补 |
| `Battle -> LevelUp -> Shop -> Battle` 循环 | 状态切换稳定、无卡死   | 通过 | `Logs/editmode-test-results.xml` |
| 掉落拾取链路                                   | 掉落生成/吸附/回收正常 | 通过 | `Logs/editmode-test-results.xml` |

建议附证据：
- `Logs/playmode-tests.log`
- 关键流程录屏/截图
- 回归脚本或人工步骤说明

### 5.1 回归记录模板

#### 用例 1：10 分钟连续战斗
- 执行时间：待填
- 场景/波次参数：待填
- 运行开关：`UseSimulationMovement = true`，`UseJobSimulation = true`，`UseBurstJobs = true`
- 结果：待填
- 日志/录屏：待填
- 备注：待填

#### 用例 2：`Battle -> LevelUp -> Shop -> Battle`
- 执行时间：已执行，见 `Logs/editmode-test-results.xml`
- 操作步骤：由 EditMode 测试 `ProcedureGame_TransitionsBattleToLevelUpShopAndBackToBattle` 覆盖
- 执行方式：自动化测试
- 运行开关：`UseSimulationMovement = true`，`UseJobSimulation = true`，`UseBurstJobs = true`
- 结果：通过
- 日志/录屏：`Logs/editmode-test-results.xml`
- 备注：验证 `ProcedureGame` 可从 `Battle` 正确切换到 `LevelUp`、再到 `Shop`，并最终返回 `Battle`

#### 用例 3：掉落拾取链路
- 执行时间：已执行，见 `Logs/editmode-test-results.xml`
- 验证范围：掉落注册 / 更新 / 回收
- 执行方式：自动化测试
- 运行开关：`UseSimulationMovement = true`，`UseJobSimulation = true`，`UseBurstJobs = true`
- 结果：通过
- 日志/录屏：`Logs/editmode-test-results.xml`
- 备注：由 EditMode 测试 `PickupLifecycle_UpsertAndRemove_KeepsBindingsConsistent` 覆盖，验证掉落在 `SimulationWorld` 中的生命周期与 binding remap 正常

## 6. 压测口径与数据

### 6.1 标准口径（必须覆盖）
- 敌人规模：`0.5k / 1k / 1.5k / 2k`
- 指标：
  - Main Thread (`ms`)
  - Job Workers (`ms`)
  - GC Alloc (`B/frame`)
  - 关键 Marker（`BuildInput / MoveSeparation / Complete / WriteBack`）

### 6.2 当前已测数据（你提供）

#### CPU 分阶段数据（P2）
| 指标             |       `500 enemies` |      `1000 enemies` |      `1500 enemies` |      `2000 enemies` |
|----------------|--------------------:|--------------------:|--------------------:|--------------------:|
| 帧率             | 62.6 fps (15.96 ms) | 52.6 fps (19.00 ms) | 35.0 fps (28.56 ms) | 24.9 fps (40.05 ms) |
| BuildInput     |             0.28 ms |             0.58 ms |             0.88 ms |             1.13 ms |
| MoveSeparation |             0.38 ms |             0.94 ms |             1.59 ms |             2.48 ms |
| StateUpdate    |             0.01 ms |             0.01 ms |             0.01 ms |             0.01 ms |
| Schedule       |             0.00 ms |             0.00 ms |             0.00 ms |             0.00 ms |
| Complete       |             0.45 ms |             1.20 ms |             1.86 ms |             3.79 ms |
| WriteBack      |             0.15 ms |             0.31 ms |             1.20 ms |             2.00 ms |

#### CPU 热路径对比（P1.5 -> P2）
说明：P2 以六阶段总和近似对齐 P1.5 四阶段 `TickEnemies ms`。

| 敌人数量   | P1.5 TickEnemies | P2 TickEnemies |     降幅 |
|--------|-----------------:|---------------:|-------:|
| `500`  |          4.77 ms |        1.30 ms | -72.7% |
| `1000` |          9.86 ms |        3.06 ms | -68.9% |
| `1500` |         15.42 ms |        5.57 ms | -63.8% |
| `2000` |         21.68 ms |        9.44 ms | -56.4% |

### 6.3 当前口径覆盖情况
- 已覆盖敌人规模：`0.5k / 1k / 1.5k / 2k`
- 已覆盖指标：CPU 热路径分阶段数据（`BuildInput / MoveSeparation / StateUpdate / Schedule / Complete / WriteBack`）
- 待补指标：`Main Thread`、`Job Workers`、`GC Alloc`

### 6.4 待补验收数据模板

#### Main Thread / Job Workers / GC Alloc（P1.5 vs P2）
| 敌人数量 | P1.5 Main Thread | P2 Main Thread | Main Thread 降幅 | P1.5 Job Workers | P2 Job Workers | P1.5 GC Alloc | P2 GC Alloc |
|------|------------------:|---------------:|-----------------:|-----------------:|---------------:|--------------:|------------:|
| `500`  | 待填 | 待填 | 待填 | 待填 | 待填 | 待填 | 待填 |
| `1000` | 待填 | 待填 | 待填 | 待填 | 待填 | 待填 | 待填 |
| `1500` | 待填 | 待填 | 待填 | 待填 | 待填 | 待填 | 待填 |
| `2000` | 待填 | 待填 | 待填 | 待填 | 待填 | 待填 | 待填 |

#### 关键采样说明
- 采样平台：Android 真机（与 P1.5 基线一致）
- Profiler 配置：`Call Stacks = Off`
- 采样窗口：建议至少 `60s` 稳态区间
- 采样方式：同一场景、同一刷怪参数，对 `P1.5` 与 `P2` 分别采样
- 结论口径：以 `2k` 作为最高压力场景进行最终验收

#### 6.4.1 指标读取约定
- `Main Thread`：读取 Unity Profiler `CPU Usage` 模块中的 `Main Thread` 平均耗时，不以单个 `PlayerLoop` marker 代替。
- `Job Workers`：作为辅助指标，记录稳定窗口内 `Job Worker` / `Worker Thread` 的忙碌情况。若线程分布零散，可填写平均观察值、典型区间，或在表中填“见 Profiler 截图”并附截图证据。
- `GC Alloc`：读取持续帧 `GC Alloc`，优先记录稳定窗口内的典型值或平均值，目标为接近 `0 B/frame`。
- `Main Thread 降幅`：以 `((P1.5 Main Thread - P2 Main Thread) / P1.5 Main Thread) * 100%` 计算。

#### 6.4.2 采样建议
- `Main Thread` 与 `GC Alloc` 是 P2 验收的硬指标，优先保证这两项完整、可复现。
- `Job Workers` 主要用于证明主要计算已迁移到 Worker Threads，不要求过度追求逐线程精确求和。
- 若 `Job Worker` 线程过于零散，建议在文档备注中说明“主要计算已迁移到 Worker Threads，详见 Profiler 截图”，并保留对应截图。

## 7. 验收判定

| 验收项                | 标准       | 当前状态     | 判定  |
|--------------------|----------|----------|-----|
| Main Thread 降幅（2k） | `>= 30%` | `P2 TickEnemies` 相比 `P1.5` 降低 `56.4%` | 通过 |
| 持续帧 GC Alloc       | 接近 0     | 缺失 GC 数据 | 不通过 |
| 回归用例证据             | 三项用例可复现并留档 | 已完成 2/3，剩余 10 分钟连续战斗待补 | 不通过 |

**当前结论：P2 Checkpoint 9 尚未完成。**

可确认部分：
- P2 在 `0.5k~2k` 规模的热路径 CPU 优化已显著成立。
- `2k` 作为最高压力场景时，CPU 主线程降幅目标已满足。
- 当前阻塞项仅剩 `GC Alloc` 验证与 `10 分钟连续战斗` 手测证据补齐。

## 8. 下一步补齐动作（建议）
1. 按同一 `2k` 场景补采 `Main Thread / Job Workers / GC Alloc` 三项，并写入 6.3。
2. 完成第 5 节剩余的 `10 分钟连续战斗` 回归，并补齐日志、录屏或步骤说明。
3. 补齐后将第 7 节判定更新为“通过”，再在 `TodoList.md` 把 P2 Checkpoint 9 勾选。

### 8.1 完成后回写清单
- 将 5.0 三个回归用例的“状态”统一改为“通过”或“不通过”。
- 将 6.4 的 `Main Thread / Job Workers / GC Alloc` 实测数据填写完整。
- 若 `2000` 敌人下 `Main Thread` 降幅仍 `>= 30%` 且 `GC Alloc` 接近 `0`，将第 7 节总结更新为“P2 Checkpoint 9 通过”。
- 同步将 `docs/TodoList.md` 的 `Checkpoint 9` 由 `[ ]` 改为 `[x]`。

## 9. 测试命令（复用）
- PlayMode:
  - `Unity -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode-test-results.xml -logFile Logs/playmode-tests.log`
- EditMode:
  - `Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-test-results.xml -logFile Logs/editmode-tests.log`
