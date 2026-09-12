# CardRPGFramework

使用 Unity 与 C# 开发的**简化版《杀戮尖塔》求职 Demo**，重点展示数据驱动、战斗规则与客户端工程化能力，不追求商业级内容量。

## 当前阶段

**0.1（Phase 1 + Phase 2a/2b/2c）、0.2（效果列表）、0.3（可见规则）、0.4（消耗堆与能力牌）、0.5（遗物三钩子）与 0.6（敌人行动表）已完成，下一步是 0.7（回合型 Buff）。** 版本路线与各版本文档见 [`Docs/开发计划/README.md`](Docs/开发计划/README.md)：0.x 继续补齐局内卡牌战斗（回合型 Buff），1.0 做第一次完整的短 Run，1.1 之后才做出包等交付工作。

Phase 1 已完成：可以在 `TestScene` 的 Play 模式里从战斗开始一路操作到胜利或失败，Core 战斗规则、ScriptableObject 配置、Unity 表现层全部接通。

Phase 2（ActionSystem/EffectSystem/BuffSystem/RuleSystem）拆分成 [Phase 2a](Docs/开发计划/Ver%200.x/0.1/Phase%202a/游戏设计.md)、[Phase 2b](Docs/开发计划/Ver%200.x/0.1/Phase%202b/游戏设计.md)、[Phase 2c](Docs/开发计划/Ver%200.x/0.1/Phase%202c/游戏设计.md) 三个独立可验收阶段，已全部完成。

**Phase 2a（ActionSystem + EffectSystem）已完成**：卡牌效果的结算方式从 `BattleSession` 直接调用 `CombatantState` 改为"生成 Action 入队、Action 执行时调用 Effect"，玩家可见行为与 Phase 1 完全一致（纯架构重构，未新增卡牌/数值）。

**Phase 2b（BuffSystem）已完成**：新增 Buff 容器（`Core/Buffs`）与两个具体 Buff——力量（纯数值，无触发，效果留给 Phase 2c 的 RuleSystem 消费）、中毒（回合开始按层数造成不受格挡影响的 HP Loss，随后层数减 1）；牌组新增「力量强化」「剧毒」两张卡，扩充为 12 张。

**Phase 2c（RuleSystem）已完成，Phase 2（2a+2b+2c）全部完成**：新增 `Core/Rules`——伤害规则是纯函数（只读 Buff、只写 `DamageContext.Damage`），固定两段管线（力量加法在前，虚弱/易伤乘法在后），最后统一 `Math.Floor` 一次。`DamageAction` 新增 `source`，玩家出牌与敌人固定攻击统一经过同一套 `DamageCalculator`；牌组新增「虚弱」「易伤」两张卡（目标都是敌人，虚弱降低的是敌人自己的攻击），扩充为 14 张。

**0.2（效果列表）已完成**：一张牌从"类型 + 一个数值"改为"类型（攻击 / 技能）+ 费用 + 效果列表"。`Core/Cards` 新增封闭的五种效果原语 `EffectKind`（伤害 / 格挡 / 治疗 / 施加 Buff / 抽牌）与只经静态工厂构造的 `readonly struct EffectSpec`；`BattleSession` 删掉按卡牌种类分支的 `EnqueueCardAction`，改为私有 `ToAction` 逐条把 `EffectSpec` 变成 Action，唯一的 `switch` 穷举的是效果原语而不是卡牌。打出时序改为原版的"先离手 → 结算 → 再进弃牌"（`CardPile.PlayCard` 拆成 `TakeFromHand` / `AddToDiscard`），抽牌类效果结算时打出的牌不在任何牌区，重洗不会把它洗回去。卡面描述由 `CardDescriptionFormatter` 从效果列表生成。新增痛击（复合）、双击（同效果两段、各自取整）、剑柄打击（打出时抽牌）三张卡，全部只是配数据；牌组扩充为 17 张 / 10 种。

**0.3（可见规则）已完成**：0.1 的伤害规则第一次在 Play 模式里看得见。`BattleSession` 暴露 `PreviewPlayerAttack(int)` / `PreviewEnemyAttack()` 两个只读预览，与 `DamageAction.Execute` 走同一个 `DamageContext` / `DamageCalculator`，返回**格挡前**的最终伤害、不改任何状态；`BattleController` 原样转发。`CardDescriptionFormatter` 新增 `Format(card, Func<int,int>)` 重载，只把 Damage 行的数字换成预览值、其余行不变（无委托版本保留给 1.0 奖励页）。`BattleView` 新增双方 Buff 行（`力量 2  中毒 3`，0 层过滤不显示、不改数据）、意图改为公式后数值、卡面改用预览重载；`Views` 里不出现任何公式常数。不改任何结算路径与规则。

**0.4（消耗堆与能力牌）已完成**：第四个牌区。`CardType` 追加 `Power`（能力），`CardDefinition` 增加消耗关键词 `bool Exhaust`（构造函数末尾可选参数，默认 false）与 `ExhaustsWhenPlayed => Exhaust || Type == CardType.Power`——打出后去哪是牌自己的规则：类型给默认去向，关键词覆盖。`CardPile` 增加只进不出的消耗堆（`AddToExhaust` / `ExhaustPileCount`，不公开列表），`ReshuffleDiscardIntoDrawPile` 与 `DiscardHand` 一字未改：重洗只洗弃牌堆，留在手里的消耗牌回合结束仍进弃牌堆，"消耗"只在打出时生效（原版规则）。`BattleSession.TryPlayCard` 结算后按 `ExhaustsWhenPlayed` 分流落堆，Session 里不出现 `CardType.Power` 或 `.Exhaust`。力量强化改为能力牌，新增坚不可摧（2 费技能、格挡 30、消耗），两者都只是配数据，牌组 18 张 / 11 种。界面新增牌堆数量行（`抽牌 X  弃牌 Y  消耗 Z`），卡面费用行带类型（`能力 · 费用 1`），勾消耗的牌描述末行印 `消耗`、能力牌不印。

