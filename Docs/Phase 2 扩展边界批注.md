# Phase 2 扩展边界批注（杀戮尖塔 1 对照）

> **性质：风险备忘，不是需求清单。** 本文不改变 Phase 2a/2b/2c 技术设计的完成定义、排除项与工期；只记录按杀戮尖塔 1 局内机制逐条核对后，现有设计将来会碰壁的位置、缺的能力和正确的引入时机。机制事实以官方 wiki 与一代反编译资料为准（见文末参考），不以其它项目（如 CardLordRng 的 RuleSystem）的既有实现作为迁移依据。
>
> **冻结声明**：本文不作为 Phase 2 实现要求；除第 1 节已吸收的接口形状外，其余能力仅在出现对应机制消费者时引入。本文自本次修订后冻结，不再继续扩充机制清单。（2026-09 勘误两处、不新增条目：第 2 节冰淇淋一行原写「能量保留」，按 wiki.gg 改为「当前能量 += 上限」；第 4 节补 `justApplied` 的原版语义。）
>
> **0.1 之后怎么用**：第 1 节两处已在代码里。第 2～4 节是加遗物 / 新挂钩时的对照表——先查表判断挂哪一层，再动手；它**不是**按行做完的 TODO。哪一版做哪件见 [`开发计划/README.md`](开发计划/README.md)（0.5 消费蛇颅骨 / 纸鹤 / 金刚杵，0.7 消费轮末减层与刚施加保护）。

## 1. 已吸收进 Phase 2b 设计的两处修正

Phase 2 尚未开始编码，下面两处只是文档层面的接口形状修正：不增加任何机制、不新增测试负担、不影响验收标准与工期。

| 修正 | 内容 | 为什么现在改而不是只记批注 |
| --- | --- | --- |
| `ApplyBuffAction` 带施加者 | 构造参数新增 `source`，本阶段无消费者 | 施加者在调用点免费可得；原版"响应施加"的机制——人工制品（Artifact，取消下一次负面施加）、Snecko Skull（施加中毒时额外 +1）、冠军腰带（Champion Belt，施加易伤时追加 1 虚弱）——全部需要"谁在施加"。事后补字段要迁移全部出牌分支，现在补是零成本 |
| 生命减少出口唯一 | `TakeDamage` 扣完格挡后的剩余伤害改为调用 `LoseHp`，`LoseHp` 是实际减少 `CurrentHp` 前的统一入口 | 外部行为与既有测试断言完全不变，属于实现方式选择而非预留。这个入口给钨条（Tungsten Rod，失去生命时少扣 1）这类"不看来源、只要生命即将减少就生效"的机制；它**不承担**格挡前的最终伤害修正——无实体（Intangible）在格挡前把伤害改为 1，挂钩点见第 2 节时机地图 |

除这两处外，下文全部是"到时再做"，任何一条都不需要在 Phase 2 动手。

**明确否决的一条同类建议**：`DamageContext` 现在不加 `DamageType` 字段。当前伤害类型的区分由 Action 类型承担（`DamageAction` = Attack、`HpLossAction` = HP Loss），`DamageCalculator` 只被 Attack 路径调用，加了枚举没有任何规则会读它。钨条的跨类型生效由"生命减少出口唯一"覆盖；无实体的跨类型生效靠"两条伤害路径在格挡前共用同一个最终修正点"解决，等无实体出现再做。`DamageType` 本身仍等荆棘等第三条路径出现时再抽，与 Phase 2b 技术设计第 4 节"出现第二次同类变化再抽象"的既定判断一致。

## 2. 现有设计与尖塔 1 机制的碰壁点对照

无实体、鸟居、钨条三件的挂钩点互不相同，先给出按原版核实的结算时机地图（供未来实现者对位，不是实现任务）：

```text
伤害产生（力量/虚弱/易伤等——仅 Attack 走 DamageCalculator）
    ↓
最终伤害修正（无实体：格挡前改为 1；Attack 与 HP Loss 都要经过）
    ↓
格挡吸收（仅 Attack；HP Loss 跳过）
    ↓
格挡后观察（鸟居：仅未被格挡的 Attack ≤5 → 1）
    ↓
LoseHp（钨条：即将减少 CurrentHp 时 -1，不看来源/类型）
    ↓
CurrentHp -= value
```

反例（挂错位置的后果）：50 攻击 + 10 格挡 + 无实体 → 伤害先在格挡前变 1，再被格挡吃掉，掉 0 血；若把无实体挂在 `LoseHp` 上，则 50 被格挡剩 40、再改写为 1，会错误地掉 1 血。

