# 0.6 TODO：敌人行动表

> **预计 3～4 个有效开发日。风险有三条：一是图省事在 `BattleSession` 里认敌人——按敌人 Id 或"意图种类"分支，而不是把行动当效果列表交给 `ToAction`，写之前对照 [`技术设计.md`](技术设计.md) 第 4.2 节；二是为了保住 0.1 那一项"敌人格挡仍是 10"的断言把清格挡挪到中毒之后，或者干脆不清敌人格挡——正确做法是改那一项断言（技术设计第 5 节）；三是测试只和 `PreviewEnemyAttack()` 对拍——按 0.3 的做法先锁预览再比 HP 差值。验收时对照技术设计第 8 节检查。**

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。

## 0. 开始前检查（约 0.5h）

- [ ] 0.5 已提交推送，工作区干净（`Assets/TextMesh Pro/Fonts/*.asset` 的 Play 抖动还原或单独提交），Console 无错误，EditMode 203 项全绿。
- [x] 技术设计第 4.3 节的 [待确认]（意图文案在 View 侧 `IntentFormatter` 拼，还是 Core 给结构化种类）已定并写回技术设计第 7 节：View 侧直接读效果列表，Core 不加意图类型。

**阶段门槛：** 有干净的回归基线。

## 1. Core：`Core.Enemies` 与 `BattleSession`（约 3h）

- [ ] 新增 `Core/Enemies/EnemyAction.cs`：`Effects` 只读列表，构造时拷贝；null 抛 `ArgumentNullException`，含 `Draw` 抛 `ArgumentException`，含 `Value <= 0` 抛 `ArgumentException`；空列表允许。
- [ ] 新增 `Core/Enemies/EnemyDefinition.cs`：`Id` / `DisplayName` / `MaxHp` / `Actions`；id、displayName 空抛，`maxHp <= 0` 抛 `ArgumentOutOfRangeException`，actions 空或含 null 抛；构造时拷贝列表、元素引用不变。不建 `EnemyIds` / `EnemyFactory`。
- [ ] `BattleSetup` 去掉 `EnemyMaxHp` / `EnemyDamage`（连同两条校验），只剩 `PlayerMaxHp` / `EnergyPerTurn` / `HandSize`。
- [ ] `BattleSession`：构造函数改为 `(BattleSetup setup, EnemyDefinition enemy, IEnumerable<CardDefinition> deck, Random random, IEnumerable<RelicState> relics = null)`，`Enemy = new CombatantState(enemy.MaxHp)`；新增 `EnemyDefinition` / `CurrentEnemyAction` 只读属性与私有 `_enemyActionIndex`；删 `EnemyDamage`。
- [ ] `TryEndPlayerTurn` 改为：`DiscardHand` → `Phase = EnemyTurn` → **`Enemy.ClearBlock()`** → `TriggerTurnStartBuffs(Enemy)` → 毒死则 Victory 返回 → `foreach effect in CurrentEnemyAction.Effects: Enqueue(ToAction(effect, Enemy))` → `RunAll` → `AdvanceEnemyAction()`（末尾回绕）→ 玩家死则 Defeat 否则 `StartPlayerTurn()`。`ToAction` 一行不改。
- [ ] `PreviewEnemyAttack()` 签名不变，改为遍历 `CurrentEnemyAction.Effects` 里的 `Damage` 段各自过 `DamageCalculator` 后求和；无攻击段返回 0。更新注释里"配对"的说明。
- [ ] 测试迁移：`BattleSessionTests.CreateSession` 保留 `enemyMaxHp` / `enemyDamage` 参数名，内部经 `FixedAttacker(maxHp, damage)` 构造 `EnemyDefinition`（`damage == 0` → 空行动），新增可选 `EnemyDefinition enemy = null`；0.1～0.5 用例正文不动。**改 1 项 0.1 断言**：`PoisonedEnemy_LosesHpIgnoringBlockAtEnemyTurnStart_AndStackDecrements` 的 `Assert.AreEqual(10, session.Enemy.Block)` 改为 `0`，用例改名为 `PoisonedEnemy_LosesHpAtEnemyTurnStart_BlockClearedFirst_AndStackDecrements`，注释写明原因（技术设计第 5 节）。
- [ ] 新增 `EnemyDefinitionTests`（约 9 项：`EnemyAction` null / 含 Draw / 含 default / 空允许 / 拷贝；`EnemyDefinition` id 空 / displayName 空 / maxHp 0 / actions 空 / 拷贝且元素同引用）。
- [ ] 新增 `BattleSession/BattleSessionTests.Enemies.cs`（约 10 项，测试内定义 `JawWorm()` / `TwoHits(a, b)` 与一个"对任何 Buff +1"的 `IApplyBuffModifier` 替身）：构造 42 血；循环 `[0]→[1]→[2]→[0]→[1]`（`AreSame`）；咬预览 11 = HP 差；猛击预览 7 = HP 差、之后格挡 5、玩家攻击 6 只掉 1；猛击后不打牌结束回合，咆哮后格挡 6 不是 11；咆哮后力量 3、咬预览 14 = HP 差、再下一回合第二圈猛击预览 10 = HP 差（这条用 `playerMaxHp: 80` 构造，40 血撑不到第 5 回合）；2 层虚弱时咬 8；`TwoHits(3, 3)` 对 1 层易伤玩家预览 `4 + 4 = 8` = HP 差；`enemyDamage: 0` 的空行动预览 0、HP 不变；玩家持 +1 替身时颚虫咆哮力量仍 3。

