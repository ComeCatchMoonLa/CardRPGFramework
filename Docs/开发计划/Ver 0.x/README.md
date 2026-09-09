# Ver 0.x：把局内做成「卡牌战斗」

> **大版本目标**：0.1 已经证明 Action / Effect / Buff / Rule 的分层能跑，0.2 证明了复合卡只是配数据，0.3 让规则在界面可见，0.4 证明了类型决定去向；0.x 剩下的工作是把这套分层用真实的卡牌战斗内容压一遍——遗物、会变招的敌人、有时限的 Debuff——每一步都必须多证明一个边界，而不是多几张同质卡。
>
> **不在 0.x 里的**：局外 Run、地图、奖励、商店（全部归 1.0 及以后）；Luban、出包、程序集拆分（归 1.x）。

版本号约定与细化深度规则见 [`../README.md`](../README.md)。**小版本按功能切，不按工期切**：一个小版本只讲清一条边界，体量可以从一两天到四五天不等；文档里把有硬耦合的东西捆在同一版（效果列表与剑柄打击的打出时序、消耗堆与能力牌的默认去向、刚施加保护与会上虚弱的敌人、三件遗物），其余各自成版。

## 0.1 局内框架（已完成）

Phase 1 + Phase 2a / 2b / 2c，文档在 [`0.1/`](0.1/)。

| 已有 | 说明 |
| --- | --- |
| 战斗循环 | `BattleSession`：回合、能量、抽牌 / 弃牌 / 重洗、胜负、结束后拒绝操作 |
| Action / Effect | `ActionQueue` FIFO，可在执行中继续入队；`Damage / Block / Heal / HpLoss / ApplyBuff` 五种 Action，对应 Effect 只改 `CombatantState` |
| Buff | `BuffState` 容器 + `IBuffTrigger.OnTurnStart`；力量 / 中毒 / 虚弱 / 易伤四种；`LoseHp` 是生命减少唯一入口 |
| Rule | `DamageCalculator` 固定两段管线（力量加法 → 虚弱 / 易伤乘法 → 一次取整）；玩家出牌与敌人固定攻击同一条路径 |
| 配置 | `CardData` / `BattleConfig` ScriptableObject，启动期校验；14 张卡、7 种 |
| 表现 | `BattleController` 持有 Session；`BattleView` / `CardButtonView` / `EndScreenView` 只读状态 |
| 测试 | 79 项 EditMode |

0.1 留下的、后续版本要接的口子：

- ~~`CardType` 同时表示「效果种类」，`EnqueueCardAction` 一种效果一个 `case`~~（0.2 已解决：`EffectSpec` 列表 + `ToAction` 穷举效果原语）。
- ~~打出时序是先结算再移出手牌，抽牌卡会把 `handIndex` 指错~~（0.2 已解决：先离手 → 结算 → 再进弃牌）。
- ~~Rule 只在测试里成立，Play 里卡面写 6、实际掉 9，看不见规则~~（0.3 已解决：`BattleSession` 两个只读预览 + Buff 行，预览 = 结算）。
- ~~只有三个牌区，没有消耗堆~~（0.4 已解决：`CardPile` 消耗堆只进不出，`TryPlayCard` 按 `ExhaustsWhenPlayed` 分流落堆）。
- `ApplyBuffAction` 的 `source` 尚无消费者（0.5 蛇颅骨兑现）。
- 敌人只有一个固定伤害数字，没有格挡、没有行动表（0.6）。
- 虚弱 / 易伤层数永不衰减；0 层 Buff 留在字典里、`HasBuff` 用 `> 0` 兜住（0.7）。

## 0.2 效果列表（已完成）

文档：[`0.2/游戏设计.md`](0.2/游戏设计.md) / [`0.2/技术设计.md`](0.2/技术设计.md) / [`0.2/TODO.md`](0.2/TODO.md)。

- 一张牌 = 费用 + 效果列表；`CardType` 只剩原版意义上的攻击 / 技能。新增痛击（复合）、双击（同效果两段）、剑柄打击（打出时抽牌），已有七种迁到列表。
- 打出时序改为「先离手 → 结算 → 再进弃牌」，是抽牌类效果做对的前提，与剑柄打击必须同版。
- 卡面描述由效果列表生成（基础数值）。

