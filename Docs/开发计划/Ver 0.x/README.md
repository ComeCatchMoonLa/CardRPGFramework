# Ver 0.x：把局内做成「卡牌战斗」

> **大版本目标**：0.1 已经证明 Action / Effect / Buff / Rule 的分层能跑，0.2 证明了复合卡只是配数据，0.3 让规则在界面可见，0.4 证明了类型决定去向，0.5 证明了遗物不是 Buff，0.6 证明了会变招的敌人也只是配数据，0.7 证明了回合型 Buff 的时间语义可以加在 `BuffState` 之外的一层里。每一步都多证明了一个边界，而不是多几张同质卡；0.x 至此完成，进入 1.0 的门槛见文末完成定义（已逐条核对）。
>
> **不在 0.x 里的**：局外 Run（1.0）、壳（1.1）、地图（1.2）、奖励（1.3）、商店（候选池）；Luban、出包、程序集拆分（1.x 候选池 / 演示前清单 / 1.4）。

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
- ~~`ApplyBuffAction` 的 `source` 尚无消费者~~（0.5 已解决：`Execute` 先遍历施加方遗物里的 `IApplyBuffModifier`，蛇颅骨是第一个消费者）。
- ~~敌人只有一个固定伤害数字，没有格挡、没有行动表~~（0.6 已解决：`EnemyDefinition` 行动表固定循环、行动 = 效果列表走同一个 `ToAction`，敌人回合开始清敌人格挡）。
- ~~虚弱 / 易伤层数永不衰减；0 层 Buff 留在字典里、`HasBuff` 用 `> 0` 兜住（0.7）。~~（0.7 已解决：`DurationBuff` 轮末减 1、刚施加的跳过一次；`RemoveExpiredBuffs` 轮末清 0 层。`HasBuff` 的 `> 0` 保留——中毒在回合开始自减到 0 后要到轮末才出字典。）

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
| 卡面 | `CardDescriptionFormatter.Format(card, Func<int,int>)` 只把 Damage 行换成预览值、其余行不变，委托非空；无委托版本保留给 1.3 奖励页 |
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

## 0.5 遗物三钩子（已完成）

文档：[`0.5/游戏设计.md`](0.5/游戏设计.md) / [`0.5/技术设计.md`](0.5/技术设计.md) / [`0.5/TODO.md`](0.5/TODO.md)。

- 遗物列表挂在玩家 `CombatantState` 上、与 Buff 字典并列；三件遗物各占一种钩子：金刚杵（开战注入，只入队 `ApplyBuffAction`）、蛇颅骨（`ApplyBuffAction` 执行前改这一次施加的层数）、纸鹤（`WeakDamageRule` 读目标是否持有，倍率 0.75 → 0.6）。钩子形状与 `IBuffTrigger` 相同，`BattleSession` 只认 `RelicState` 与 `IBattleStartRelic`。
- 第一步先把 Buff Id 字面量收进 `BuffIds` 单独提交，再开始遗物；遗物 Id 同样只在 `RelicIds` 一处。