**0.5（遗物三钩子）已完成**：遗物不是 Buff。新增 `Core/Relics`：`RelicState` 只有 `Id`（跨战斗持有、无层数、无衰减），挂在玩家 `CombatantState` 独立的 `Relics` 列表里、与 Buff 字典并列，Buff 字典里不会出现遗物；`RelicFactory` 是遗物 Id → 实例的唯一转换点。三件遗物各占一种钩子，钩子形状与 `IBuffTrigger` 相同：金刚杵实现 `IBattleStartRelic`，`BattleSession.StartBattle` 在第一个玩家回合前遍历玩家遗物触发它，钩子里只入队 `ApplyBuffAction`（`BuffFactory.Create` 力量 1）、不直接改状态；蛇颅骨实现 `IApplyBuffModifier`，`ApplyBuffAction.Execute` 先遍历**施加方**遗物里的修改器再交给 `BuffEffect`，只给中毒这一次施加 +1 层（0.1 留下的 `source` 参数至此有了消费者）；纸鹤没有钩子，`WeakDamageRule` 按目标是否持有切换 0.75 / 0.6 两个常数——改公式常数的遗物改对应 Rule 一处，`BattleSession` 里 grep 不到任何遗物 Id。开工前先把四个 Buff Id 字面量收进 `Core.Buffs.BuffIds` 单独提交（`Assets/Scripts/Runtime` 里字面量只剩这一处）。配置：`RelicData`（id / displayName）三份资产，`BattleConfig.relics` 列表允许为空、同一遗物引用两次报错，显示名由 `BattleController.RelicDisplayName` 从资产翻译；界面新增遗物行 `遗物：金刚杵  蛇颅骨  纸鹤`（无遗物写 `遗物：无`），Buff 行里没有遗物。

**0.6（敌人行动表）已完成**：敌人从"一个伤害数字"变成配置。新增 `Core/Enemies`：`EnemyAction` = 效果列表（与卡牌共用 `EffectSpec`，构造时拒绝 `Draw`、允许空列表 = 待机），`EnemyDefinition` = Id + 显示名 + 血量 + 至少一条行动，只引用 `Core.Cards`、和 `CardDefinition` 一样只描述不执行。`BattleSession` 用一个私有下标按行动表固定循环（`CurrentEnemyAction`，行动结束立刻 `(i + 1) % Count`），敌人回合改为"清敌人格挡 → 回合开始 Buff（中毒）→ 毒死则胜利、不执行行动也不推进指针 → 当前行动的效果逐条 `ToAction(effect, Enemy)` 入队、一次 `RunAll` → 推进指针 → 判负"。`ToAction` 一行未改，敌人行动只是它的第二个调用方：`source` 传 `Enemy`，`Self` 就是敌人自己、`Opponent` 是玩家；`ApplyBuffAction` 查的是施加方的遗物，所以颚虫咆哮给自己的力量不吃玩家的蛇颅骨。`PreviewEnemyAttack()` 签名不变，改为逐段过 `DamageCalculator` 求和（每段独立取整，与双击一致）；`BattleSetup` 只剩玩家三个标量。内容：一只原版怪颚虫（42 血；咬 11 → 猛击 7 + 格挡 5 → 咆哮 力量 3 + 格挡 6，循环）+ 把 0.1 的固定攻击敌人迁成第二份配置；玩家生命 40 → 80（测试仍用 40）。配置：`EnemyActionData`（名字只给 Inspector 看；校验拒绝空效果、`Draw`、给玩家上 Buff——最后一条是 0.6 的范围限制，0.7 放开）、`EnemyData`（SO）、`BattleConfig.enemy` 替代两个敌人标量；顺带定下"必填字段不给能通过校验的默认值"（`EnemyData.maxHp` 无默认、`CardData.cost` 默认 -1）。界面：状态栏带敌人名（`颚虫 HP 42/42  格挡 0`），意图由 `IntentFormatter` 从效果列表推导（`攻击 11` / `攻击 7 · 防御` / `增益 · 防御` / `减益` / `待机`，攻击数字是 Core 预览的总和，按首次出现顺序去重），Core 里没有意图枚举。0.1～0.5 的用例只改了 1 项断言：敌人回合开始先清敌人格挡，中毒用例里敌人格挡从 10 变 0。

对照历史计划 [`MVP计划与迭代计划.MD`](Docs/开发计划/Ver%200.x/0.1/MVP计划与迭代计划.MD) 的完成标准：战斗可玩、卡牌数量（当前 18 张）、5 种效果（伤害/格挡/治疗/HP Loss/Buff）、规则系统、简单 AI（0.6 的固定循环行动表，不做随机）均已达成；Buff 目前 4 种；配置驱动（Luban）不再按该文档的 Phase 3 推进，进 1.2+ 候选池，见 [`Docs/开发计划/README.md`](Docs/开发计划/README.md)。

**已完成**

