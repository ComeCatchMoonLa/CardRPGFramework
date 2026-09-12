# 0.7 TODO：回合型 Buff

> 随 0.6 三件套一并写成草稿；第 0 节（对照 0.6 实际代码复核转正）已做完（`098f2cd`），实现从第 1 节开始。条目基于转正后的 [`技术设计.md`](技术设计.md)。0.6 完成时 EditMode 246 项，本文数字以此为基数。
>
> **预计 2～3 个有效开发日。风险有两条：一是把减层做成中毒那种"结算时自减"或挂到各自回合结束——减层只在轮结束、双方一起（[`../../../Phase 2 扩展边界批注.md`](../../../Phase%202%20扩展边界批注.md) 第 4 节）；二是"叠加也刷新保护"的直觉——原版不刷新，游戏设计第 4 节第三个例子就是为它写的。验收时对照技术设计第 8 节检查。**

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。

## 0. 开始前检查与复核（约 1h）

- [x] 0.6 已提交推送（`1245c29`），工作区干净，Console 无错误，EditMode 全绿（246 项，本文与技术设计的基数已按它替换）。
- [x] 走完技术设计第 9 节复核清单：`ToAction` 形状、`TryEndPlayerTurn` 里推进与判负的先后、`EnemyActionData.TryValidate` 那条校验的实际写法、`CreateSession` 签名、"上 Buff → 结束回合 → 断言"用例清单（7 个，全部只断言 HP）、意图文案所在层。结论记在技术设计第 9 节。
- [x] 确认两处已定的接口形状与 0.6 实际代码兼容：保护标记走 `BuffFactory.Create` 可选参数——`ToAction` 的 `ApplyBuff` 分支是 `EffectSpec → Buff` 的唯一构造点（`VajraRelic` 另有一处造力量，忽略该参数）；`OnRoundEnd()` 无参数。兼容，不改。
- [x] 去掉三份文档的草稿头。

**阶段门槛：** 技术设计与 0.6 实际代码一致，可以照着写。

## 1. Core：轮末减层与刚施加保护（约 3h）

- [x] 新增 `IRoundEndTrigger`（`OnRoundEnd()`）与 `DurationBuff : BuffState, IRoundEndTrigger`（`_justApplied` 跳过一次，否则 `RemoveStacks(1)`）；`WeakBuff` / `VulnerableBuff` 改为继承它，构造函数多一个 `justApplied = false`。`BuffState` 不改。
- [x] `BuffFactory.Create(id, stacks, justApplied = false)`：只对虚弱 / 易伤传下去。`IsKnown` 不改。
- [x] `BattleSession.ToAction` 的 `ApplyBuff` 分支传 `justApplied: source == Enemy`；注释写清"只看施加方（一代 `isSourceMonster`），不看目标是谁、也不看所有者本轮是否已行动"。
- [x] `CombatantState.RemoveExpiredBuffs()`：移除 `Stacks <= 0` 的条目——先收集 key 再逐个 `Remove`，不在枚举 `_buffs` 时删；`ApplyBuff` / `HasBuff` 不改。
- [x] `BattleSession.TryEndPlayerTurn`：在 `AdvanceEnemyAction` 之后、未失败分支里 `EndRound()` 再 `StartPlayerTurn()`；`EndRound` = 双方 `TriggerRoundEnd`（`foreach` + `is IRoundEndTrigger`）→ 双方 `RemoveExpiredBuffs`。与开战 / 回合开始两个遍历并列，不抽公共方法。
- [x] 测试：`BuffTests`（+4：减层、保护跳过一次、易伤同、力量 / 中毒不是 `IRoundEndTrigger`）；`BuffFactoryTests`（+2）；`CombatantStateTests`（+2：`RemoveExpiredBuffs`、叠加保留原实例）；新建 `BattleSession/BattleSessionTests.RoundEnd.cs`（+9：痛击 2 层覆盖两轮、玩家上虚弱轮末消失、耙后保护与下一回合 ×0.75、连续两次耙 1 / 1 / 0、敌人给自己上易伤轮末不减（锁住判定看施加方而不是"目标是玩家"）、中毒不轮末减、中毒 0 层移除、玩家自己带虚弱、失败时不轮结束），用测试内 `Raker()`、`[耙, 耙, 待机, 待机]` 与 `[ApplyBuff Self vulnerable 1]` 三个敌人定义；待机是 Core 的空 `EnemyAction`（`Array.Empty<EffectSpec>()`），不用 `Damage(0)`，也不为测试放开 Data 校验。

**阶段门槛：** 游戏设计第 4 节三个例子各有用例通过；`BuffState.cs`、`ApplyBuffAction.cs`、三条 Rule 无改动；0.1～0.6 用例不改断言。（第 1 节完成 263 项 = 246 + 17；三个文件 diff 为空，既有测试文件只增行。）