| 已有 | 说明 |
| --- | --- |
| 遗物容器 | `Core/Relics`：`RelicState`（只有 `Id`）、`RelicIds`、`RelicFactory`（Id → 实例的唯一转换，未知抛异常）；`CombatantState.Relics` / `AddRelic`（重复 Id / null 抛异常）/ `HasRelic`，与 Buff 字典互不出现 |
| 三种钩子 | `IBattleStartRelic.OnBattleStart(owner, queue)`：`BattleSession.StartBattle` 在第一个玩家回合前遍历玩家遗物触发，只入队 Action——金刚杵入队 `ApplyBuffAction(owner, owner, BuffFactory.Create(力量, 1))`。`IApplyBuffModifier.ModifyOutgoingBuff(source, target, buff)`：`ApplyBuffAction.Execute` 先遍历**施加方**遗物再 `BuffEffect.Apply`——蛇颅骨只给中毒 +1 层。无钩子：`WeakDamageRule` 按 `Target.HasRelic(纸鹤)` 取 0.75 / 0.6，是 Core 里唯一读具体遗物 Id 的地方 |
| Buff Id | `Core.Buffs.BuffIds` 单独提交；`Assets/Scripts/Runtime` 里四个字面量只剩这一处，测试与 `Assets/Data` 资产里的字面量有意保留 |
| 配置 | `RelicData`（id / displayName；校验 id 非空且 `RelicFactory.IsKnown`、displayName 非空）三份资产；`BattleConfig.relics` 允许为空，校验空引用 / 逐个 / Id 重复（同一资产引用两次也算）；`Default.asset` 配三件 |
| 表现 | `BattleController` 用 `ToRelicStates()` 构造 Session，`RelicDisplayName(id)` 从资产翻显示名（未知原样返回）；`BattleView` 遗物行 `遗物：金刚杵  蛇颅骨  纸鹤`，无遗物写 `遗物：无`；不设 `RelicDisplayNames` |
| 测试 | 203 项 EditMode（0.1～0.4 的 165 项不改断言；新增 38 项：`CombatantStateTests` 遗物列表 4 项，用测试内 `FakeRelic`；`RelicFactoryTests` 10 项；`BattleSessionTests.Relics.cs` 16 项——开战钩子用替身锁接线 3 项、构造 3 项、金刚杵 / 蛇颅骨 / 纸鹤各先预览再结算比 HP、三件同配 `Buffs` 里只有力量；`ActionQueueTests` 施加钩子 4 项——中毒 3 → 4、力量不变、施加方无遗物不变、只目标持有不生效；`DamageRuleTests` 纸鹤 4 项——6 → 3.6 否则 4.5、施加方持有不生效、层数无关、无虚弱不变）。五个 `BattleSessionTests*.cs` partial 挪进 `EditMode/BattleSession/` |

**证明的边界**：遗物不是 Buff；「改公式常数」与「改一次施加」分属 Rule 与施加钩子两层。三件遗物各自只是一个 C# 子类加一份 `RelicData`，`BattleSession` 里 grep 不到任何遗物 Id；删除任一件的类只影响它自己的用例。

## 0.6 敌人行动表（已完成）

文档：[`0.6/游戏设计.md`](0.6/游戏设计.md) / [`0.6/技术设计.md`](0.6/技术设计.md) / [`0.6/TODO.md`](0.6/TODO.md)。

- 敌人从「一个伤害数字」变成 `EnemyData`：血量 + 行动表，行动 = 效果列表（与卡牌共用 `EffectSpec`，意图文案由它推导），固定循环；敌人回合开始清敌人格挡。一只原版怪（颚虫）+ 把 0.1 的固定攻击敌人迁成第二份配置；玩家生命 40 → 80（改资产与 `BattleConfig.playerMaxHp` 默认值，测试保留 40）。
- 本版本的敌人不给玩家上虚弱 / 易伤——没有减层就上，那层永远不掉；配置层拒绝，0.7 放开。空行动 Core 允许（测试的无害敌人，显示 `待机`）、配置层拒绝（防漏配，配沉睡类敌人时放开）。