- `BattlePhase`：战斗阶段枚举（`NotStarted` / `PlayerTurn` / `EnemyTurn` / `Victory` / `Defeat`）。
- `CombatantState`：生命值、格挡、治疗与边界。
- `CardType` / `CardDefinition` / `EffectKind` / `EffectTarget` / `EffectSpec`：卡牌 = 类型（攻击 / 技能 / 能力）+ 费用 + 效果列表（至少一条，构造时拷贝）+ 消耗关键词（`bool Exhaust`，默认 false）。`ExhaustsWhenPlayed`（能力牌或勾消耗）决定打出后去消耗堆还是弃牌堆，是牌自己的规则，Session 只问结果。`EffectSpec` 是"做什么、对谁、多少"的 `readonly struct`，只经 `Damage / Block / Heal / ApplyBuff / Draw` 五个静态工厂构造并校验；目标用 `Self / Opponent` 而不是玩家 / 敌人，为 0.6 敌人行动复用。`Core.Cards` 不引用 `Actions` / `Buffs`，Buff 在这里只是字符串 Id。
- `CardPile`：抽牌堆、手牌、弃牌堆、消耗堆四个牌区；抽空重洗只洗弃牌堆，消耗堆只进不出、不公开列表；两堆皆空时安全停止。打出拆成 `TakeFromHand`（离手，此刻不在任何牌区）与 `AddToDiscard` / `AddToExhaust`（结算完再落堆，去向由调用方决定）两步，不提供组合方法；`DiscardHand` 不看关键词。
- `BattleSetup` / `BattleSession`：`BattleSetup` 只装玩家三个标量（生命 / 每回合能量 / 手牌数），敌人由构造函数第二个参数 `EnemyDefinition` 给。`BattleSession` 负责战斗初始化、玩家回合循环（清格挡、恢复能量、抽牌）、卡牌使用校验与能量消耗、按效果列表逐条 `ToAction` 入队结算（唯一的 `switch` 穷举 `EffectKind`，没有按卡牌 Id 或种类的分支）、结算后按 `ExhaustsWhenPlayed` 落弃牌堆或消耗堆、结束回合与敌人行动、胜负判定与结束后拒绝操作；转发三个牌堆数量。敌人回合按行动表固定循环：私有下标 + `CurrentEnemyAction`，流程是清敌人格挡 → 回合开始 Buff → 毒死则胜利（不执行、不推进）→ 当前行动逐条 `ToAction(effect, Enemy)` 一次 `RunAll` → 推进指针 → 判负；Session 里没有任何敌人 Id 或按敌人的分支。构造函数末尾可选 `relics` 参数只落到玩家身上；`StartBattle` 在第一个玩家回合前遍历玩家遗物里的 `IBattleStartRelic` 并结算其入队的 Action（与 `TriggerTurnStartBuffs` 同形），Session 只认 `RelicState` 与 `IBattleStartRelic`、不认任何遗物 Id。另暴露 `PreviewPlayerAttack(int)` / `PreviewEnemyAttack()` 两个只读预览：新建 `DamageContext` 交给 `DamageCalculator`，返回格挡前的最终伤害，`(基础值, source, target)` 配对与结算处相同，不改状态；敌人预览把当前行动的每个 Damage 段各自过一遍再求和。
- `EnemyAction` / `EnemyDefinition`（`Core/Enemies`）：一条行动 = 效果列表（构造时拷贝，拒绝 `Draw`，允许空列表 = 待机，眩晕不是空行动）；敌人定义 = Id + 显示名 + 血量 + 至少一条行动（构造时拷贝列表）。只引用 `Core.Cards`，不引用 `Actions` / `UnityEngine`；没有名字、没有意图种类字段，意图由效果推导。
- `IAction` / `ActionContext` / `ActionQueue`：贯穿整场战斗的 FIFO 行动队列，`RunAll` 执行期间可以继续入队新 Action。
- `DamageAction` / `BlockAction` / `HealAction` / `DrawCardsAction`：与伤害 / 格挡 / 治疗 / 抽牌四种效果原语对应，前三者 `Execute` 时调用对应 Effect；`DrawCardsAction` 直接持有 `CardPile` 调 `Draw`，不设 `DrawEffect`（Effect 只改 `CombatantState`，抽牌改的是牌堆）。
- `DamageEffect` / `BlockEffect` / `HealEffect` / `HpLossEffect` / `BuffEffect`：转发到 `CombatantState` 现有方法的纯状态修改层，不做任何数值计算。
- `BuffState` / `IBuffTrigger` / `StrengthBuff` / `PoisonBuff` / `WeakBuff` / `VulnerableBuff` / `BuffFactory` / `BuffIds`：Buff 容器基础设施。触发行为下沉到具体子类自己实现（`PoisonBuff` 自己实现 `IBuffTrigger`），不做外部按 Id 扫描匹配的触发器；虚弱/易伤和力量一样是纯数值、无触发，只是被 Rule 读取。`BuffFactory` 是 Buff Id → 实例的唯一转换点（`switch` 穷举，不做注册表），供 `EffectSpec.ApplyBuff` 的字符串 Id 落地与 Data 层校验。`BuffIds` 是四个 Buff Id 字面量的唯一出处，Buff 子类的 `Id`、`BuffFactory`、三条 Rule、`BuffDisplayNames` 与遗物都对着它（测试文件与 `Assets/Data` 资产里的字面量有意保留，正好锁住常量值）。
- `RelicState` / `RelicIds` / `IBattleStartRelic` / `IApplyBuffModifier` / `VajraRelic` / `SneckoSkullRelic` / `PaperKraneRelic` / `RelicFactory`：遗物基础设施（`Core/Relics`）。遗物只有 `Id`，与 Buff 分开存放；钩子形状与 `IBuffTrigger` 相同——`IBattleStartRelic.OnBattleStart(owner, queue)` 只入队 Action，`IApplyBuffModifier.ModifyOutgoingBuff(source, target, buff)` 直接改这一次施加的 `BuffState`；`PaperKraneRelic` 不实现任何接口，只被 `WeakDamageRule` 读"是否持有"。`RelicFactory` 是遗物 Id → 实例的唯一转换点（`switch` 穷举，未知 Id 抛异常），供 `RelicData` 校验与落地。
- `ApplyBuffAction` / `HpLossAction`：`ApplyBuffAction` 把 Buff 施加到目标，施加前先遍历**施加方**遗物里的 `IApplyBuffModifier`（蛇颅骨）改这一次施加的 `BuffState`，再交给 `BuffEffect`；`HpLossAction` 独立于 `DamageAction` 存在（不受格挡影响），当前唯一使用者是中毒。
- `CombatantState` 新增 Buff 容器（`ApplyBuff`/`GetBuffStacks`/`HasBuff`/`Buffs`）、遗物列表（`Relics`/`AddRelic`/`HasRelic`，重复 Id 抛 `ArgumentException`、null 抛 `ArgumentNullException`，与 Buff 字典并列、互不出现）与 `LoseHp`（忽略格挡，且是生命值减少的唯一入口，`TakeDamage` 内部扣完格挡后复用它）。
- `DamageContext` / `IDamageRule` / `StrengthDamageRule` / `WeakDamageRule` / `VulnerableDamageRule` / `DamageCalculator`：伤害规则纯函数，固定两段管线（加法先于乘法），不做规则注册引擎；虚弱/易伤按"有无"而非层数生效。`DamageAction` 新增 `source`，`Execute` 内部先经 `DamageCalculator` 修正再交给 `DamageEffect`，玩家出牌与敌人固定攻击统一走这一条路径。`WeakDamageRule` 的倍率按目标是否持有纸鹤取 0.75 / 0.6，是 Core 里唯一读具体遗物 Id 的地方——改公式常数的遗物就该改对应 Rule 一处。
- `CardData` / `EffectSpecData` / `RelicData` / `EnemyData` / `EnemyActionData` / `BattleConfig`：ScriptableObject 配置，转换为 `CardDefinition`/`RelicState`/`EnemyDefinition`/`BattleSetup`，带启动期校验（空引用、非法数值、空 ID、重复 ID、越界的 `CardType`、效果列表非空且每条合法：`value > 0`、`buffId` 已知、kind 与 target 一致）。`CardData` 有 `exhaust` 勾选框：新增一张消耗牌 = 勾上它，新增一张能力牌 = 类型选能力，代码不改。`EffectSpecData` 是 `EffectSpec` 的可序列化影子，`ToSpec` 复用 Core 的静态工厂。`RelicData` 只有 id / displayName（校验 id 非空且 `RelicFactory.IsKnown`、displayName 非空），行为全在 `Core/Relics` 的类里。`EnemyData`（id / displayName / maxHp / 行动列表）经 `ToDefinition` 变成 `EnemyDefinition`；`EnemyActionData` 是 `EnemyAction` 的可序列化影子（`name` 只给 Inspector 列表看，不进 Core），校验先逐条过 `EffectSpecData`，再拒绝空效果（没有沉睡类敌人前防漏配）、`Draw`（敌人没有牌堆）与"给玩家上 Buff"（0.6 没有减层，0.7 放开），错误信息带 `actions[i]` 与行动名。新增一只敌人 = 一份 `EnemyData` + 在 `BattleConfig.enemy` 里换引用，零 C#。`BattleConfig.relics` 允许为空，校验空引用、逐个校验与 Id 重复——同一资产引用两次也算，遗物唯一，与牌组允许重复引用不同。必填字段不给能通过校验的默认值（`maxHp` 无默认、`cost` 默认 -1），漏填在启动校验就报出来，而不是带着"看起来合理"的数字开战。
- `BattleController`：场景里显式引用 `BattleConfig`，创建并持有 `BattleSession`（用 `ToEnemyDefinition()` / `ToRelicStates()` 传入敌人与遗物栏），转发使用手牌/结束回合命令、两个只读预览与三个牌堆数量，暴露只读战斗状态；`EnemyDisplayName` / `CurrentEnemyAction` 直接来自 Core 的 `EnemyDefinition`（和 `CardDefinition.DisplayName` 一样是内容数据）；`RelicDisplayName(id)` 把遗物 Id 翻成 `RelicData` 里的显示名（未知原样返回），View 不认识 Data 层。
- `BattleView` / `CardButtonView` / `EndScreenView` / `CardDescriptionFormatter` / `IntentFormatter` / `BuffDisplayNames`：最小战斗 UI，显示双方状态（敌人状态行带名字：`颚虫 HP 42/42  格挡 0`）与 Buff 行（`名称 层数`，多条用两个空格连接，0 层不显示）、遗物行（`遗物：金刚杵  蛇颅骨  纸鹤`，无遗物写 `遗物：无`，Id 读 `Player.Relics`、名字问 Controller，Buff 行里不出现遗物）、能量、牌堆数量行（`抽牌 X  弃牌 Y  消耗 Z`）、回合、敌人意图、手牌与最近操作结果，胜利/失败时显示遮罩并阻止继续操作。意图文案由纯函数 `IntentFormatter.Format(EnemyAction, int)` 从当前行动的效果列表推导：Damage 段合成一项 `攻击 N`（N 是 Controller 转发的 Core 预览总和），`Block` → `防御`，`ApplyBuff(Self)` / `Heal` → `增益`，`ApplyBuff(Opponent)` → `减益`，按首次出现顺序去重、` · ` 连接，空行动 → `待机`；与卡面描述同一条路，Core 里没有意图枚举。卡面为 `名字 / 类型 · 费用 X / 描述`，类型中文（攻击 / 技能 / 能力）是 `BattleView` 里一个私有 `switch`；描述由 `CardDescriptionFormatter` 从效果列表逐条生成，勾消耗的牌末行追加 `消耗`（能力牌不印，类型标签已说明去向）：无委托版本只用基础数值（1.0 奖励页用），带 `Func<int,int>` 委托的重载只把 Damage 行换成 Controller 转发的预览值、其余行不变；两者都只收 `CardDefinition`、不依赖 Controller。View 不复算任何公式常数。Buff 中文名只在 `BuffDisplayNames` 维护一份。
- EditMode 测试覆盖伤害/格挡/治疗/生命值损失边界、Buff 容器与中毒触发链路、`BuffFactory` 四个 Id 与未知 Id、伤害规则单独验证与组合取整时机、抽牌/弃牌/重洗与固定种子洗牌、`TakeFromHand` 后立刻重洗不含该牌、`ActionQueue` 的执行顺序与重入入队、Effect 转发、`EffectSpec` 构造校验、`CardDefinition` 效果列表拷贝与空列表拒绝、`EffectSpecData` 校验（未知 buffId / value ≤ 0 / Damage + Self）、卡面文案、`BattleSession` 的合法流程与关键拒绝路径（含中毒致死立即胜利、跳过敌人攻击；敌人固定攻击迁移到 `DamageAction` 管线后无 Buff 时数值与 Phase 1 一致），抽牌堆耗尽后经过完整战斗流程仍能正确重洗弃牌堆，三张锚点卡（痛击的 8 点不吃自己刚施加的易伤、双击对易伤敌人 14 而非 15、剑柄打击手牌数不变且重洗不含刚打出的那张），预览 = 结算（先预览再打出 / 结束回合，HP 差值等于预览值：易伤 9、2 层力量 + 易伤 12、敌人虚弱 4；痛击预览不含自己即将施加的易伤；连续预览不改状态）与卡面预览重载（双击两段各换、痛击的易伤行不变、Block 行不换、null 委托抛异常），以及消耗堆（`CardDefinition` 三种去向与默认 false；`CardPile` 的 `AddToExhaust` / null 抛异常 / 重洗只回收弃牌堆 / 两堆皆空不取消耗堆 / `DiscardHand` 不看关键词；`BattleSession` 打出能力牌后连续三回合重洗都抽不到它、四区之和恒等于牌组张数、坚不可摧格挡 30 进消耗堆而留手进弃牌堆、0.1 的 `Strength()` 仍是技能进弃牌堆；卡面 `消耗` 行：勾消耗的技能印、能力牌不印、预览重载同样追加），以及遗物（`CombatantState` 遗物列表 4 项，用测试内 `FakeRelic` 不依赖三件遗物；`RelicFactory` 三个 Id 各返回正确类型 / `IsKnown` 三真三假 / 未知抛异常 10 项；`BattleSessionTests.Relics.cs` 16 项——开战钩子先用替身锁接线：只触发一次、队列被结算、重复 `StartBattle` 不再触发、无钩子的遗物被忽略；金刚杵开战力量 1 且 `PreviewPlayerAttack(6) == 7`、打出后 HP 差 7；蛇颅骨打出剧毒 4 层、只配蛇颅骨时力量强化仍 2 层；纸鹤下 `PreviewEnemyAttack() == 3`、结束回合掉 3、玩家自带虚弱时 `PreviewPlayerAttack(6) == 4`；三件同配 `Buffs` 里只有力量且没有任何遗物 Id；`ActionQueueTests` 施加方持蛇颅骨中毒 3 → 4、力量不变、施加方无遗物不变、只目标持有不生效 4 项；`DamageRuleTests` 目标持纸鹤 6 → 3.6 否则 4.5、施加方持有不生效、层数无关、无虚弱不变 4 项），以及敌人行动表（`EnemyDefinitionTests` 14 项：`EnemyAction` null / 含 Draw / 含 default / 空允许 / 拷贝，`EnemyDefinition` 的 id / displayName / maxHp / actions 校验与拷贝保留元素引用；`BattleSessionTests.Enemies.cs` 12 项——颚虫 42 血、循环回绕 `[0]→[1]→[2]→[0]→[1]`、咬 11 / 猛击 7 + 格挡 5 / 咆哮后格挡 6 不是 11 各自预览 = HP 差、猛击的格挡顶住玩家 6 点攻击只掉 1、咆哮后咬 14 与第二圈猛击 10、2 层虚弱咬 8、双段 3 + 3 对易伤玩家 4 + 4 = 8 不是 9、空行动预览 0 且 HP 不变、99 层中毒毒死后 Victory 且指针仍在 `Actions[0]`、玩家持"任何 Buff +1"替身时颚虫自己的力量仍是 3；`EnemyActionDataTests` 7 项：Damage + Block / ApplyBuff Self 通过，空 / Draw / ApplyBuff Opponent 拒绝，内层错误带下标透传，`ToAction` 保序；`IntentFormatterTests` 10 项：单攻击、攻击 + 防御、增益 + 防御、攻击 + 减益、双段合一、治疗归增益、双格挡去重、空 → 待机、首次出现顺序、null 抛异常）。

