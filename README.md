# CardRPGFramework

使用 Unity 与 C# 开发的**简化版《杀戮尖塔》求职 Demo**，重点展示数据驱动、战斗规则与客户端工程化能力，不追求商业级内容量。

## 当前阶段

**0.1（Phase 1 + Phase 2a/2b/2c）已完成，下一步是 0.2。** 版本路线与各版本文档见 [`Docs/开发计划/README.md`](Docs/开发计划/README.md)：0.x 继续补齐局内卡牌战斗（效果列表、消耗堆、遗物、敌人行动表），1.0 做第一次完整的短 Run，1.1 之后才做出包等交付工作。

Phase 1 已完成：可以在 `TestScene` 的 Play 模式里从战斗开始一路操作到胜利或失败，Core 战斗规则、ScriptableObject 配置、Unity 表现层全部接通。

Phase 2（ActionSystem/EffectSystem/BuffSystem/RuleSystem）拆分成 [Phase 2a](Docs/开发计划/Ver%200.x/0.1/Phase%202a/游戏设计.md)、[Phase 2b](Docs/开发计划/Ver%200.x/0.1/Phase%202b/游戏设计.md)、[Phase 2c](Docs/开发计划/Ver%200.x/0.1/Phase%202c/游戏设计.md) 三个独立可验收阶段，已全部完成。

**Phase 2a（ActionSystem + EffectSystem）已完成**：卡牌效果的结算方式从 `BattleSession` 直接调用 `CombatantState` 改为"生成 Action 入队、Action 执行时调用 Effect"，玩家可见行为与 Phase 1 完全一致（纯架构重构，未新增卡牌/数值）。

**Phase 2b（BuffSystem）已完成**：新增 Buff 容器（`Core/Buffs`）与两个具体 Buff——力量（纯数值，无触发，效果留给 Phase 2c 的 RuleSystem 消费）、中毒（回合开始按层数造成不受格挡影响的 HP Loss，随后层数减 1）；牌组新增「力量强化」「剧毒」两张卡，扩充为 12 张。

**Phase 2c（RuleSystem）已完成，Phase 2（2a+2b+2c）全部完成**：新增 `Core/Rules`——伤害规则是纯函数（只读 Buff、只写 `DamageContext.Damage`），固定两段管线（力量加法在前，虚弱/易伤乘法在后），最后统一 `Math.Floor` 一次。`DamageAction` 新增 `source`，玩家出牌与敌人固定攻击统一经过同一套 `DamageCalculator`；牌组新增「虚弱」「易伤」两张卡（目标都是敌人，虚弱降低的是敌人自己的攻击），扩充为 14 张。

对照历史计划 [`MVP计划与迭代计划.MD`](Docs/开发计划/Ver%200.x/0.1/MVP计划与迭代计划.MD) 的完成标准：战斗可玩、14 张卡牌、5 种效果（伤害/格挡/治疗/HP Loss/Buff）、规则系统均已达成；Buff 目前 4 种、配置驱动（Luban）与简单 AI 未达成。后两项不再按该文档的 Phase 3 推进：敌人行动表归 0.6，Luban 进 1.2+ 候选池，见 [`Docs/开发计划/README.md`](Docs/开发计划/README.md)。

**已完成**

