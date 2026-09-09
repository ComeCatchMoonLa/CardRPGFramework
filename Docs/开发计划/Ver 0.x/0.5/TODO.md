# 0.5 TODO：遗物三钩子

> **预计 3～4 个有效开发日。风险有两条：一是图省事把三件都做成"开战给一个 Buff"——三件遗物必须落在三个不同位置（开战钩子 / `ApplyBuffAction` 内 / `WeakDamageRule` 内），写之前对照 [`技术设计.md`](技术设计.md) 第 4.1～4.4 节；二是测试只和 Rule 对拍——按 0.3 的做法先锁预览再比 HP 差值，蛇颅骨走打出剧毒。验收时对照技术设计第 8 节检查。**

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。本文由原"0.3 消耗堆、能力牌、遗物"的 TODO 拆出，已对照 0.2～0.4 的实际实现复核并转正；四处 [待确认] 已定（技术设计第 7 节）。

## 0. 开始前检查（约 0.5h）

- [x] 0.4 已提交推送，工作区干净（`Assets/TextMesh Pro/Fonts/*.asset` 的 Play 抖动还原或单独提交），Console 无错误，EditMode 165 项全绿。
- [x] 四处 [待确认] 已定并写回技术设计：施加钩子直接改 `BuffState`；加 `BuffIds` 且单独提交；遗物挂 `CombatantState`；显示名走 `RelicData` + Controller。

**阶段门槛：** 有干净的回归基线。

## 1. Core：BuffIds 与遗物基础设施（约 2～3h）

- [x] **单独一步、单独提交**：新增 `Core.Buffs.BuffIds`；替换 `Assets/Scripts/Runtime` 里的 9 处字面量（四个 Buff 子类的 `Id`、`BuffFactory`、三条 Rule、`BuffDisplayNames`）；**不改**测试文件里的字面量；`Assets/Data/Cards/*.asset` 里的 `buffId: strength` 是资产数据，本来就不改，grep 核对只看 `Assets/Scripts/Runtime`；165 项全绿后提交（提交说明写"重构：Buff Id 字面量收进 BuffIds"，中文）。
- [ ] 新增 `Core.Relics` 的基础部分：`RelicState`、`RelicIds`、`IBattleStartRelic`、`IApplyBuffModifier`。`RelicFactory` 和三个遗物类一起放第 2 节——工厂的 `Create` 要 `new` 三个类，拆开编不过。
- [ ] `CombatantState` 增加 `Relics` / `AddRelic`（重复 Id 抛 `ArgumentException`）/ `HasRelic`。
- [ ] `BattleSession` 构造函数增加可选 `relics` 参数；`StartBattle` 在 `StartPlayerTurn()` 之前调用 `TriggerBattleStartRelics(Player)`（`foreach` + `is IBattleStartRelic` + `RunAll`；此时还没有实现者，行为由第 2 节金刚杵的用例锁）。
- [ ] 测试：`CombatantStateTests` 加 `AddRelic` / 重复抛异常 / `HasRelic`，用测试内定义的 `RelicState` 子类（与 `ActionQueueTests.RecordingAction` 同一做法），不依赖三件遗物；`BattleSessionTests.CreateSession` 加 `relics` 可选参数，0.1～0.4 用例不改。

**阶段门槛：** `Core.Relics` 不引用 `UnityEngine`；`Assets/Scripts/Runtime` 里 Buff Id 字面量只剩 `BuffIds.cs`；0.1～0.4 测试不改仍通过。

## 2. Core：三件遗物（约 3h）

- [ ] 金刚杵：`VajraRelic : RelicState, IBattleStartRelic`，`OnBattleStart` 只入队 `new ApplyBuffAction(owner, owner, BuffFactory.Create(BuffIds.Strength, 1))`，不 `new StrengthBuff`、不 `owner.ApplyBuff`。
- [ ] 蛇颅骨：`ApplyBuffAction.Execute` 先遍历**施加方**遗物里的 `IApplyBuffModifier` 再 `BuffEffect.Apply`；`SneckoSkullRelic` 只在 `buff.Id == BuffIds.Poison` 时 `AddStacks(1)`；更新构造函数上"source 尚无消费者"的注释。
- [ ] 纸鹤：`PaperKraneRelic` 无钩子；`WeakDamageRule` 倍率由 `context.Target.HasRelic(RelicIds.PaperKrane)` 决定（0.6 / 0.75 两个常量）。
- [ ] `RelicFactory`（`IsKnown` / `Create`，未知 Id 抛 `ArgumentException`）；新增 `RelicFactoryTests`：三个 Id 各返回正确类型、`IsKnown` 三真一假、未知抛异常。
- [ ] 测试（`BattleSessionTests.Relics.cs`）：金刚杵——开战力量 1、`Player.Buffs` 只有一条、`PreviewPlayerAttack(6) == 7` 且打出后敌人 HP 差 7、无遗物时 0 / 6；蛇颅骨——打出剧毒 4 层、无遗物 3 层、**只配蛇颅骨**时打出力量强化仍 2 层（带金刚杵会是 3，那 1 层是金刚杵的）；纸鹤——先打虚弱给敌人，`PreviewEnemyAttack() == 3` 且结束回合玩家 HP 差 3、无纸鹤 4 / 4、玩家自己带虚弱且持纸鹤时 `PreviewPlayerAttack(6) == 4`；三件同配——`Player.Relics.Count == 3`、`Buffs` 里只有力量且没有任何 `RelicIds.*`。
- [ ] 测试（`ActionQueueTests`）：施加方持蛇颅骨 + 中毒 → 4；+ 力量 → 2；施加方无遗物 → 不变；**目标**持蛇颅骨、施加方没有 → 不变。
- [ ] 测试（`DamageRuleTests`）：目标持纸鹤 6 → 3.6、否则 4.5；纸鹤在施加方身上不生效；2 层与 1 层相同。