**范围外（后续版本或候选池，未实现）**

- 回合型 Buff 衰减与 0 层清理、敌人给玩家上 Debuff（0.7，蓝奴隶贩子）；局外短 Run（1.0）。
- 敌人的随机行动 / "不能连用同一招"（需要第二个随机源，1.0 之后随 Run 种子考虑）、敌人回合的分段动画 / 异步、Core 里的意图枚举或行动名字字段、沉睡 / 眩晕等意图变体（配置层暂不允许空行动，眩晕是"行动存在、执行前作废"，属于 Session 流程层）、第三只敌人。
- 更多遗物钩子：回合开始 / 结束钩子、施加钩子取消一次施加（人工制品）、`OnBattleStart` 带对手参数（弹珠袋）、`LoseHp` 前的挂钩（钨条）、第二件改公式常数的遗物（纸蛙，到 `VulnerableDamageRule` 加同样一行即可）、遗物图标 / `RelicDisplayNames`；遗物栏 1.0 起改由 Run 提供。
- 第二个关键词（虚无 / 固有 / 保留，届时 `bool Exhaust` 才换成 `[Flags]`）、"消耗手牌中一张牌"类效果（被效果消耗是另一个时机）、消耗触发（"每当消耗一张牌"）、消耗堆内容列表（只显示数量）、类型中文抽成 `CardTypeDisplayNames`（1.0 奖励页出现第二个调用方再抽）。
- 卡面力量快照（`6 (+2)`）、Buff / 意图图标、事件驱动的界面刷新、多敌人的预览目标参数。
- Attribute / Event 等独立框架；规则集合硬编码在 `DamageCalculator` 内部，不做注册/配置化；效果原语是封闭枚举，不做可注册的效果插件。
- 可组合 Action（`CompositeAction`/`ConditionalAction` 等）、条件效果、随机目标、X 费、多敌人、目标选择、手牌上限。
- 暴击、闪避、护甲穿透等规则；无实体/鸟居/钨条等更靠后的伤害挂钩点（见 [`Docs/Phase 2 扩展边界批注.md`](Docs/Phase%202%20扩展边界批注.md)）。
- 卡牌升级、稀有度、装备、商店/事件节点、存档、战斗回放。
- 配置驱动（Luban/Excel）、出包与程序集拆分（1.1+）、正式美术、动画、音效。

