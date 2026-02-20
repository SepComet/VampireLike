## 测试机性能
iQOO Neo8

CPU: 第一代骁龙 8+ 八核

内存: 12 GB

系统: OriginOS 6 (Android 16)

## CPU
| 怪物数量   | 帧率                    | TickEnemies 占比     | TickEnemies GC | Movement_Update 占比 |
|--------|-----------------------|--------------------|----------------|--------------------|
| `500`  | `60.8 fps (16.43 ms)` | `37.6% (6.18 ms)`  | `27.4 KB`      | `0.0% (0 ms)`      |
| `1000` | `40.2 fps (24.85 ms)` | `51.5% (12.8 ms)`  | `54.5 KB`      | `0.0% (0 ms)`      |
| `1500` | `28.0 fps (35.62 ms)` | `56.4% (20.11 ms)` | `82.1 KB`      | `0.0% (0 ms)`      |
| `2000` | `18.8 fps (53.04 ms)` | `55.8% (29.62 ms)` | `107.6 KB`     | `0.0% (0 ms)`      |

tip:
1. 60 fps 为 Android 端帧率上限，具体可参考 CPU ms 耗时
2. 注意 Profiler 里会产生性能损耗的配置（Call Stacks）

### Memory
| 怪物数量 | GC Used Memory | GC Allocated In Frame |
|------|----------------|-----------------------|
| 500  | 7.8 MB         | 29.5 KB               |
| 1000 | 8.8 MB         | 56.6 KB               |
| 1500 | 10.0 MB        | 84.2 KB               |
| 2000 | 11.8 MB        | 109.7 KB              |


### Render
| 维度                   | 500 enemies | 1000 enemies | 1500 enemies | 2000 enemies |
|----------------------|-------------|--------------|--------------|--------------|
| SetPass Calls        | 41          | 42           | 43           | 44           |
| Draw Calls           | 46          | 46           | 48           | 49           |
| Batches              | 46          | 46           | 48           | 49           |
| **Static Batching:** |             |              |              |              |
| Batched Draw Call    | 0           | 0            | 0            | 0            |
| Batched              | 0           | 0            | 0            | 0            |
| **Instancing:**      |             |              |              |              |
| Batched Draw Call    | 420         | 505          | 584          | 962          |
| Batched              | 5           | 5            | 7            | 8            |

