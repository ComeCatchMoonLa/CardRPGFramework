# 0.2 TODO：效果列表

> **预计 4～5 个有效开发日。第 2 节是本版本唯一"修改已有行为路径"的切片，也是唯一让程序集短暂编不过的切片——`CardDefinition` 删 `Value` 会同时打断 `BattleSession` / `CardData` / `BattleView`，所以这些调用点必须在同一切片里改完，改完先跑 0.1 的全部 `BattleSessionTests`（只改构造、不改断言）再往下走。七份资产迁移要按 [`技术设计.md`](技术设计.md) 第 4.7 节的枚举重编号提醒逐份核对。**

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。技术设计里四处接口形状已标 **[已定]**（`readonly struct` / 删 `PlayCard` / 不设 `DrawEffect` / `ToAction` 留在 Session），实现时按已定执行，不再重议。

## 0. 开始前检查（约 0.5h）

- [x] 0.1 已提交推送，工作区干净，Unity Console 无错误，79 项 EditMode 测试全部通过。
- [x] 读过技术设计第 4.1 / 4.4 / 4.5 / 4.6 节的四处 [已定]。

**阶段门槛：** 有干净的回归基线。

## 1. Core：只加不改（约 2～3h，编译全程保持绿）

- [x] 新增 `EffectKind`、`EffectTarget`、`EffectSpec`（`readonly struct`，静态构造与校验）。
- [x] 新增 `BuffFactory`（`IsKnown` / `Create`，四个 Id）。
- [x] `CardPile` 新增 `TakeFromHand` / `AddToDiscard`（暂不删 `PlayCard`，下一节一起删）。
- [x] 新增 `DrawCardsAction`（直接调 `CardPile.Draw`，不设 `DrawEffect`）。
- [x] 编写 `EffectSpec`（Value ≤ 0、ApplyBuff 缺 Id、Draw 非 Self 抛异常）、`BuffFactory`（四个 Id 与未知 Id）、`CardPile` 新方法（含"`TakeFromHand` 后立刻重洗不包含该牌"）的 EditMode 测试。

**阶段门槛：** 编译通过，79 + 新增测试全绿；已有行为一行未动。

## 2. Core + Data + View：破坏性改动一次做完（约 4～5h，一次提交）

这一节里的改动互相牵连，拆开做就会停在红编译上；按顺序改完再编译。

- [ ] `CardType` 改为 `{ Attack, Skill }`；`CardDefinition` 用 `Effects` 替换 `Value`（构造时拷贝、至少一条）。
- [ ] `BattleSession.TryPlayCard` 改为"扣能量 → `TakeFromHand` → 按列表 `ToAction` 入队 → `RunAll` → `AddToDiscard`"；删除 `EnqueueCardAction`，新增私有 `ToAction` 与 `Opponent`。
- [ ] 删除 `CardPile.PlayCard`。
- [ ] 新增 `EffectSpecData`（`ToSpec` / `TryValidate`，含 kind 与 target 一致性校验）；`CardData` 增加 `effects`、删除 `value`、扩展 `TryValidate`。
- [ ] 新增 `BuffDisplayNames`、`CardDescriptionFormatter.Format(CardDefinition)`；`BattleView.RefreshHand` 用它替换 `数值 {card.Value}`。
- [ ] 修改所有构造 `CardDefinition` 的测试辅助方法（`BattleSessionTests` 的 `Attack/Defend/Heal/Strength/Poison/Weak/Vulnerable/BuildDeck`、`CardPileTests.BuildDeck`），只改构造。
- [ ] `CardPileTests.PlayCard_MovesCardFromHandToDiscardPile` 改写为 `TakeFromHand` 与 `AddToDiscard` 两个用例（本版本唯一不只改构造的旧测试）。
- [ ] 运行 0.1 全部 `BattleSessionTests`，确认断言未改仍通过（包括中毒致死、抽牌堆耗尽重洗）。
- [ ] `EffectSpecData.TryValidate` 的 EditMode 测试（未知 buffId、value ≤ 0、Damage + Self）。

**阶段门槛：** 编译通过；玩家可见行为与 0.1 一致；抽牌堆为空时重洗不会把打出中的牌洗回去；`BattleSession` 里没有按卡牌种类的分支。

## 3. Core：三张锚点卡的行为测试（约 1～2h）

- [ ] 痛击：先掉 8 血再得 2 层易伤，8 点不吃刚施加的易伤。
- [ ] 双击：无 Buff 掉 10；敌人易伤掉 14（不是 15）。
- [ ] 剑柄打击：手牌数不变、弃牌堆数量 +1、末张来自抽牌堆；抽牌堆为空时重洗不含刚打出的那张。

**阶段门槛：** 三张卡都只用 `EffectSpec` 列表构造，没有任何新分支代码。

## 4. Data：资产迁移与新增（约 2h）

- [ ] 逐份重设七份 `CardData`：类型（攻击 / 技能，剧毒是技能）+ 效果列表；删掉 YAML 里的 `value:`；不要依赖"Attack=0 / Defend=1 碰巧还对"。
- [ ] 新增痛击、双击、剑柄打击三份 `CardData`（数值见游戏设计第 2.2 节）。
- [ ] `Default.asset` 牌组加入三张新卡，共 17 张；提交前核对不要留下 Play 验收时临时堆的牌。

**阶段门槛：** Play 模式启动无校验报错；新卡仅通过 Inspector 配置出现在战斗中。

## 5. 联调与验收（约 1.5h）

- [ ] 完整运行一局：打痛击、双击、剑柄打击各一次，对照游戏设计第 6 节逐条核对。
- [ ] 卡面描述与效果列表逐条一致（痛击两行、双击两行、剑柄打击两行）。
- [ ] 抽牌堆快空时打剑柄打击，观察重洗后手牌里没有刚打出的那张。
- [ ] 运行全部 EditMode 测试；Console 无错误。

**阶段门槛：** 游戏设计第 6 节全部满足。

## 6. 文档与交付（约 0.5h）

- [ ] 更新 `README.md`：卡牌 17 张 / 10 种、`Core/Cards` 目录说明、测试数量、已知限制里"`EnqueueCardAction` 结算顺序"一条改为已解决并描述新时序。
- [ ] 更新 [`../README.md`](../README.md) 的 0.2 状态。
- [ ] 提交并推送 0.2（不要带上 `Assets/TextMesh Pro/Fonts/*.asset` 的 Play 抖动）。

## 建议日程

| 开发日 | 当日交付 |
| --- | --- |
| Day 1 | 第 1 节：效果原语、工厂、牌堆新方法、抽牌 Action，编译绿 |
| Day 2 | 第 2 节：一次性切换到效果列表与新时序，回归全绿 |
| Day 3 | 第 3～4 节：锚点卡测试、资产迁移与新增 |
| Day 4 | 第 5～6 节：联调验收、文档、提交 |
| Day 5 | 缓冲 |

## 范围控制

开发中出现以下想法时，记录到 0.3 及以后，不立即实现：

- "顺便把公式后伤害显示出来。"（0.3 的内容，需要 Session 暴露预览）
- "顺便加个手牌上限 10。"（没有消费者）
- "顺便让效果支持条件 / 随机目标。"
- "顺便把描述文案做成配置字段。"（生成即可，两套真相更糟）
- "顺便保留 `PlayCard` 当快捷方法。"（已定删除）

只有阻止本版本验收的问题才进入当日任务。