## 2. Data：放开校验与蓝奴隶贩子（约 0.5h）

- [x] `EnemyActionData.TryValidate` 删掉"ApplyBuff 且 Opponent"那一条与注释；`Draw` 与空 `effects` 仍拒绝。`EnemyActionDataTests.TryValidate_ApplyBuffOnOpponent_ReturnsError` 改为 `_Passes`。
- [x] 新增 `Assets/Data/Enemies/BlueSlaver.asset`（`blue_slaver` / 蓝奴隶贩子 / 48；刺击 12；耙 7 + 给玩家 1 层虚弱）。`Default.asset` 保持颚虫。

**阶段门槛：** 换成蓝奴隶贩子只需改 `Default.asset` 的引用。

## 3. 表现层（约 0.5h）

- [x] 不改代码。Play 里确认：`虚弱 1` 在下一轮末消失、`易伤 2` 逐轮变 1 变无、Buff 行不再出现 0 层条目、意图 `攻击 7 · 减益`。（与第 4 节前两条同一局观察；意图文案另有 0.6 的 `IntentFormatterTests` 锁住。）

**阶段门槛：** 界面显示与内部状态一致。

## 4. 联调与验收（约 1h）

- [x] 蓝奴隶贩子一局：耙之后玩家 Buff 行 `虚弱 1`，下一回合攻击卡卡面 5、打出掉 5，再下一回合虚弱消失、卡面 7；对照游戏设计第 4～5 节逐条核对。
- [x] 颚虫一局：打痛击后敌人 `易伤 2` → 下一回合 `易伤 1` → 再下一回合消失；剧毒减到 0 后 Buff 行没有中毒。
- [x] 运行全部 EditMode 测试；Console 无错误；grep 核对：`BattleSession.cs` 里三个固定时机各是独立方法；`BuffState.cs` 无 diff。（263 项通过；`TriggerBattleStartRelics` / `TriggerTurnStartBuffs` / `EndRound` + `TriggerRoundEnd` 各是独立私有方法；`BuffState.cs`、`ApplyBuffAction.cs`、`Core/Rules` 对 HEAD 无 diff；`BattleSession.cs` 里没有任何字符串字面量。）

**阶段门槛：** 游戏设计第 4～5 节全部满足。

## 5. 文档与交付（约 1h）

- [x] 更新 `README.md`：`Core/Buffs` 目录说明加 `IRoundEndTrigger` / `DurationBuff`、敌人三只、测试数；已知限制里"虚弱 / 易伤层数永不衰减"那条删掉。（"范围外"里的 0.7 那条与 `EnemyTurn` 限制里"等 0.7 放开"的措辞一并改掉，新增一条"回合型 Buff 只有虚弱 / 易伤、时机只有轮结束"的限制。）
- [x] 更新 [`../README.md`](../README.md)：0.7 节标"（已完成）"与摘要表；0.1 遗留口子里"虚弱 / 易伤层数永不衰减"划掉；**0.x 完成定义逐条核对打勾**——满足即进入 1.0。
- [x] 更新 [`../../README.md`](../../README.md)：路线总览 0.7 标已完成与测试数；文档地图；第 2 节例外说明。
- [x] 提交并推送 0.7（分两次：Core 与测试、Data 与文档）。

## 建议日程

| 开发日 | 当日交付 |
| --- | --- |
| Day 1 | 第 1 节 Core 与用例（第 0 节已完成） |
| Day 2 | 第 2～4 节：校验、资产、联调 |
| Day 3 | 第 5 节：文档、0.x 完成定义核对、提交（含缓冲） |

## 范围控制

开发中出现以下想法时，记录到 1.x 候选，不立即实现：

- "顺便做脆弱，就是再来一个 `DurationBuff` 子类。"（没有消费者；`BlockAction` 也没有格挡公式管线）
- "顺便加 `OnTurnEnd`（角色行动结束）时机，金属化 / 活动肌肉要用。"（没有消费者）
- "顺便让叠加刷新保护，玩家体验更直观。"（原版不刷新；会多出一回合谁都没施加过的虚弱）
- "顺便做人工制品，施加钩子已经有了。"（需要取消与极性，另一个版本）
- "顺便让蓝奴隶贩子按原版 60/40 随机。"（随机行动随 1.0 的 Run 种子一起考虑）
- "顺便把 0 层过滤从 `BattleView` 删掉。"（不是无害，是还在用：回合开始自减到 0 的中毒要到轮末才出字典；负层出现时再看）

只有阻止本版本验收的问题才进入当日任务。
