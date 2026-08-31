# Phase 2c TODO：RuleSystem（伤害计算规则）

> **预计 2～3 个有效开发日。敌人固定攻击迁移到 Action 管线是本阶段唯一一处"修改已有行为路径"的改动，务必用回归测试锁定数值不变。**

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。

## 0. 开始前检查（约 0.5h）

- [ ] Phase 2b 分支干净，Unity Console 无错误，EditMode 测试全部通过。

**阶段门槛：** Phase 2b 的 BuffSystem（力量/中毒）已就绪，作为本阶段规则的数据来源。

## 1. 实现 RuleSystem 基础设施（约 3～4h）

- [ ] 实现 `DamageContext`（`double Damage`、`Source`、`Target`）。
- [ ] 实现 `IDamageRule`。
- [ ] 实现 `StrengthDamageRule`、`WeakDamageRule`、`VulnerableDamageRule`。
- [ ] 实现 `DamageCalculator`（固定加法阶段在前、乘法阶段在后，最后 `Math.Floor` 一次）。
- [ ] 编写三条规则单独的 EditMode 测试，以及 `DamageCalculator` 组合场景测试。

**阶段门槛：** `Core.Rules` 不引用 `UnityEngine`、`Core.Actions`、`Core.Effects`。

## 2. 改造 DamageAction 与 BattleSession（约 2～3h）

- [ ] `DamageAction` 新增 `source` 字段，`Execute` 内部改为构造 `DamageContext` → `DamageCalculator.CalculateFinalDamage` → `DamageEffect.Apply`。
- [ ] `EnqueueCardAction` 的 `Attack` 分支传入 `Player` 作为 source。
- [ ] 敌人固定攻击从直接调用 `Player.TakeDamage` 改为 `_actionQueue.Enqueue(new DamageAction(Enemy, Player, EnemyDamage))` + `RunAll`。
- [ ] 回归测试：敌人固定攻击在没有任何 Buff 时，玩家扣血数值与 Phase 1 完全一致。

**阶段门槛：** 玩家和敌人的攻击伤害走同一套计算路径，无特殊分支。

## 3. 接入 ScriptableObject 配置（约 1h）

- [ ] 创建虚弱药剂、易伤药剂两份 `CardData`（费用 1，数值均为 2）。
- [ ] 把两张新卡加入默认 `BattleConfig` 牌组，扩充为 14 张。

**阶段门槛：** 不改代码，仅通过 Inspector 配置即可让新卡牌出现在战斗中。

## 4. 联调与验收（约 2h）

- [ ] 完整运行一次：玩家对敌人施加易伤后打攻击牌，确认最终伤害符合公式。
- [ ] 完整运行一次：玩家自己有虚弱时打攻击牌，确认最终伤害按 0.75 倍下调。
- [ ] 完整运行一次：玩家自己有力量时打攻击牌，确认最终伤害按层数上调。
- [ ] 构造三条规则同时生效的场景，核对结算顺序和最终取整结果与设计文档公式一致。
- [ ] 确认中毒伤害数值不受本阶段改动影响。
- [ ] 运行全部 EditMode 测试，确认 Phase 1/2a/2b 遗留测试无需修改断言即可通过。
- [ ] 确认 Unity Console 无错误。

**阶段门槛：** 满足游戏设计文档第 6 节的全部验收标准。

## 5. 文档与交付（约 0.5h）

- [ ] 更新 `README.md`：牌组数量、`Core/Rules` 目录说明、MVP 完成标准勾选情况。
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