| 已有 | 说明 |
| --- | --- |
| 敌人定义 | `Core/Enemies`：`EnemyAction`（效果列表，构造时拷贝、拒绝 `Draw`、允许空 = 待机）、`EnemyDefinition`（Id / 显示名 / 血量 / 至少一条行动）；只引用 `Core.Cards`，没有名字或意图种类字段 |
| 敌人回合 | `BattleSession`：私有下标 + `CurrentEnemyAction`，行动结束立刻 `(i + 1) % Count`；流程 清敌人格挡 → 回合开始 Buff → 毒死则胜利（不执行、不推进）→ 当前行动逐条 `ToAction(effect, Enemy)` 一次 `RunAll` → 推进 → 判负。`ToAction` 一行未改，敌人只是它的第二个调用方；`BattleSetup` 只剩玩家三个标量；`PreviewEnemyAttack()` 签名不变，逐段过 `DamageCalculator` 求和 |
| 配置 | `EnemyActionData`（名字只给 Inspector；校验拒绝空效果 / `Draw` / 给玩家上 Buff）、`EnemyData`（SO）、`BattleConfig.enemy` 替代两个敌人标量，`playerMaxHp` 默认 80；`Assets/Data/Enemies/JawWorm.asset`（42；咬 11 → 猛击 7 + 格挡 5 → 咆哮 力量 3 + 格挡 6）与 `FixedAttacker.asset`（36；攻击 6）。顺带定下"必填字段不给能通过校验的默认值"（`maxHp` 无默认、`CardData.cost` 改为 -1） |
| 表现 | `BattleController.EnemyDisplayName` / `CurrentEnemyAction`（来自 Core 的 `EnemyDefinition`，不设字典）；`BattleView` 状态行 `颚虫 HP 42/42  格挡 0`；`IntentFormatter.Format(EnemyAction, int)` 从效果列表推导 `攻击 N` / `防御` / `增益` / `减益` / `待机`，首次出现顺序去重、` · ` 连接；Core 无意图枚举 |
| 测试 | 246 项 EditMode（0.1～0.5 的 203 项只有 1 项改断言：敌人回合开始先清格挡，中毒用例的敌人格挡 10 → 0；新增 43 项：`EnemyDefinitionTests` 14；`BattleSessionTests.Enemies.cs` 12——循环回绕、颚虫三招各自预览 = HP 差、猛击格挡顶住玩家攻击、咆哮后 14 与第二圈 10、虚弱 8、双段 4 + 4 = 8、空行动、毒死不推进指针、玩家的施加修改器不影响敌人自 Buff；`EnemyActionDataTests` 7；`IntentFormatterTests` 10） |

**证明的边界**：敌人行动与卡牌走同一套效果原语；新敌人 = 配数据。颚虫与固定攻击敌人各自只是一份 `EnemyData`，`BattleSession` 里 grep 不到任何敌人 Id，`ToAction` / `DamageCalculator` / 三条 Rule 的 diff 为空。

## 0.7 回合型 Buff（已完成）

文档：[`0.7/游戏设计.md`](0.7/游戏设计.md) / [`0.7/技术设计.md`](0.7/技术设计.md) / [`0.7/TODO.md`](0.7/TODO.md)。

- 虚弱 / 易伤的层数是「剩余轮数」：轮结束统一减 1，刚施加的不减，0 层移除。
- 加入蓝奴隶贩子（刺击 12；耙：7 伤 + 给玩家 1 层虚弱）当刚施加保护的消费者——只是一份 `EnemyData` 加删一条配置校验。

| 已有 | 说明 |
| --- | --- |
| 轮末触发 | `Core/Buffs`：`IRoundEndTrigger.OnRoundEnd()`（无参，减层没有对外效果）；`DurationBuff : BuffState, IRoundEndTrigger` 是虚弱 / 易伤的中间基类，`_justApplied` 为真时跳过第一次减层并清标记，否则 `RemoveStacks(1)`；`BuffState` 不加字段，力量 / 中毒不实现它 |
| 保护标记 | `BuffFactory.Create(id, stacks, justApplied = false)` 只对虚弱 / 易伤传下去；`BattleSession.ToAction` 的 `ApplyBuff` 分支传 `justApplied: source == Enemy`——只看施加方（一代 `isSourceMonster`），不看目标是谁、不看所有者本轮是否已行动。叠加走 `ApplyBuff` 的 `AddStacks`，带保护的新实例被丢弃，保护不随叠加刷新 |
| 轮结束 | `TryEndPlayerTurn`：推进指针 → 判负 → 未失败则 `EndRound()`（双方 `TriggerRoundEnd`：`foreach` + `is IRoundEndTrigger`；再双方 `CombatantState.RemoveExpiredBuffs()`：先收集 0 层 key 再删）→ `StartPlayerTurn`。与开战遗物、回合开始 Buff 并列的第三个固定时机遍历，不抽公共方法、不合并成事件；中毒在回合开始自减到 0 的也在这里清 |
| 配置 | `EnemyActionData.TryValidate` 删掉"ApplyBuff 且 Opponent"一条，空 `effects` 与 `Draw` 仍拒；`Assets/Data/Enemies/BlueSlaver.asset`（`blue_slaver` / 48 血；刺击 12 → 耙 7 + weak 1）；`Default.asset` 保持颚虫，换敌人只改引用 |
| 表现 | 不改代码。`BattleView` 的 0 层过滤保留（回合开始自减到 0 的中毒要到轮末才出字典）；意图 `攻击 7 · 减益` 是 0.6 的 `IntentFormatter` 现成的 |
| 测试 | 263 项 EditMode：0.1～0.6 的 246 项不改断言；新增 17 项——`BuffTests` 4、`BuffFactoryTests` 2、`CombatantStateTests` 2、`BattleSessionTests.RoundEnd.cs` 9（痛击 2 层覆盖本回合与下回合再移除、玩家上的虚弱轮末消失、耙后保护与下一回合 ×0.75、`[耙, 耙, 待机, 待机]` 层数 1 / 1 / 0、敌人给自己上易伤轮末不减——锁住判定看施加方而不是"目标是玩家"、中毒不轮末减、中毒 0 层轮末移除、玩家自带虚弱无保护、失败时不轮结束）；`EnemyActionDataTests` 的 ApplyBuff Opponent 用例由拒改通过 |

