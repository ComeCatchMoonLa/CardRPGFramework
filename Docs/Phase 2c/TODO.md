# Phase 2c TODO：RuleSystem（伤害计算规则）

> **预计 2～3 个有效开发日。敌人固定攻击迁移到 Action 管线是本阶段唯一一处"修改已有行为路径"的改动，务必用回归测试锁定"敌人无 Buff 时"数值不变——打出虚弱后敌人攻击从 6 变 4 是虚弱卡的正确效果，不是回归失败。**

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。

## 0. 开始前检查（约 0.5h）

- [x] Phase 2b 分支干净，Unity Console 无错误，EditMode 测试全部通过。

**阶段门槛：** Phase 2b 的 BuffSystem（力量/中毒）已就绪，作为本阶段规则的数据来源。

## 1. 实现 RuleSystem 基础设施（约 3～4h）

- [x] 实现 `DamageContext`（`double Damage`、`Source`、`Target`）。
- [x] 实现 `IDamageRule`。
- [x] 实现 `StrengthDamageRule`、`WeakDamageRule`、`VulnerableDamageRule`。
- [x] 实现 `DamageCalculator`（固定加法阶段在前、乘法阶段在后，最后 `Math.Floor` 一次）。
- [x] 编写三条规则单独的 EditMode 测试，以及 `DamageCalculator` 组合场景测试。

**阶段门槛：** `Core.Rules` 不引用 `UnityEngine`、`Core.Actions`、`Core.Effects`。

## 2. 改造 DamageAction 与 BattleSession（约 2～3h）

- [x] `DamageAction` 新增 `source` 字段，`Execute` 内部改为构造 `DamageContext` → `DamageCalculator.CalculateFinalDamage` → `DamageEffect.Apply`；既有 `ActionQueueTests` 里的两参数构造改为三参数，断言数值不变。
- [x] `EnqueueCardAction` 的 `Attack` 分支传入 `Player` 作为 source。
- [x] 敌人固定攻击从直接调用 `Player.TakeDamage` 改为 `_actionQueue.Enqueue(new DamageAction(Enemy, Player, EnemyDamage))` + `RunAll`，位置保持在 `TriggerTurnStartBuffs(Enemy)` 与 `Enemy.IsDead` 胜利判定之后，与中毒结算是两次独立的 `RunAll`。
- [x] 回归测试：敌人固定攻击在没有任何 Buff 时，玩家扣血数值与 Phase 1 完全一致；Phase 2b 的毒杀胜利测试仍然通过。

**阶段门槛：** 玩家和敌人的攻击伤害走同一套计算路径，无特殊分支。

## 3. 接入卡牌与 ScriptableObject 配置（约 1.5h）

- [x] 实现 `WeakBuff`、`VulnerableBuff`（与 `StrengthBuff` 同形，无触发；Id 分别为 `"weak"`、`"vulnerable"`，必须与规则里读取的字符串一致）。（Step 1 时已提前实现，见该步说明）
- [x] `CardType` 新增 `Weak`、`Vulnerable`；`EnqueueCardAction` 新增对应分支，两张卡的目标都是 `Enemy`（不要照力量卡抄成给自己）。
- [x] 创建虚弱、易伤两份 `CardData`（费用 1，数值均为 2；命名沿用 Phase 2b 约定，不带"药剂"）。
- [x] 把两张新卡加入默认 `BattleConfig` 牌组，扩充为 14 张；Play 验收时若临时改过牌组，提交前恢复为 14 张。

**阶段门槛：** 打出虚弱/易伤后敌人身上出现对应 Buff（`Enemy.GetBuffStacks` 断言）；代码接线完成后，新卡只靠配置即可进入战斗。

## 4. 联调与验收（约 2h）

- [x] 完整运行一次：玩家对敌人施加易伤后打攻击牌，确认最终伤害符合公式（6 → 9）。
- [x] 完整运行一次：玩家对敌人施加虚弱后结束回合，确认敌人攻击从 6 降为 4。
- [x] 完整运行一次：玩家自己有力量时打攻击牌，确认最终伤害按层数上调。
- [x] 三条规则同时生效的场景用 EditMode 测试构造（对局里没有给玩家上虚弱的来源），核对取整用例 `5 + 虚弱 + 易伤 = 5`（中间取整会得 4）。（Step 1 已写：`DamageRuleTests.Calculator_WeakAndVulnerable_FloorsOnceAtEnd`；三条规则一起的版本见 `Calculator_AllThreeRules_MatchesDesignFormula`，不需要再补）
- [x] 确认中毒伤害数值不受本阶段改动影响。
- [x] 运行全部 EditMode 测试，确认 Phase 1/2a/2b 遗留测试无需修改断言即可通过。
- [x] 确认 Unity Console 无错误。

**阶段门槛：** 满足游戏设计文档第 6 节的全部验收标准。

## 5. 文档与交付（约 0.5h）

- [x] 更新 `README.md`：牌组数量、`Core/Rules` 目录说明、MVP 完成标准勾选情况。
- [ ] 提交并推送 Phase 2c 成果，Phase 2（2a+2b+2c）全部完成。

## 建议日程

| 开发日 | 当日交付 |
| --- | --- |
| Day 1 | RuleSystem 基础设施与单元测试 |
| Day 2 | `DamageAction`/`BattleSession` 改造、ScriptableObject 配置 |
| Day 3 | 联调、验收、文档提交 |

## 范围控制

开发中出现以下想法时，记录到 Phase 3 候选清单，不立即实现：

- "顺便加暴击规则。"
- "顺便把规则做成配置驱动。"
- "顺便给敌人也设计几个技能用上这些 Buff。"（这属于敌人 AI/意图系统，不在 RuleSystem 范围内）

只有阻止本阶段验收的问题才进入当日任务。
