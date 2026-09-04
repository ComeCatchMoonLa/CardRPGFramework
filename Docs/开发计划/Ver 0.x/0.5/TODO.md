# 0.5 TODO：遗物三钩子

> **草稿。** 从原"0.3 消耗堆、能力牌、遗物"的 TODO 拆出遗物部分。进入 0.5 前对照 [`技术设计.md`](技术设计.md) 复核后再去掉这个草稿头。预计 3～4 个有效开发日。三件遗物必须落在三个不同位置（开战钩子 / `ApplyBuffAction` 内 / `WeakDamageRule` 内），写之前对照技术设计第 4.1～4.4 节，不要图省事把三件都做成"开战给一个 Buff"。

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。开工前把技术设计里标 **[待确认]** 的几处（施加钩子形状、`BuffIds` 常量、遗物存放位置、显示名来源）定下来。

## 0. 开始前检查（约 0.5h）

- [ ] 0.4 已提交推送，工作区干净，Console 无错误，EditMode 全绿。
- [ ] [待确认] 各项已有结论并写回技术设计。

**阶段门槛：** 有干净的回归基线。

## 1. Core：遗物基础设施（约 2～3h）

- [ ] 新增 `Core.Relics`：`RelicState`、`RelicIds`、`IBattleStartRelic`、`IApplyBuffModifier`、`RelicFactory`。
- [ ] `CombatantState` 增加 `Relics` / `AddRelic`（重复 Id 抛异常）/ `HasRelic`。
- [ ] `BattleSession` 构造函数增加可选 `relics` 参数；`StartBattle` 增加 `TriggerBattleStartRelics(Player)`。
- [ ] （若采纳）新增 `BuffIds` 常量并替换现有字面量。
- [ ] 编写 `CombatantState` 遗物容器与 `RelicFactory` 的测试。

**阶段门槛：** `Core.Relics` 不引用 `UnityEngine`；0.1～0.4 测试不改仍通过。

## 2. Core：三件遗物（约 3h）

- [ ] 金刚杵：`VajraRelic` 实现 `IBattleStartRelic`；测试开战后力量 1、攻击卡 6 → 7。
- [ ] 蛇颅骨：`ApplyBuffAction.Execute` 先遍历施加方遗物；`SneckoSkullRelic` 只对 `poison` 加 1；测试剧毒 3 → 4、力量不受影响、施加方无遗物不变。
- [ ] 纸鹤：`WeakDamageRule` 倍率由目标是否持有决定；测试敌人虚弱 + 玩家持有 → 6 → 3；无纸鹤 → 4；玩家自己虚弱仍 0.75。
- [ ] 三件遗物都不出现在 `Buffs` 里（断言 `Player.Buffs` 中没有遗物 Id）。

**阶段门槛：** 三件遗物分别位于三个不同挂钩点，删除任一件的类不影响另两件。

## 3. Data：资产与配置（约 1.5h）

- [ ] 新增 `RelicData`；创建金刚杵 / 蛇颅骨 / 纸鹤三份资产。
- [ ] `BattleConfig` 增加 `relics` 列表、`ToRelicStates`、校验（空引用 / 未知 Id / 重复）。
- [ ] `BattleController` 用配置的遗物构造 Session；暴露遗物 Id 与显示名。
- [ ] `Default.asset`：遗物三件。
- [ ] `BattleConfig.TryValidate` 遗物校验的 EditMode 测试。

**阶段门槛：** 只改 Inspector 就能增减遗物。

## 4. 表现层（约 1h）

- [ ] `BattleView` 新增遗物栏行。
- [ ] 新增 `RelicDisplayNames`（或按结论用 Controller 映射）。
- [ ] 场景补文本对象并接线。

**阶段门槛：** 遗物栏显示三件名称。

## 5. 联调与验收（约 1.5h）

- [ ] 完整运行一局：对照游戏设计第 5 节逐条核对；纸鹤要先打出虚弱卡给敌人再结束回合。
- [ ] 分别只配置一件遗物运行三次，核对 7 点伤害 / 4 层中毒 / 3 点敌人伤害。
- [ ] 运行全部 EditMode 测试；Console 无错误。

**阶段门槛：** 游戏设计第 5 节全部满足。

## 6. 文档与交付（约 0.5h）

- [ ] 更新 `README.md`：`Core/Relics` 目录说明、遗物三件、测试数量。
- [ ] 更新 [`../README.md`](../README.md) 的 0.5 状态。
- [ ] 提交并推送 0.5。

## 建议日程

| 开发日 | 当日交付 |
| --- | --- |
| Day 1 | 第 1 节：遗物基础设施 |
| Day 2 | 第 2 节：三件遗物与测试 |
| Day 3 | 第 3～4 节：资产、配置、表现层 |
| Day 4 | 第 5～6 节：联调、文档、提交（含缓冲） |

## 范围控制

开发中出现以下想法时，记录到 0.6 / 1.x 候选，不立即实现：

- "顺便把钨条也做了，就在 `LoseHp` 前加一行。"（它是另一个挂钩点，等有版本专门放它）
- "顺便让施加钩子支持取消。"（人工制品没来）
- "顺便给遗物加回合开始钩子。"（没有消费者）
- "顺便做遗物图标。"（文字即可）

只有阻止本版本验收的问题才进入当日任务。