- `BattlePhase`：战斗阶段枚举（`NotStarted` / `PlayerTurn` / `EnemyTurn` / `Victory` / `Defeat`）。
- `CombatantState`：生命值、格挡、治疗与边界。
- `CardType` / `CardDefinition`：攻击、防御、治疗三种不可变卡牌定义。
- `CardPile`：抽牌堆、手牌、弃牌堆；抽空重洗；两堆皆空时安全停止。
- `BattleSetup` / `BattleSession`：战斗初始化、玩家回合循环（清格挡、恢复能量、抽牌）、卡牌使用校验与能量消耗、结束回合与敌人固定攻击、胜负判定与结束后拒绝操作。
- `IAction` / `ActionContext` / `ActionQueue`：贯穿整场战斗的 FIFO 行动队列，`RunAll` 执行期间可以继续入队新 Action。
- `DamageAction` / `BlockAction` / `HealAction`：与攻击/防御/治疗三种卡牌效果一一对应，`Execute` 时调用对应 Effect。
- `DamageEffect` / `BlockEffect` / `HealEffect` / `HpLossEffect` / `BuffEffect`：转发到 `CombatantState` 现有方法的纯状态修改层，不做任何数值计算。
- `BuffState` / `IBuffTrigger` / `StrengthBuff` / `PoisonBuff` / `WeakBuff` / `VulnerableBuff`：Buff 容器基础设施。触发行为下沉到具体子类自己实现（`PoisonBuff` 自己实现 `IBuffTrigger`），不做外部按 Id 扫描匹配的触发器；虚弱/易伤和力量一样是纯数值、无触发，只是被 Rule 读取。
- `ApplyBuffAction` / `HpLossAction`：`ApplyBuffAction` 把 Buff 施加到目标；`HpLossAction` 独立于 `DamageAction` 存在（不受格挡影响），当前唯一使用者是中毒。
- `CombatantState` 新增 Buff 容器（`ApplyBuff`/`GetBuffStacks`/`HasBuff`/`Buffs`）与 `LoseHp`（忽略格挡，且是生命值减少的唯一入口，`TakeDamage` 内部扣完格挡后复用它）。
- `DamageContext` / `IDamageRule` / `StrengthDamageRule` / `WeakDamageRule` / `VulnerableDamageRule` / `DamageCalculator`：伤害规则纯函数，固定两段管线（加法先于乘法），不做规则注册引擎；虚弱/易伤按"有无"而非层数生效。`DamageAction` 新增 `source`，`Execute` 内部先经 `DamageCalculator` 修正再交给 `DamageEffect`，玩家出牌与敌人固定攻击统一走这一条路径。
- `CardData` / `BattleConfig`：ScriptableObject 配置，转换为 `CardDefinition`/`BattleSetup`，带启动期校验（空引用、非法数值、空 ID、重复 ID）。
- `BattleController`：场景里显式引用 `BattleConfig`，创建并持有 `BattleSession`，转发使用手牌/结束回合命令，暴露只读战斗状态。
- `BattleView` / `CardButtonView` / `EndScreenView`：最小战斗 UI，显示双方状态、能量、回合、敌人意图、手牌与最近操作结果，胜利/失败时显示遮罩并阻止继续操作。
- EditMode 测试覆盖伤害/格挡/治疗/生命值损失边界、Buff 容器与中毒触发链路、伤害规则单独验证与组合取整时机、抽牌/弃牌/重洗与固定种子洗牌、`ActionQueue` 的执行顺序与重入入队、Effect 转发、`BattleSession` 的合法流程与关键拒绝路径（含中毒致死立即胜利、跳过敌人攻击；敌人固定攻击迁移到 `DamageAction` 管线后无 Buff 时数值与 Phase 1 一致），以及抽牌堆耗尽后经过完整战斗流程仍能正确重洗弃牌堆。

**0.1 范围外（后续版本或候选池，未实现）**

- 卡牌效果列表、复合卡、打出时抽牌（0.2）；公式后伤害与 Buff 层数显示（0.3）；消耗堆、能力牌（0.4）；遗物（0.5）；敌人行动表与意图（0.6）；回合型 Buff 衰减（0.7）；局外短 Run（1.0）。
- Attribute / Event 等独立框架；规则集合硬编码在 `DamageCalculator` 内部，不做注册/配置化。
- 可组合 Action（`CompositeAction`/`ConditionalAction` 等）、多敌人、目标选择。
- 暴击、闪避、护甲穿透等规则；无实体/鸟居/钨条等更靠后的伤害挂钩点（见 [`Docs/Phase 2 扩展边界批注.md`](Docs/Phase%202%20扩展边界批注.md)）。
- 卡牌升级、稀有度、装备、商店/事件节点、存档、战斗回放。
- 配置驱动（Luban/Excel）、出包与程序集拆分（1.1+）、正式美术、动画、音效。

## 文档

- **开发计划（按版本）**：[`Docs/开发计划/README.md`](Docs/开发计划/README.md) —— 版本约定、路线总览、[0.x 大纲](Docs/开发计划/Ver%200.x/README.md)、[1.x 大纲](Docs/开发计划/Ver%201.x/README.md)
- 下一版本 0.2：[游戏设计](Docs/开发计划/Ver%200.x/0.2/游戏设计.md) / [技术设计](Docs/开发计划/Ver%200.x/0.2/技术设计.md) / [TODO](Docs/开发计划/Ver%200.x/0.2/TODO.md)
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
4. 点击 Play 运行场景：可以看到玩家/敌人生命与格挡、能量、回合数、敌人固定攻击意图；点击手牌使用卡牌，点击“结束回合”推进战斗；胜利或失败时会显示对应遮罩并禁止继续操作。

![Phase1局内战斗截图](image/README/Phase1局内战斗截图.png)

## 运行测试

1. 打开 Unity 菜单 `Window` → `General` → `Test Runner`。
2. 切换到 **EditMode** 页签。
3. 点击 **Run All**。`CombatantStateTests`、`CardPileTests`、`BattleSessionTests`、`ActionQueueTests`、`EffectTests`、`BuffTests` 与 `DamageRuleTests` 应全部通过（共 79 项）。

## 目录说明