**证明的边界**：回合型 Buff 的衰减时机与中毒不同（轮末 vs 回合开始结算时），敌人给玩家上的 Debuff 能活到玩家下一回合。时间语义加在 `DurationBuff` 一层，`BuffState.cs` / `ApplyBuffAction.cs` / 三条 Rule 的 diff 为空，蓝奴隶贩子零 C#。

## 0.x 完成定义（进入 1.0 的门槛）

0.7 完成时逐条核对：

- [x] 新增一张卡、一只敌人、一件用现有钩子的遗物，各自最多新增一个 C# 子类，不在 `BattleSession` 里新增按卡牌 / 敌人 / 遗物 Id 或种类的分支。新的效果原语或新的钩子时机仍要改一处 `switch` 或一处遍历——那是有意的封闭点。（卡：0.2 起一张卡 = 一份 `CardData`；敌人：0.6 颚虫、0.7 蓝奴隶贩子都只是 `EnemyData`；遗物：0.5 每件一个子类 + 一份 `RelicData`。`BattleSession` 里 grep 不到卡牌 / 敌人 / 遗物 Id。）
- [x] 四个牌区齐全，能力牌打出后本场不再出现。（0.4：`CardPile` 抽牌 / 手牌 / 弃牌 / 消耗四区，`ExhaustsWhenPlayed`。）
- [x] 三条伤害规则在界面可见，预览数值与结算一致。（0.3：`PreviewPlayerAttack` / `PreviewEnemyAttack` 与结算共用 `DamageCalculator`，`Preview.cs` 用例锁住。）
- [x] 遗物三种钩子各有一件标本，且没有任何遗物进入 Buff 字典。（0.5：金刚杵 / 蛇颅骨 / 纸鹤；`Relics` 列表与 `Buffs` 字典并列。）
- [x] 至少两只配置不同的敌人，意图循环，公式后预览。（0.6 颚虫、固定攻击敌人；0.7 蓝奴隶贩子，共三份 `EnemyData`。）
- [x] 虚弱 / 易伤会随轮结束衰减，刚施加的不被立刻减掉；0 层 Buff 不残留。（0.7：`DurationBuff` / `EndRound` / `RemoveExpiredBuffs`。）
- [x] 以上全部有 EditMode 测试；0.1 的 79 项测试不改断言仍通过（因接口改动而改构造方式除外）。唯一的例外记录在案：0.6 起敌人回合开始先清敌人格挡，中毒用例 `PoisonedEnemy_…` 里"敌人格挡仍是 10"的断言改为 0——规则本身变了才允许改断言，且要在当版本 TODO 与技术设计里写明。（0.7 完成时 263 项全部通过；0.7 没有新的例外。）

全部满足：局内已经能支撑「三场不同的战斗 + 战后选卡 + 遗物栏」，1.0 `RunState` 已完成。下一刀是 1.1 Run 壳与战斗接入；短 Run 完成定义在 1.3，见 [`../Ver 1.x/游戏设计.md`](../Ver%201.x/游戏设计.md)。