## 文档

- **开发计划（按版本）**：[`Docs/开发计划/README.md`](Docs/开发计划/README.md) —— 版本约定、路线总览、[0.x 大纲](Docs/开发计划/Ver%200.x/README.md)、[1.x 大纲](Docs/开发计划/Ver%201.x/README.md)
- 下一版本 0.7 回合型 Buff：[游戏设计](Docs/开发计划/Ver%200.x/0.7/游戏设计.md) / [技术设计](Docs/开发计划/Ver%200.x/0.7/技术设计.md)（草稿）/ [TODO](Docs/开发计划/Ver%200.x/0.7/TODO.md)（草稿）——进入 0.7 时先按其第 0 节对照 0.6 实际实现复核转正
- 0.6（已完成）敌人行动表：[游戏设计](Docs/开发计划/Ver%200.x/0.6/游戏设计.md) / [技术设计](Docs/开发计划/Ver%200.x/0.6/技术设计.md) / [TODO](Docs/开发计划/Ver%200.x/0.6/TODO.md)
- 0.5（已完成）遗物三钩子：[游戏设计](Docs/开发计划/Ver%200.x/0.5/游戏设计.md) / [技术设计](Docs/开发计划/Ver%200.x/0.5/技术设计.md) / [TODO](Docs/开发计划/Ver%200.x/0.5/TODO.md)
- 0.4（已完成）消耗堆与能力牌：[游戏设计](Docs/开发计划/Ver%200.x/0.4/游戏设计.md) / [技术设计](Docs/开发计划/Ver%200.x/0.4/技术设计.md) / [TODO](Docs/开发计划/Ver%200.x/0.4/TODO.md)
- 0.3（已完成）可见规则：[游戏设计](Docs/开发计划/Ver%200.x/0.3/游戏设计.md) / [技术设计](Docs/开发计划/Ver%200.x/0.3/技术设计.md) / [TODO](Docs/开发计划/Ver%200.x/0.3/TODO.md)
- 0.2（已完成）效果列表：[游戏设计](Docs/开发计划/Ver%200.x/0.2/游戏设计.md) / [技术设计](Docs/开发计划/Ver%200.x/0.2/技术设计.md) / [TODO](Docs/开发计划/Ver%200.x/0.2/TODO.md)
- 0.1（已完成）历史计划：[`MVP计划与迭代计划.MD`](Docs/开发计划/Ver%200.x/0.1/MVP计划与迭代计划.MD)
- Phase 1：[游戏设计](Docs/开发计划/Ver%200.x/0.1/Phase%201/游戏设计.md) / [技术设计](Docs/开发计划/Ver%200.x/0.1/Phase%201/技术设计.md) / [TODO](Docs/开发计划/Ver%200.x/0.1/Phase%201/TODO.md)
- Phase 2a：ActionSystem + EffectSystem —— [游戏设计](Docs/开发计划/Ver%200.x/0.1/Phase%202a/游戏设计.md) / [技术设计](Docs/开发计划/Ver%200.x/0.1/Phase%202a/技术设计.md) / [TODO](Docs/开发计划/Ver%200.x/0.1/Phase%202a/TODO.md)
- Phase 2b：BuffSystem —— [游戏设计](Docs/开发计划/Ver%200.x/0.1/Phase%202b/游戏设计.md) / [技术设计](Docs/开发计划/Ver%200.x/0.1/Phase%202b/技术设计.md) / [TODO](Docs/开发计划/Ver%200.x/0.1/Phase%202b/TODO.md)
- Phase 2c：RuleSystem —— [游戏设计](Docs/开发计划/Ver%200.x/0.1/Phase%202c/游戏设计.md) / [技术设计](Docs/开发计划/Ver%200.x/0.1/Phase%202c/技术设计.md) / [TODO](Docs/开发计划/Ver%200.x/0.1/Phase%202c/TODO.md)
- 架构思路（启动期评审，部分已过时，见文首状态）：[`Docs/设计思路.MD`](Docs/设计思路.MD)
- 杀戮尖塔机制研究：[`Docs/杀戮尖塔机制研究.md`](Docs/杀戮尖塔机制研究.md)；局内规则对照表：[`Docs/详细游戏设计文档参考/README.md`](Docs/详细游戏设计文档参考/README.md)；扩展边界批注：[`Docs/Phase 2 扩展边界批注.md`](Docs/Phase%202%20扩展边界批注.md)
- AI 协作分工：[`Docs/AI协作分工.md`](Docs/AI协作分工.md)