**阶段门槛：** 三件遗物分别位于三个不同挂钩点，删除任一件的类只影响它自己的用例；`BattleSession.cs` 里 grep 不到 `RelicIds` / `Vajra` / `Snecko` / `PaperKrane`。

## 3. Data：资产与配置（约 1.5h）

- [ ] 新增 `RelicData`（`id` / `displayName` / `ToState` / `TryValidate`：id 非空且 `RelicFactory.IsKnown`、displayName 非空）；在 `Assets/Data/Relics/` 创建 `Vajra` / `SneckoSkull` / `PaperKrane` 三份资产（显示名：金刚杵 / 蛇颅骨 / 纸鹤）。
- [ ] `BattleConfig` 增加 `relics` 列表（允许为空）、`Relics` 只读属性、`ToRelicStates`；`TryValidate` 增加：空引用 / 各自校验 / Id 重复（同一资产引用两次也算）。
- [ ] `BattleController.Awake` 用 `battleConfig.ToRelicStates()` 构造 Session；新增 `RelicDisplayName(string id)`（Id → displayName 字典，未知原样返回）。
- [ ] `Default.asset`：遗物三件。
- [ ] 不写 SO 校验的单测（与 `CardData` / `BattleConfig` 既有做法一致），配错检查放到第 5 节联调。

**阶段门槛：** 只改 Inspector 就能增减遗物。

## 4. 表现层（约 1h）

- [ ] `BattleView` 新增 `relicsText`，`Refresh` 写 `遗物：金刚杵  蛇颅骨  纸鹤`（Id 经 `battleController.Player.Relics` 读、名字经 `RelicDisplayName`），无遗物写 `遗物：无`。
- [ ] 场景：`TopBar` 下补文本对象（可 Duplicate `PileCounts`）并接线。
- [ ] 不新增 `Views/RelicDisplayNames.cs`。

**阶段门槛：** 遗物栏显示三件名称；Buff 行里没有遗物。

## 5. 联调与验收（约 1.5h）

- [ ] 完整运行一局：开局 Buff 行 `力量 1`、攻击卡卡面 7；打剧毒看敌人 `中毒 4`；打虚弱给敌人后意图从 4 变 3，结束回合掉 3；对照游戏设计第 5 节逐条核对。
- [ ] 分别只配置一件遗物运行三次，核对 7 点伤害 / 4 层中毒 / 3 点敌人伤害各自独立成立。
- [ ] 故意配错一次看 Console：`RelicData.id` 改成未知值 → 启动报错；`Default.asset` 里同一遗物引用两次 → 启动报错；改回后无错误。
- [ ] 运行全部 EditMode 测试；Console 无错误；grep 核对：`BattleSession.cs` 无 `RelicIds` / `Vajra` / `Snecko` / `PaperKrane`，`ApplyBuffAction.cs` 只有 `IApplyBuffModifier`，`Assets/Scripts/Runtime` 里 `"strength"` / `"poison"` / `"weak"` / `"vulnerable"` 只出现在 `BuffIds.cs`（测试文件与 `Assets/Data` 资产里的字面量不在核对范围）。

**阶段门槛：** 游戏设计第 5 节全部满足。

## 6. 文档与交付（约 0.5h）

- [ ] 更新 `README.md`：`Core/Relics` 与 `BuffIds` 目录说明、遗物三件、遗物栏、测试数量、已知限制（钩子里只入队 Action；`IBattleStartRelic` 不带对手）。
- [ ] 更新 [`../README.md`](../README.md) 的 0.5 状态（节标已完成、摘要表；0.1 遗留口子里"`ApplyBuffAction` 的 `source` 尚无消费者"那条划掉）。
- [ ] 更新 [`../../README.md`](../../README.md)（开发计划总览）：路线总览 0.5 标已完成与测试数；文档地图。
- [ ] 提交并推送 0.5（不带 `Assets/TextMesh Pro/Fonts/*.asset` 的 Play 抖动）。

## 建议日程

| 开发日 | 当日交付 |
| --- | --- |
| Day 1 | 第 1 节：`BuffIds` 单独提交 → 遗物基础设施 |
| Day 2 | 第 2 节：三件遗物与测试 |
| Day 3 | 第 3～4 节：资产、配置、表现层 |
| Day 4 | 第 5～6 节：联调、文档、提交（含缓冲） |

## 范围控制

开发中出现以下想法时，记录到 0.6 / 1.x 候选，不立即实现：

- "顺便把钨条也做了，就在 `LoseHp` 前加一行。"（它是另一个挂钩点，等有版本专门放它）
- "顺便让施加钩子支持取消。"（人工制品没来）
- "顺便给遗物加回合开始钩子。"（没有消费者）
- "顺便把纸蛙也做了，就在 `VulnerableDamageRule` 加一行。"（同一种钩子，不多证明边界）
- "顺便给 `OnBattleStart` 传对手。"（弹珠袋没来）
- "顺便做遗物图标 / `RelicDisplayNames`。"（文字即可；显示名已定走 Controller）
- "顺便把测试里的字面量也换成 `BuffIds`。"（保留字面量正好锁常量值）

只有阻止本版本验收的问题才进入当日任务。