| 现有选择（出处） | 尖塔 1 真实机制 | 缺的能力 | 引入时机 |
| --- | --- | --- | --- |
| `ApplyBuffAction.Execute` 直接写容器（2b §4） | 人工制品取消负面施加；Snecko Skull 给这一次施加 +1（不触发第二次"施加"事件，旁证：Sadistic Nature 不会因它二次触发）；冠军腰带施加易伤时追加 1 虚弱 | 施加执行前后的钩子：可改量、可取消、可追加新 Action；Buff/Debuff 极性标记 | 第一个"响应施加"的遗物/卡牌 |
| `ApplyBuff` 只会 `AddStacks`（2b §4） | 缴械（Disarm）施加 -2 力量；人工制品拦截后"这次没加上" | 负层数入口；施加结果反馈 | 缴械/人工制品 |
| 触发只扫 `owner.Buffs`、只有 `OnTurnStart`（2b §5） | 活动肌肉（Flex）的力量下降、金属化在"角色行动结束"触发；虚弱/易伤在"轮结束"统一减 1，且本轮刚施加的不减（justApplied 保护） | `OnTurnEnd` 与"轮结束"两级时机；触发遍历中只入队 Action、不得直接改 Buff 容器 | Flex / 金属化 / 虚弱衰减 |
| 遗物没有容身处（研究文档 §6） | 纸鹤（Paper Krane）、钨条、Snecko Skull 常驻遗物列表，从不进任何生物的 Power 栏 | 本场可见的遗物列表，与 Buff 字典并列、复用同一类钩子形状；不要把遗物 `ApplyBuff` 进字典 | 第一个遗物 |
| `DamageCalculator` 两段管线止于取整（2c §4） | 无实体在**格挡前**把伤害改为 1（原版 `atDamageFinal*` 钩子），对 HP Loss 同样生效 | 格挡前的 Final 修正点；`DamageAction` 与 `HpLossAction` 都要经过它（HP Loss 不进 Calculator 的力量/虚弱段，但要进 Final） | 无实体 |
| `TakeDamage` 扣完格挡直接进扣血（2b §4） | 鸟居（Torii）看的是"**未被格挡**的攻击伤害 ≤5 → 1"，仅 Attack | 格挡后、仅 Attack 的观察点；不放进 Calculator，也不放进 `LoseHp` | 鸟居 |
| `HpLossAction` 不经过 Calculator（2b §4） | 钨条对中毒同样减 1（"即将失去生命"时机，不是 Final） | `LoseHp` 前的数值修正——`LoseHp` 已是唯一生命减少入口（见第 1 节），届时在入口前加一次修正即可 | 钨条 |
| `ClearBlock`/能量重置硬编码在 `BattleSession`（2b §5） | 路障（Barricade）：回合开始不清格挡；冰淇淋（Ice Cream）：回合开始不是「当前能量 = 上限」而是「当前能量 += 上限」（上限 3、剩 2 → 5；剩 10 → 13），**不是**跳过重置只带着剩余 | "清格挡"成为可跳过的步骤，"重置能量"成为可换算法（set → add）的步骤，检查 Power/遗物旗标即可，不需要规则引擎；同时重置必须早于狂暴等「回合开始获得能量」的效果 | 路障/冰淇淋 |
| `ActionQueue` 纯 FIFO、不可取消（2a §4） | 荆棘反伤插到队列头（原版 addToTop）；人工制品取消当前施加 | `EnqueueFront`；当前 Action 的取消标记 | 荆棘/人工制品 |
| `DamageContext` 只读 Source/Target 的 Buff（2c §4） | 纸鹤（虚弱 25%→40%）、纸蛙（Paper Phrog，易伤 50%→75%）、怪异蘑菇（Odd Mushroom，自身易伤 50%→25%）都不是交战双方身上的 Buff | Context 可读"本场持有的遗物/旗标" | 纸鹤类遗物 |

## 3. "修改 Buff"必须拆成两类（最易混淆的边界）

CardLordRng 的 `BuffModify`（通用的"某 Buff 强度/上限/时长 ±N%"配置）在尖塔 1 里**没有对应物**，不迁移。原版真实存在的"改 Buff"分两类，落在完全不同的层：

| 类别 | 原版实例 | 落在哪一层 |
| --- | --- | --- |
| 改公式常数（不动层数） | 纸鹤：虚弱减伤 25%→40%；纸蛙：易伤增伤 50%→75%；怪异蘑菇：自身易伤 50%→25% | RuleSystem。`WeakDamageRule` 读"此刻虚弱倍率是多少"（默认 0.75，遗物改这个参数），不是再乘一层伤害 |
| 改某一次施加（改量/取消/追加） | Snecko Skull：这次施加的中毒 +1；人工制品：取消这次负面施加；冠军腰带：追加一次"施加 1 虚弱" | `ApplyBuffAction` 执行前后的钩子。**不是 Rule**——按本项目定义 Rule 是纯函数、不改状态 |