- `Assets/Scripts/Runtime/`：C# 代码，`Runtime.asmdef` 单一程序集（引用 `Unity.TextMeshPro`、`UnityEngine.UI`），按依赖方向分四层：
  - `Core/`：纯 C# 战斗规则（`Battle`/`Combatants`/`Cards`/`Actions`/`Effects`/`Buffs`/`Rules`），不引用 `UnityEngine`。`Actions` 只知道"排队和执行"，`Effects` 只知道"怎么改 `CombatantState`"，两者互不知道调用方是谁（详见 [Phase 2a 技术设计](Docs/开发计划/Ver%200.x/0.1/Phase%202a/技术设计.md)）；`Buffs` 保存 Buff 核心状态，只有需要生命周期行为的 Buff 才额外实现 `IBuffTrigger`，触发逻辑下沉到具体子类自己实现（详见 [Phase 2b 技术设计](Docs/开发计划/Ver%200.x/0.1/Phase%202b/技术设计.md)）；`Rules` 是纯函数（只读 Buff、只写伤害数值），固定两段管线写死在 `DamageCalculator` 内部，不做注册引擎（详见 [Phase 2c 技术设计](Docs/开发计划/Ver%200.x/0.1/Phase%202c/技术设计.md)）。
  - `Data/`：`CardData`/`BattleConfig` 等 ScriptableObject 配置定义。
  - `Controllers/`：`BattleController`，Unity 适配与组装，持有 `BattleSession`。
  - `Views/`：`BattleView`/`CardButtonView`/`EndScreenView`，纯显示与输入捕获，不直接调用 `Core`/`Data`。
- `Assets/Scripts/Tests/EditMode/`：`Core` 的 EditMode 单元测试（`Tests.asmdef`）。Controllers/Views 是薄封装/展示层，按技术设计文档的测试策略不做单元测试，靠 Play 模式手动验收。
- `Assets/Data/Cards/`、`Assets/Data/Battles/`：运行时 ScriptableObject 配置资产（攻击/防御/治疗/力量强化/剧毒/虚弱/易伤共 7 种卡牌，`Default` 战斗配置的牌组共 14 张）。
- `Assets/Scenes/`：Unity 场景，`TestScene` 内含 Canvas 战斗界面与 `BattleController`。
- `Docs/`：设计文档。`开发计划/` 按版本存放各版本的游戏设计 / 技术设计 / TODO（`Ver 0.x/0.1/` 是已完成的 Phase 1 / 2 与历史 MVP 计划）；`详细游戏设计文档参考/` 是杀戮尖塔局内规则对照表；根目录是跨版本的机制研究、扩展边界批注、AI 协作分工与启动期设计思路。
- `Packages/`：Unity Package Manager 依赖清单。
- `ProjectSettings/`：Unity 工程设置。

## 已知限制与 Phase 2 输入

- **中文字体**：TMP 默认字体 `LiberationSans SDF` 不含中文字形，目前是否已配置 Fallback 字体资产取决于本地环境；如果界面显示中文方块/警告，按 `Window → TextMeshPro → Font Asset Creator` 生成一个中文字体资产并加入 Fallback 列表（详见开发过程记录，未固化为文档）。
- **`EnqueueCardAction` 结算顺序**：`BattleSession.TryPlayCard` 里，Action 队列执行发生在卡牌移入弃牌堆之前。目前七种卡牌效果都不修改手牌/牌堆，顺序目前没有影响；以后出现"打出时触发抽牌"等会改变手牌的效果时，需要重新核对 `handIndex` 在结算时刻的语义（另见 [`Phase 1/技术设计.md`](Docs/开发计划/Ver%200.x/0.1/Phase%201/技术设计.md) 第 7 节）。0.2 会把时序改为"先离手、再结算、再进弃牌"，见 [0.2 技术设计](Docs/开发计划/Ver%200.x/0.2/技术设计.md) 第 4.6 节。
- **`EnemyTurn` 是瞬时阶段**：敌人固定攻击自 Phase 2c 起已经迁移到 `DamageAction`/`ActionQueue`，和玩家出牌走同一套规则管线，但没有独立 AI 或动画等待，`EnemyTurn` 存在时间极短、外部几乎观察不到；意图/AI（目前只有固定攻击一种"意图"）留给后续 Phase。
- **`BattleSession` 身兼多职**：战斗生命周期、回合、能量、卡牌流转、敌人行动目前都在一个类里；效果结算已迁移到独立的 `Core/Actions`/`Core/Effects`（Phase 2a），其余职责暂不拆分。
- **Controllers/Views 无自动化测试**：`BattleController`/`BattleView` 等只做转发和展示，真正的规则逻辑都在已被覆盖的 `BattleSession`；为它们搭 PlayMode 测试基建的收益暂不成比例，可靠性依赖手动 Play 模式验收。