| 已有 | 说明 |
| --- | --- |
| 效果原语 | `Core/Cards`：`EffectKind`（Damage / Block / Heal / ApplyBuff / Draw，封闭枚举）、`EffectTarget`（Self / Opponent）、`readonly struct EffectSpec`（只经静态工厂构造）；`CardDefinition.Effects` 至少一条、构造时拷贝 |
| 转换点 | `BattleSession.ToAction` 是唯一的 `EffectSpec → IAction` 转换，`switch` 穷举效果原语；`EnqueueCardAction` 删除。`BuffFactory` 是 Buff Id → 实例的唯一转换；`DrawCardsAction` 直接调 `CardPile.Draw`，不设 `DrawEffect` |
| 打出时序 | `TryPlayCard`：扣能量 → `TakeFromHand` → 逐条入队并 `RunAll` → `AddToDiscard`；`CardPile.PlayCard` 删除 |
| 配置 | `EffectSpecData` 可序列化条目，`CardData.effects` 列表；启动期校验 `value > 0`、`buffId` 已知、kind 与 target 一致、`CardType` 未越界；17 张卡、10 种 |
| 表现 | `CardDescriptionFormatter.Format(CardDefinition)` 每条效果一行（基础数值），`BuffDisplayNames` 维护 Buff 中文名 |
| 测试 | 139 项 EditMode（0.1 的 79 项只改构造、未改断言；新增效果原语 / 工厂 / 牌堆新方法 / Data 校验 / 卡面文案，以及痛击顺序、双击分段取整、剑柄打击时序与重洗三张锚点卡） |

**证明的边界**：新卡 = 配数据，不在 `BattleSession` 里新增按卡的分支。痛击、双击、剑柄打击三张卡各自只是一份 `CardData`，运行时代码为它们新增的分支为零。

## 0.3 可见规则（已完成）

文档：[`0.3/游戏设计.md`](0.3/游戏设计.md) / [`0.3/技术设计.md`](0.3/技术设计.md) / [`0.3/TODO.md`](0.3/TODO.md)。

- 双方 Buff 层数、攻击牌与敌人意图的公式后伤害在界面上可见；预览与结算走同一个 `DamageCalculator`。
- 不改任何规则，只加两个只读预览函数和表现层。

| 已有 | 说明 |
| --- | --- |
| 预览 | `BattleSession.PreviewPlayerAttack(int)` / `PreviewEnemyAttack()`：新建 `DamageContext` 交给 `DamageCalculator`，返回**格挡前**最终伤害，不改状态；`(基础值, source, target)` 配对与 `ToAction` / `TryEndPlayerTurn` 相同。`BattleController` 原样转发 |
| 卡面 | `CardDescriptionFormatter.Format(card, Func<int,int>)` 只把 Damage 行换成预览值、其余行不变，委托非空；无委托版本保留给 1.0 奖励页 |
| 表现 | `BattleView` 双方 Buff 行（`名称 层数`，两个空格连接，0 层过滤、不改数据）、意图 `攻击 {预览}`、卡面用预览重载；`Views` 里没有 0.75 / 1.5 等公式常数 |
| 测试 | 149 项 EditMode（0.1 / 0.2 的 139 项不改断言；新增预览 = 结算 6 项——先预览再打出 / 结束回合比 HP 差值、痛击不含自身易伤、连续预览不改状态；卡面预览重载 4 项） |

**证明的边界**：Rule 在 Play 模式里看得见，且预览等于结算（格挡前）。

## 0.4 消耗堆与能力牌（已完成）

文档：[`0.4/游戏设计.md`](0.4/游戏设计.md) / [`0.4/技术设计.md`](0.4/技术设计.md) / [`0.4/TODO.md`](0.4/TODO.md)。

- 第四牌区消耗堆；消耗关键词 `bool Exhaust`；能力牌类型默认进消耗堆。力量强化改为能力牌，新增坚不可摧（带消耗的技能），两者都只是配数据。卡面标类型，勾消耗的牌印"消耗"。
- "消耗"只在打出时生效：`DiscardHand` 与重洗一字未改，留手的消耗牌回合结束照常进弃牌堆。