**阶段门槛：** `Core.Enemies` 不引用 `UnityEngine` / `Core.Actions`；`ToAction`、`DamageCalculator`、三条 Rule 的 diff 为空；0.1～0.5 用例除 `CreateSession` 内部与那 1 项断言外不改仍通过；第 1 节完成约 222 项。

## 2. Data：`EnemyData` 与配置（约 2h）

- [ ] 新增 `Data/EnemyActionData.cs`（`[Serializable] struct`：`name` + `List<EffectSpecData> effects`；测试构造函数；`ToAction`；`TryValidate`：空允许、逐条 `EffectSpecData.TryValidate`、`Draw` 拒绝、`ApplyBuff && Opponent` 拒绝并在注释里写"0.7 删这一条"）。新增 `EnemyActionDataTests`（约 7 项：Damage + Block 通过 / ApplyBuff Self 通过 / 空通过 / Draw 拒 / ApplyBuff Opponent 拒 / 内层 value 0 拒并透传错误 / `ToAction` 条数一致）。
- [ ] 新增 `Data/EnemyData.cs`（SO：`id` / `displayName` / `maxHp` / `actions`；`ToDefinition`；`TryValidate` 错误信息带 `actions[i]` 与 `name`）。
- [ ] `BattleConfig`：删 `enemyMaxHp` / `enemyDamage` 字段与校验；加 `EnemyData enemy`（非空 + `TryValidate`）；`playerMaxHp` 默认 80；`ToSetup()` 改三参数；新增 `ToEnemyDefinition()`。
- [ ] 资产：`Assets/Data/Enemies/JawWorm.asset`（`jaw_worm` / 颚虫 / 42；咬 11；猛击 7 + 格挡 5；咆哮 力量 3 + 格挡 6，力量条放前面）与 `FixedAttacker.asset`（`fixed_attacker` / 固定攻击敌人 / 36；攻击 6）；`Default.asset` 改 `playerMaxHp: 80`、`enemy` 指向 JawWorm、删 `enemyMaxHp` / `enemyDamage` 两行。
- [ ] `BattleController.Awake` 改为五参数构造；新增 `EnemyDisplayName` / `CurrentEnemyAction`；删 `EnemyDamage`。
- [ ] 不写 `EnemyData` / `BattleConfig` 的 SO 单测（与 `CardData` / `RelicData` 既有做法一致），配错检查放到第 4 节联调。第 2 节完成约 229 项。

**阶段门槛：** 只改 Inspector 就能换敌人 / 加敌人；`BattleController` 里没有敌人数值。

## 3. 表现层（约 1h）

- [ ] 新增 `Views/IntentFormatter.cs`：`Format(EnemyAction action, int previewedAttackDamage)`——攻击段合成一项 `攻击 N`，`Block` → `防御`，`ApplyBuff(Self)` / `Heal` → `增益`，`ApplyBuff(Opponent)` → `减益`，按首次出现顺序去重、` · ` 连接，空行动 → `待机`；`Draw` 走 `ArgumentOutOfRangeException`（构造期已拦）。新增 `IntentFormatterTests`（约 8 项，见技术设计第 5 节）。
- [ ] `BattleView.Refresh`：敌人状态行前缀改为 `EnemyDisplayName`；意图行改为 `敌人意图：{IntentFormatter.Format(CurrentEnemyAction, PreviewEnemyAttack())}`。
- [ ] 场景不改。第 3 节完成约 237 项。

**阶段门槛：** 颚虫一局里意图依次显示 `攻击 11` → `攻击 7 · 防御` → `增益 · 防御` → `攻击 14` → `攻击 10 · 防御`（完整序列与数字以游戏设计第 6 节为准）；`Views` 里没有公式常数。

## 4. 联调与验收（约 1.5h）