## 开发工作流

个人开发阶段采用简单的 trunk-based 工作流，只维护 `main` 分支并保持小步提交；暂不创建 `dev` 分支。

## 运行方式

1. 使用 Unity Hub 安装 `ProjectSettings/ProjectVersion.txt` 中指定的版本（当前为 **Unity 6000.5.9f1**）。
2. 在 Unity Hub 中打开本项目根目录。
3. 打开 `Assets/Scenes/TestScene.unity`。
4. 点击 Play 运行场景：玩家 80 血，对手是颚虫（状态栏 `颚虫 HP 42/42  格挡 0`）。可以看到双方生命与格挡、双方 Buff 层数、能量、三个牌堆数量（抽牌 / 弃牌 / 消耗）、遗物行（`遗物：金刚杵  蛇颅骨  纸鹤`）、回合数、敌人意图；手牌费用行带类型（`攻击 / 技能 / 能力 · 费用 X`），攻击伤害是当前力量 / 易伤修正后的数值，与打出后实际发生的一致；勾消耗的牌（坚不可摧）末行印 `消耗`，它和能力牌（力量强化）打出后进消耗堆、本场不再抽到，留在手里结束回合则照常进弃牌堆。敌人意图按行动表循环、由效果推导：`攻击 11` → `攻击 7 · 防御` → `增益 · 防御` → `攻击 14`（咆哮给了自己 3 层力量，敌人 Buff 行 `力量 3`）→ `攻击 10 · 防御` → …，猛击 / 咆哮给的格挡顶住玩家整个下一回合、在敌人自己的下一回合开始才清。三件遗物在默认配置里都能看到：开局 Buff 行就有金刚杵给的 `力量 1`、攻击卡卡面 7（打在猛击的 5 格挡上掉 2）；打出剧毒敌人是 `中毒 4`（蛇颅骨 +1）；给敌人上虚弱后咬的意图从 11 变 6（纸鹤把虚弱倍率从 0.75 压到 0.6；无纸鹤是 8）。在 `Default.asset` 的 `Relics` 列表里增删即可换遗物配置；把 `Enemy` 换成 `Assets/Data/Enemies/FixedAttacker.asset` 就是 0.1 那只每回合固定攻击 6 的敌人——换敌人只是换一个引用，不用改代码。点击手牌使用卡牌，点击"结束回合"推进战斗；胜利或失败时会显示对应遮罩并禁止继续操作。

![Phase1局内战斗截图](image/README/Phase1局内战斗截图.png)

## 运行测试