| 已有 | 说明 |
| --- | --- |
| 去向 | `CardType` 追加 `Power`（末尾，旧资产整数不错位）；`CardDefinition.Exhaust`（构造函数末尾可选参数，默认 false）与 `ExhaustsWhenPlayed => Exhaust \|\| Type == Power`——去向是牌自己的规则。`BattleSession.TryPlayCard` 结算后按它 `AddToExhaust` / `AddToDiscard`，Session 里没有 `CardType.Power` / `.Exhaust` |
| 牌区 | `CardPile` 消耗堆只进不出：`AddToExhaust`（null 抛异常）、`ExhaustPileCount`，不公开列表；`ReshuffleDiscardIntoDrawPile` / `DiscardHand` 未改。`BattleSession` 新增只读 `ExhaustPileCount` |
| 配置 | `CardData.exhaust`；`Strength.asset` 类型改为能力，新增 `Impervious.asset`（2 费技能、格挡 30、勾消耗）；18 张卡、11 种 |
| 表现 | `BattleController` 转发 `ExhaustPileCount`；`BattleView` 牌堆数量行 `抽牌 X  弃牌 Y  消耗 Z`、卡面 `名字 / 类型 · 费用 X / 描述`（类型中文是 View 私有 `switch`）；`CardDescriptionFormatter` 在 `Exhaust` 为 true 时末行追加 `消耗`，能力牌不印，两个重载共用 |
| 测试 | 165 项 EditMode（0.1～0.3 的 149 项不改断言；新增 16 项：`CardDefinitionTests` 三种去向 3 项；`CardPileTests` 消耗堆 / 重洗只回收弃牌堆 / `DiscardHand` 不看关键词 5 项；`BattleSessionTests.Exhaust.cs` 5 项——四区之和恒等于牌组张数且连续三回合抽不到能力牌、坚不可摧进消耗堆与留手进弃牌堆、0.1 的 `Strength()` 仍是技能进弃牌堆；`CardDescriptionFormatterTests` 卡面 `消耗` 行 3 项——勾消耗的技能印、能力牌不印、预览重载同样追加） |

**证明的边界**：类型决定去向而不是效果；关键词与类型默认落到同一个牌区。新增消耗牌 / 能力牌都是一份 `CardData`，运行时代码为它们新增的分支为零。

## 0.5 遗物三钩子

文档：[`0.5/游戏设计.md`](0.5/游戏设计.md)；[`0.5/技术设计.md`](0.5/技术设计.md) / [`0.5/TODO.md`](0.5/TODO.md) 为草稿，进入前复核。

- 遗物列表挂在玩家身上、与 Buff 字典并列；三件遗物各占一种钩子：金刚杵（开战注入）、蛇颅骨（改这一次施加）、纸鹤（改虚弱倍率常数）。

**证明的边界**：遗物不是 Buff；「改公式常数」与「改一次施加」分属 Rule 与施加钩子两层。

## 0.6 敌人行动表

文档：[`0.6/游戏设计.md`](0.6/游戏设计.md)（技术设计与 TODO 进入 0.6 前再补）。

- 敌人从「一个伤害数字」变成 `EnemyData`：血量 + 行动表，行动 = 意图 + 效果列表（与卡牌共用 `EffectSpec`），固定循环。一只原版怪（颚虫）+ 把 0.1 的固定攻击敌人迁成第二份配置；玩家生命 40 → 80。
- 本版本的敌人不给玩家上虚弱 / 易伤——没有减层就上，那层永远不掉。

**证明的边界**：敌人行动与卡牌走同一套效果原语；新敌人 = 配数据。

## 0.7 回合型 Buff

文档：[`0.7/游戏设计.md`](0.7/游戏设计.md)（技术设计与 TODO 进入 0.7 前再补）。

- 虚弱 / 易伤的层数是「剩余轮数」：轮结束统一减 1，刚施加的不减，0 层移除。
- 加入蓝奴隶贩子（耙：7 伤 + 给玩家 1 层虚弱）当刚施加保护的消费者。

**证明的边界**：回合型 Buff 的衰减时机与中毒不同；敌人给玩家上的 Debuff 要能活到玩家下一回合。

## 0.x 完成定义（进入 1.0 的门槛）

- 新增一张卡、一只敌人、一件用现有钩子的遗物，各自最多新增一个 C# 子类，不在 `BattleSession` 里新增按卡牌 / 敌人 / 遗物 Id 或种类的分支。新的效果原语或新的钩子时机仍要改一处 `switch` 或一处遍历——那是有意的封闭点。
- 四个牌区齐全，能力牌打出后本场不再出现。
- 三条伤害规则在界面可见，预览数值与结算一致。
- 遗物三种钩子各有一件标本，且没有任何遗物进入 Buff 字典。
- 至少两只配置不同的敌人，意图循环，公式后预览。
- 虚弱 / 易伤会随轮结束衰减，刚施加的不被立刻减掉；0 层 Buff 不残留。
- 以上全部有 EditMode 测试；0.1 的 79 项测试不改断言仍通过（因接口改动而改构造方式除外）。

满足后，局内已经能支撑「三场不同的战斗 + 战后选卡 + 遗物栏」，才值得开 1.0。