两个容易误判成"通用倍率"的旁证：催化剂（Catalyst）"中毒翻倍/三倍"是**卡牌效果**，本身走一次施加、可被人工制品拦截，不是全局规则；圣树皮（Sacred Bark）只翻倍**药水**效果，也不是"所有施加 ×2"。

## 4. 层数语义备忘

`BuffState.Stacks` 一个字段承载两种语义，与原版 `amount` 一致，字段本身不用改：

- **强度型**：力量层数 = 加多少伤害；中毒层数 = 掉多少血。
- **回合型**：虚弱/易伤层数 = 还剩几回合，效果强度恒定（0.75 / 1.5 与层数无关）。

由此有两条禁令：`WeakDamageRule` 永远按"有无"乘倍率，不要改成按层数放大；将来引入虚弱/易伤衰减时，加的是"轮结束统一减 1 层 + 本轮刚施加不减（justApplied）"，不能复用中毒那种"结算时自减"的模式（时机、粒度都不同，见研究文档 §2）。

`justApplied` 的原版语义：由 Power 构造参数 `isSourceMonster`（施加者是不是怪物）决定，只跳过第一次轮末减层；它**不是**"所有者本轮是否已行动完"的通用判断。1 人局里"敌人给玩家 / 玩家给敌人"两支下两种表述等价，"敌人给自己"这一支只有施加方版本正确；不要把玩家向的直觉表述推广成多人或自施 Debuff 的通用规则。本仓库 0.7 已按施加方实现，见 [`开发计划/Ver 0.x/0.7/游戏设计.md`](开发计划/Ver%200.x/0.7/游戏设计.md) 第 4 节。

## 5. 明确不做的（尖塔 1 局内没有对应物）

| 不做 | 理由 |
| --- | --- |
| AttributeSystem（攻/防/暴击/速度面板） | 尖塔没有可重算的属性面板：牌面伤害是印在卡上的整数，力量/敏捷是 Power，最大生命是局外实体上的整数，进战斗时拷贝即可 |
| 通用 BuffModifier 配置表 | 见第 3 节：原版没有"任意 Buff ±N%"的通用规则，做了反而把纸鹤/人工制品这类条件钩子挤成特例 |
| EventBus 式 TriggerSystem | 原版是战斗循环在固定时机遍历 Power/遗物列表调虚方法（`foreach power → hook()`），不是订阅发布总线；现有 `IBuffTrigger` 方向正确，遗物出现时加同形状的遗物钩子接口即可 |
| Rule 注册中心（`RuleManager.Register`） | 规则集合硬编码在 `DamageCalculator` 里是 2c 既定决策，注册机制没有第二个消费者 |
| 局内/局外两套 RuleSystem | 局外是 RunState（牌组/金币/最大生命/遗物栏），进战斗时转换成局内状态（研究文档 §1 二代架构已验证）；不存在"局外规则系统" |

## 参考

- [Paper Krane](https://slaythespire.wiki.gg/wiki/Paper_Krane) / [Paper Phrog](https://slaythespire.wiki.gg/wiki/Paper_Phrog) / [Odd Mushroom](https://slaythespire.wiki.gg/wiki/Odd_Mushroom)：改虚弱/易伤倍率的三件遗物
- [Snecko Skull](https://slaythespire.wiki.gg/wiki/Snecko_Skull) / [Champion Belt](https://slaythespire.wiki.gg/wiki/Champion_Belt) / [Artifact](https://slay-the-spire.fandom.com/wiki/Artifact)：响应/拦截施加的机制
- [Tungsten Rod](https://slaythespire.wiki.gg/wiki/Tungsten_Rod)（含与无实体、鸟居的结算顺序）/ [Barricade](https://slaythespire.wiki.gg/wiki/Barricade) / [Ice Cream](https://slaythespire.wiki.gg/wiki/Ice_Cream)（*adds the maximum energy limit to the player's current energy*）
- [Flex](https://slaythespire.wiki.gg/wiki/Flex)：力量 + 回合末"力量下降"的配对 Buff 实现
- [Catalyst](https://slaythespire.wiki.gg/wiki/Catalyst) / [Sacred Bark](https://slaythespire.wiki.gg/wiki/Sacred_Bark)：容易被误判成"通用倍率"的两个实例
- 一代 Power 钩子全解（中文）：[杀戮尖塔如何实现Buff效果](https://rinkastone.com/2022/06/16/archives/290/)（`onApplyPower` / `atDamageFinal*` / `stackPower` 等钩子清单）