1. 打开 Unity 菜单 `Window` → `General` → `Test Runner`。
2. 切换到 **EditMode** 页签。
3. 点击 **Run All**。`CombatantStateTests`、`CardPileTests`、`CardDefinitionTests`、`EffectSpecTests`、`EnemyDefinitionTests`、`BattleSessionTests`（六个 partial 在 `BattleSession/` 子目录：`AnchorCards.cs` 三张锚点卡、`Preview.cs` 预览 = 结算、`Exhaust.cs` 消耗堆、`Relics.cs` 遗物、`Enemies.cs` 敌人行动表）、`ActionQueueTests`、`EffectTests`、`BuffTests`、`BuffFactoryTests`、`RelicFactoryTests`、`DamageRuleTests`、`EffectSpecDataTests`、`EnemyActionDataTests`、`CardDescriptionFormatterTests` 与 `IntentFormatterTests` 应全部通过（共 246 项：0.1～0.5 的 203 项中 202 项未改断言——0.1 的 79 项在 0.2 只改过卡牌构造方式，0.6 把它们的敌人参数改为经 `FixedAttacker` 构造的 `EnemyDefinition`；唯一改断言的 1 项是中毒用例的敌人格挡 10 → 0，因为 0.6 起敌人回合开始先清敌人格挡；0.6 新增 43 项）。

## 目录说明

- `Assets/Scripts/Runtime/`：C# 代码，`Runtime.asmdef` 单一程序集（引用 `Unity.TextMeshPro`、`UnityEngine.UI`），按依赖方向分四层：
  - `Core/`：纯 C# 战斗规则（`Battle`/`Combatants`/`Cards`/`Actions`/`Effects`/`Buffs`/`Rules`/`Relics`/`Enemies`），不引用 `UnityEngine`。`Actions` 只知道"排队和执行"，`Effects` 只知道"怎么改 `CombatantState`"，两者互不知道调用方是谁（详见 [Phase 2a 技术设计](Docs/开发计划/Ver%200.x/0.1/Phase%202a/技术设计.md)）；`Buffs` 保存 Buff 核心状态，只有需要生命周期行为的 Buff 才额外实现 `IBuffTrigger`，触发逻辑下沉到具体子类自己实现（详见 [Phase 2b 技术设计](Docs/开发计划/Ver%200.x/0.1/Phase%202b/技术设计.md)）；`Rules` 是纯函数（只读 Buff、只写伤害数值），固定两段管线写死在 `DamageCalculator` 内部，不做注册引擎（详见 [Phase 2c 技术设计](Docs/开发计划/Ver%200.x/0.1/Phase%202c/技术设计.md)）；`Cards` 里的 `EffectSpec` 是封闭的五种效果原语，只描述"做什么、对谁、多少"、不带执行逻辑，把它变成 Action 的地方只有 `BattleSession.ToAction`，`Cards` 不引用 `Actions` / `Buffs`（Buff 只是字符串 Id，由 `Buffs/BuffFactory` 解析）（详见 [0.2 技术设计](Docs/开发计划/Ver%200.x/0.2/技术设计.md)）；`Battle` 另暴露两个只读预览，复用 `Rules` 的 `DamageCalculator`，是界面上公式后数字的唯一来源（详见 [0.3 技术设计](Docs/开发计划/Ver%200.x/0.3/技术设计.md)）；打出后的去向由 `Cards` 里的 `CardDefinition.ExhaustsWhenPlayed`（类型默认 + 消耗关键词）回答，`CardPile` 是四个牌区的哑容器、消耗堆只进不出，`Battle` 只问结果、不认类型（详见 [0.4 技术设计](Docs/开发计划/Ver%200.x/0.4/技术设计.md)）；`Buffs/BuffIds` 是 Buff Id 字面量的唯一出处；`Relics` 是遗物基础设施与三件标本——遗物不是 Buff，挂在 `CombatantState` 独立列表里，钩子形状与 `IBuffTrigger` 相同（开战钩子只入队 Action、施加钩子直接改这一次的 `BuffState`），改公式常数的遗物不设钩子、由对应 Rule 读"是否持有"，`Battle` 只认 `RelicState` 与 `IBattleStartRelic`（详见 [0.5 技术设计](Docs/开发计划/Ver%200.x/0.5/技术设计.md)）；`Enemies` 是敌人定义与行动表——`EnemyAction` = 效果列表、`EnemyDefinition` = 血量 + 至少一条行动，只引用 `Cards`、和 `Cards` 一样只描述不执行；`Battle` 按行动表固定循环执行敌人回合，`ToAction` 是卡牌与敌人行动共用的唯一 `EffectSpec → IAction` 转换点，`Session` 里没有任何敌人 Id 或按敌人的分支（详见 [0.6 技术设计](Docs/开发计划/Ver%200.x/0.6/技术设计.md)）。
  - `Data/`：`CardData`（含 `exhaust` 勾选）/`EffectSpecData`/`RelicData`/`EnemyData`/`EnemyActionData`/`BattleConfig`（含 `enemy` 引用与 `relics` 列表）等 ScriptableObject 配置定义，`Data → Core` 单向依赖，`ToDefinition` 把可序列化的效果条目转成 `EffectSpec`（卡牌与敌人行动共用 `EffectSpecData`），`ToState` 经 `RelicFactory` 把遗物 Id 落成实例。必填字段不给能通过校验的默认值，漏填在启动校验就报出来。
  - `Controllers/`：`BattleController`，Unity 适配与组装，持有 `BattleSession`，转发命令、只读预览与牌堆数量，暴露 `EnemyDisplayName` / `CurrentEnemyAction`（来自 Core 的 `EnemyDefinition`），把遗物 Id 翻成显示名（`RelicDisplayName`）。
  - `Views/`：`BattleView`/`CardButtonView`/`EndScreenView` 与纯函数 `CardDescriptionFormatter`/`IntentFormatter`/`BuffDisplayNames`，纯显示与输入捕获，不直接调用 `Core`/`Data`，不复算任何伤害公式（描述生成器只读 `CardDefinition` 与一个 `int → int` 预览委托；意图生成器只读 `EnemyAction` 与 Controller 转发的预览总和）；卡面类型中文是 `BattleView` 私有 `switch`，消耗关键词行由 `CardDescriptionFormatter` 追加；遗物行的显示名问 Controller，不设 `RelicDisplayNames`。