- [ ] 完整运行一局颚虫：状态栏 `颚虫 HP 42/42`；意图序列如上；猛击后敌人格挡 5 顶住玩家一张 6 点攻击只掉 1；咆哮后敌人 Buff 行 `力量 3`、意图 `攻击 14`、结束回合掉 14；第二圈猛击意图 `攻击 10 · 防御`（力量永久）；给敌人上虚弱后咬显示 **6**——`Default.asset` 默认带纸鹤，8 是无遗物配置（测试）的数字，不要为了看到 8 去删默认遗物；对照游戏设计第 6 节逐条核对。
- [ ] `Default.asset` 换成 FixedAttacker 跑一局：每回合掉 6；0.5 的三件遗物表现不变（金刚杵 7 / 蛇颅骨 4 层 / 虚弱后纸鹤 3，无遗物才是 4）；换回颚虫。
- [ ] 故意配错各看一次 Console 后改回：颚虫某行动加一条 `Draw` → 启动报错；加一条 `ApplyBuff / Opponent / weak` → 启动报错；`Default.asset` 的 `enemy` 留空 → 报错；`EnemyData.id` 留空 → 报错；`maxHp` 填 0 → 报错。
- [ ] 运行全部 EditMode 测试；Console 无错误；grep 核对：`BattleSession.cs` 无 `jaw` / `fixed_attacker` / `EnemyData`，敌人相关符号只有 `EnemyDefinition` / `EnemyAction`；`Views` 无 `0.75` / `1.5`；`ToAction` / `DamageCalculator` / `*DamageRule.cs` 无改动。

**阶段门槛：** 游戏设计第 6 节全部满足。

## 5. 文档与交付（约 0.5h）

- [ ] 更新 `README.md`：目录说明加 `Core/Enemies`、`Data/EnemyData` / `EnemyActionData`、`Views/IntentFormatter`、`Assets/Data/Enemies`；"已完成"加 0.6 段；"范围外"去掉敌人行动表；运行方式改为颚虫与意图序列、玩家 80；测试数；已知限制里"`EnemyTurn` 是瞬时阶段"与"`BattleSession` 身兼多职"两段按 0.6 现状改写。
- [ ] 更新 [`../README.md`](../README.md) 的 0.6 状态：节标"（已完成）"、摘要表；0.1 遗留口子里"敌人只有一个固定伤害数字"划掉；0.x 完成定义里"0.1 的 79 项测试不改断言"补上 0.6 因敌人清格挡改了 1 项的例外。
- [ ] 更新 [`../../README.md`](../../README.md)（开发计划总览）：路线总览 0.6 标已完成与测试数；文档地图；第 2 节例外说明改为"0.7 的技术设计与 TODO 是草稿，进入前复核转正"。
- [ ] 进入 0.7 前：对照本版本实际实现复核 [`../0.7/技术设计.md`](../0.7/技术设计.md) 与 [`../0.7/TODO.md`](../0.7/TODO.md) 的草稿并转正（那两份文首列了复核清单）。
- [ ] 提交并推送 0.6（不带 `Assets/TextMesh Pro/Fonts/*.asset` 的 Play 抖动；分三次：Core 与测试迁移、Data 与表现层、文档）。

## 建议日程

| 开发日 | 当日交付 |
| --- | --- |
| Day 1 | 第 1 节：`Core.Enemies`、`BattleSetup` / `BattleSession` 改造、`CreateSession` 迁移与那 1 项断言 |
| Day 2 | 第 1 节余下的敌人用例 → 第 2 节：`EnemyActionData` / `EnemyData` / `BattleConfig` / 资产 / Controller |
| Day 3 | 第 3～4 节：`IntentFormatter`、`BattleView`、联调与配错检查 |
| Day 4 | 第 5 节：文档、0.7 草稿复核、提交（含缓冲） |

## 范围控制

开发中出现以下想法时，记录到 0.7 / 1.x 候选，不立即实现：

- "顺便让颚虫按原版随机、不能连续两次咬。"（需要第二个随机源与 `IEnemyPattern`，1.0 之后随 Run 种子一起考虑）
- "顺便让蓝奴隶贩子也进来，反正只是配一份资产。"（它给玩家上虚弱，没有 0.7 的减层那层虚弱永远挂着；`EnemyActionData` 的校验就是为了挡它）
- "顺便给 `EnemyAction` 加名字 / 意图种类字段进 Core。"（文案由效果推导；名字只在 Data 层给 Inspector 用）
- "顺便把敌人回合做成分段动画 / 异步。"（`EnemyTurn` 仍是瞬时阶段）
- "顺便加第三只敌人。"（一只原版怪 + 一份迁移配置已证明"敌人 = 数据"）
- "顺便做多敌人 / 目标选择。"（1.2+ 候选池）
- "顺便把测试里的玩家血量也改成 80。"（测试常量，所有 HP 断言相对它写；改了只是给自己找 diff）
- "为了不改 0.1 那项断言，把清格挡放到中毒之后。"（顺序会与玩家侧不一致；改断言才是对的）

只有阻止本版本验收的问题才进入当日任务。
