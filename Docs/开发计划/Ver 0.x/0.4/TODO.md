# 0.4 TODO：消耗堆与能力牌

> **预计 1～2 个有效开发日。改动小、风险低，重点是"消耗只在打出时生效、回合结束仍进弃牌堆"这条规则的测试，以及别让 0.1 的 `Strength()` 辅助方法跟着资产改成能力牌（假绿）。验收时对照 [`技术设计.md`](技术设计.md) 第 8 节检查。**

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。本文由原"0.3 消耗堆、能力牌、遗物"的 TODO 拆出，已对照 0.2 / 0.3 的实际实现复核并转正。

## 0. 开始前检查（约 0.5h）

- [x] 0.3 已提交推送，工作区干净，Console 无错误，EditMode 全绿。
- [x] 关键词表示已定：`bool Exhaust`，不做 `[Flags]`（技术设计 4.1 [已定]）。

**阶段门槛：** 有干净的回归基线。

## 1. Core：第四牌区与去向（约 2h）

- [x] `CardType` 追加 `Power`；`CardDefinition` 增加 `exhaust` 可选参数、`Exhaust` 与 `ExhaustsWhenPlayed`。
- [x] `CardPile` 增加消耗堆：`AddToExhaust`（null 抛异常）、`ExhaustPileCount`；**不改** `ReshuffleDiscardIntoDrawPile` 与 `DiscardHand`，不公开消耗堆列表。
- [x] `BattleSession`：落堆改为按 `ExhaustsWhenPlayed` 分流；新增只读 `ExhaustPileCount`。
- [x] 测试：`CardDefinition` 三种去向 + 默认 `Exhaust` 为 false；`CardPile` 的 `AddToExhaust` / null / 两堆空时不取消耗堆 / `DiscardHand` 不看关键词；`BattleSessionTests.Exhaust.cs` 里"四区之和 = 牌组张数且消耗堆为 1、重洗后抽不到能力牌""坚不可摧格挡 30 进消耗堆、留手进弃牌堆"。新增 `Inflame()` / `Impervious()` 辅助方法，**不改** 0.1 的 `Strength()`。

**阶段门槛：** 能力牌打出后整场不再出现，有测试证明；0.1～0.3 测试不改仍通过，`Strength()` 仍是 Skill。

## 2. Data：资产与配置（约 1h）

- [x] `CardData` 增加 `exhaust`；`Strength.asset` 在 Inspector 里显式把类型改为能力（`Power = 2` 之后校验拦不住旧整数，别指望它提醒）；新增 `Impervious.asset`（坚不可摧：技能、2 费、格挡 30、勾消耗）。
- [x] `Default.asset`：牌组 18 张。

**阶段门槛：** 只改 Inspector 就能增减消耗牌 / 能力牌。

## 3. 表现层（约 1.5h）

- [x] `BattleController` 转发 `ExhaustPileCount`。
- [x] `BattleView` 新增牌堆数量行（抽牌 / 弃牌 / 消耗）；场景补文本对象并接线。
- [x] 卡面：`RefreshHand` 加类型标签（`类型 · 费用 X`，类型中文用 `BattleView` 私有 `switch`）。
- [x] `CardDescriptionFormatter`：`Exhaust` 为 true 时末尾追加一行 `消耗`，能力牌不印；测试：勾消耗的技能末行 `消耗`、Power 不印、带预览重载同样追加；0.2 / 0.3 用例不改断言。

**阶段门槛：** 打出能力牌后界面消耗堆数字 +1；坚不可摧卡面有 `消耗`，力量强化卡面标 `能力` 且无 `消耗`。

## 4. 联调与验收（约 1h）

- [x] 完整运行一局：打出力量强化观察消耗堆 +1；打坚不可摧观察格挡 30 与消耗堆 +1；把坚不可摧留在手里结束回合，观察它进弃牌堆、消耗堆不变；多打几回合确认重洗后抽不到力量强化。
- [x] 卡面对照：所有手牌费用行都带类型（`攻击 / 技能 / 能力 · 费用 X`）；坚不可摧末行 `消耗`，力量强化标 `能力` 且无 `消耗`；其余牌的描述行与 0.3 相同。
- [x] 运行全部 EditMode 测试；Console 无错误；`BattleSession` 里 grep 不到 `CardType.Power` 与 `.Exhaust`（属性名 `ExhaustsWhenPlayed` 是唯一允许出现的）。

**阶段门槛：** 游戏设计第 5 节全部满足。

## 5. 文档与交付（约 0.5h）

- [x] 更新 `README.md`：卡牌 18 张 / 11 种、四个牌区、卡面类型与关键词、测试数量。
- [x] 更新 [`../README.md`](../README.md) 的 0.4 状态（节标已完成、摘要表；0.1 遗留口子里"只有三个牌区"那条划掉）。
- [x] 更新 [`../../README.md`](../../README.md)（开发计划总览）：路线总览 0.4 标已完成与测试数；文档地图。
- [ ] 提交并推送 0.4。

## 范围控制

开发中出现以下想法时，记录到 0.5 及以后，不立即实现：

- "顺便把关键词做成 Flags。"（第二个关键词还没出现）
- "顺便做一张'消耗手牌中一张牌'的卡。"（被效果消耗是另一个时机）
- "顺便显示消耗堆里有哪些牌。"（数量即可）
- "顺便让 `DiscardHand` 看关键词。"（那是虚无的语义，0.4 没有虚无）
- "顺便把类型中文抽成 `CardTypeDisplayNames`。"（1.0 奖励页出现第二个调用方再抽）

只有阻止本版本验收的问题才进入当日任务。