- `Assets/Scripts/Tests/EditMode/`：`Core`、`Data` 校验与纯函数 View（卡面文案、意图文案）的 EditMode 单元测试（`Tests.asmdef`；`BattleSessionTests` 的六个 partial 在 `BattleSession/` 子目录；根目录到 15 个测试文件，0.7 起新文件进子目录）。Controllers/带 MonoBehaviour 的 Views 是薄封装/展示层，按技术设计文档的测试策略不做单元测试，靠 Play 模式手动验收。
- `Assets/Data/Cards/`、`Assets/Data/Relics/`、`Assets/Data/Enemies/`、`Assets/Data/Battles/`：运行时 ScriptableObject 配置资产（攻击/防御/治疗/力量强化（能力）/剧毒/虚弱/易伤/痛击/双击/剑柄打击/坚不可摧（消耗）共 11 种卡牌；金刚杵/蛇颅骨/纸鹤三件遗物；颚虫 / 固定攻击敌人两份 `EnemyData`；`Default` 战斗配置：玩家 80、敌人颚虫、牌组共 18 张、遗物三件）。
- `Assets/Scenes/`：Unity 场景，`TestScene` 内含 Canvas 战斗界面与 `BattleController`。
- `Docs/`：设计文档。`开发计划/` 按版本存放各版本的游戏设计 / 技术设计 / TODO（`Ver 0.x/0.1/` 是已完成的 Phase 1 / 2 与历史 MVP 计划）；`详细游戏设计文档参考/` 是杀戮尖塔局内规则对照表；根目录是跨版本的机制研究、扩展边界批注、AI 协作分工与启动期设计思路。
- `Packages/`：Unity Package Manager 依赖清单。
- `ProjectSettings/`：Unity 工程设置。

## 已知限制

- **中文字体**：TMP 默认字体 `LiberationSans SDF` 不含中文字形，目前是否已配置 Fallback 字体资产取决于本地环境；如果界面显示中文方块/警告，按 `Window → TextMeshPro → Font Asset Creator` 生成一个中文字体资产并加入 Fallback 列表（详见开发过程记录，未固化为文档）。
- **打出时序（0.1 的"`EnqueueCardAction` 结算顺序"一条，0.2 已解决）**：`BattleSession.TryPlayCard` 现在是"扣能量 → `TakeFromHand`（离手，此刻不在任何牌区）→ 按效果列表 `ToAction` 入队并 `RunAll` → 按 `ExhaustsWhenPlayed` `AddToDiscard` 或 `AddToExhaust`"。抽牌类效果结算时打出的牌已经离手，抽到的牌落在手牌末尾；抽牌堆为空触发重洗时它不在弃牌堆里，不会被洗回去。`EnqueueCardAction` 与 `CardPile.PlayCard` 已删除，见 [0.2 技术设计](Docs/开发计划/Ver%200.x/0.2/技术设计.md) 第 4.4 / 4.6 节。仍留的口子："离手到落堆"之间牌不在任何牌区，原版此时可以被"复制打出"类效果读到，那需要给 `CardPile` 加显式的"结算中"位置，出现消费者再做。
- **"消耗"只在打出时生效**：`CardPile.DiscardHand` 与重洗都不看关键词——留在手里的坚不可摧回合结束进弃牌堆、下回合还能抽到，这是原版规则（在这里看关键词是"虚无"的语义，0.4 没有虚无）；被效果消耗（"消耗手牌中一张牌"）是另一个时机，尚无消费者。`CardType.Power = 2` 追加后，`CardData` 的越界校验拦不住 0.1 旧资产残留的整数 2（它现在是合法的能力牌），手改资产时只能靠 Inspector 核对类型。
- **遗物钩子只有两种时机、且只入队 Action**：`IBattleStartRelic.OnBattleStart(owner, queue)` 不带对手参数（弹珠袋来了再加），钩子里只入队 Action、不直接改状态；`IApplyBuffModifier` 只能改这一次施加的 `BuffState`，不能取消施加（人工制品来了再加）；遗物没有回合开始 / 结束钩子，`LoseHp` 前也没有挂钩（钨条）。三件遗物都在玩家身上，敌人遗物没有消费者。遗物栏目前来自 `BattleConfig.relics`，1.0 起应由 Run 提供。`WeakDamageRule` 是 Core 里唯一按具体遗物 Id 分支的地方——改公式常数的遗物就该改对应 Rule 一处，Session 不认遗物 Id。
- **预览是格挡前的最终伤害**：卡面与意图上的数字是 `DamageCalculator` 的输出，不扣格挡——玩家先打防御再看意图，意图仍显示公式后数值，实际掉血更少或不掉。这与原版意图一致，是设计不是缺陷；把格挡折进预览就得在 Session 或 View 复算 `TakeDamage`，等于第二条结算路径。预览读的是此刻的 Buff，痛击自己即将施加的易伤不算进它的 8 点，与结算的"先伤后易伤"一致；多段攻击的意图数字是各段独立取整后的和。
- **`EnemyTurn` 仍是瞬时阶段，行动表是固定循环**：敌人回合（清敌人格挡 → 中毒 → 当前行动 → 推进指针）在 `TryEndPlayerTurn` 里一次跑完，没有 AI 决策、动画等待或分段，外部几乎观察不到 `EnemyTurn`。行动顺序固定循环（原版颚虫按概率选招且同一招不连用），出现概率或"不能连用"时再抽行动模式对象——现在的行动指针只是一个 `int`。0.6 的配置校验拒绝空行动与"敌人给玩家上 Buff"：前者是没有沉睡类敌人之前防漏配，后者要等 0.7 的减层，蓝奴隶贩子随 0.7 进来。意图只有文字、由效果推导，没有图标，Core 里没有意图枚举。
- **`BattleSession` 身兼多职**：战斗生命周期、回合、能量、卡牌流转、敌人行动表的执行与指针都在一个类里；效果结算已迁移到独立的 `Core/Actions`/`Core/Effects`（Phase 2a），`EffectSpec → IAction` 的转换（`ToAction`）有意留在 Session 私有方法里——它依赖的全是 Session 字段，0.6 敌人行动成了它的第二个调用方但仍在 Session 内（`source` 传 `Enemy`），预期中"Session 以外的调用方"没有出现，所以没有抽成工厂。其余职责暂不拆分。
- **Controllers/Views 无自动化测试**：`BattleController`/`BattleView` 等只做转发和展示，真正的规则逻辑都在已被覆盖的 `BattleSession`；为它们搭 PlayMode 测试基建的收益暂不成比例，可靠性依赖手动 Play 模式验收。
