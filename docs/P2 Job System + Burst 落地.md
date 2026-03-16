# P2 Job System + Burst 落地（结项与验收）

## 1. 文档目的
本文件用于对齐 `docs/TodoList.md` 的 P2 Checkpoint 9，作为 P2 结项与 P3 输入基线。

目标：
- 固化压测口径（1k/2k/3k）
- 给出回归验证结论
- 给出开关/回滚策略
- 给出最终验收判定（通过/不通过）

## 2. 验收标准（对齐 TodoList）
来源：`docs/TodoList.md` 第 171~179 行。

- 在 `3k` 敌人规模下，CPU Main Thread 明显下降（目标 `>= 30%`）。
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
| `Battle -> LevelUp -> Shop -> Battle` 循环 | 状态切换稳定、无卡死   | 待补 | 待补 |
| 掉落拾取链路                                   | 掉落生成/吸附/回收正常 | 待补 | 待补 |

建议附证据：
- `Logs/playmode-tests.log`
- 关键流程录屏/截图
- 回归脚本或人工步骤说明

## 6. 压测口径与数据

### 6.1 标准口径（必须覆盖）
- 敌人规模：`1k / 2k / 3k`
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

## 7. 验收判定

| 验收项                | 标准       | 当前状态     | 判定  |
|--------------------|----------|----------|-----|
| Main Thread 降幅（2k） | `>= 30%` | 缺失 3k 数据 | 不通过 |
| 持续帧 GC Alloc       | 接近 0     | 缺失 GC 数据 | 不通过 |

**当前结论：P2 Checkpoint 9 暂不通过。**

可确认部分：
- P2 在 `500~2000` 规模的热路径 CPU 优化已显著成立。
- 但未满足 TodoList 的完整验收口径（3k + GC + 回归证据）。

## 8. 下一步补齐动作（建议）
1. 按同一场景补采 `3k` 数据（P1.5 与 P2 各一次，至少 60s 稳态窗口）。
2. 记录 `Main Thread / Job Workers / GC Alloc` 三项，写入 6.3 对应表。
3. 完成 5.0 的三个回归用例并填入证据。
4. 补齐后将第 7 节判定更新为“通过”，再在 `TodoList.md` 把 P2 Checkpoint 9 勾选。

## 9. 测试命令（复用）
- PlayMode:
  - `Unity -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode-test-results.xml -logFile Logs/playmode-tests.log`
- EditMode:
  - `Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-test-results.xml -logFile Logs/editmode-tests.log`
