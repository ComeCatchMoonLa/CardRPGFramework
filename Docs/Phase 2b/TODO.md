# Phase 2b TODO：BuffSystem（力量与中毒）

> **预计 3～4 个有效开发日（本阶段是三个子阶段里唯一同时改动 `CombatantState`/`Actions`/`BattleSession`/`CardData`/`BattleConfig`/测试六处的阶段，比 2a/2c 多留一天缓冲）。中毒的回合开始触发和死亡判定时机是本阶段最容易出错的地方，务必按 [`技术设计.md`](技术设计.md) 第 5 节的插入位置逐字核对。**

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。

## 0. 开始前检查（约 0.5h）

- [ ] Phase 2a 分支干净，Unity Console 无错误，EditMode 测试全部通过。

**阶段门槛：** Phase 2a 的 `ActionQueue`/`Core.Effects` 已就绪，作为本阶段基础设施。

## 1. 实现 BuffSystem 基础设施（约 3～4h）

- [ ] 实现 `BuffState` 抽象基类（Id、Stacks、AddStacks、RemoveStacks）。
- [ ] 实现 `IBuffTrigger`（`OnTurnStart`）。
- [ ] 实现 `StrengthBuff`（无触发）、`PoisonBuff`（实现 `IBuffTrigger`，层数为 0 不触发，触发后层数减 1）。
- [ ] `CombatantState` 新增 Buff 容器：`ApplyBuff`、`GetBuffStacks`、`HasBuff`、`Buffs` 只读属性、`LoseHp`（忽略格挡；同步把 `TakeDamage` 改为扣完格挡后剩余伤害调用 `LoseHp`，外部行为不变，理由见技术设计第 4 节）。
- [ ] 编写 `BuffState`/`PoisonBuff`/`CombatantState` 新增成员的 EditMode 测试。

**阶段门槛：** `Core.Buffs` 不引用 `UnityEngine`，独立于 `BattleSession` 可单独测试。

## 2. 实现新增 Action / Effect（约 1～2h）

- [ ] 实现 `BuffEffect`、`HpLossEffect`。
- [ ] 实现 `ApplyBuffAction`、`HpLossAction`。
- [ ] 编写对应 EditMode 测试。

**阶段门槛：** `HpLossAction` 造成的伤害不受目标格挡影响，有测试证明。

## 3. 接入 BattleSession（约 3～4h）

- [ ] 新增 `TriggerTurnStartBuffs(CombatantState owner)`，遍历 `owner.Buffs`，对实现 `IBuffTrigger` 的 Buff 调用 `OnTurnStart`，随后 `RunAll`。
- [ ] `StartPlayerTurn`：`ClearBlock` 之后、恢复能量之前调用 `TriggerTurnStartBuffs(Player)`，之后检查 `Player.IsDead` 并处理战斗失败分支。
- [ ] `TryEndPlayerTurn`：进入 `EnemyTurn` 后、`Player.TakeDamage` 之前调用 `TriggerTurnStartBuffs(Enemy)`，之后检查 `Enemy.IsDead` 并处理战斗胜利分支（跳过敌人攻击）。
- [ ] `CardType` 新增 `Strength`、`Poison`；`EnqueueCardAction` 新增对应分支。

**阶段门槛：** 中毒把敌人打死时，敌人不会再发起攻击，战斗立即以胜利结束。

## 4. 接入 ScriptableObject 配置（约 1～2h）

- [ ] 创建力量药剂、剧毒药剂两份 `CardData`（费用 1，数值分别为 2、3）。
- [ ] 把两张新卡加入默认 `BattleConfig` 牌组，扩充为 12 张。

**阶段门槛：** 不改代码，仅通过 Inspector 配置即可让新卡牌出现在战斗中。

## 5. 联调与验收（约 2h）

- [ ] 完整运行一次：打出力量卡后确认内部层数增加（可临时加日志或断点观察，验收后移除）。
- [ ] 完整运行一次：打出剧毒卡，观察敌人在下一次回合开始时按层数掉血、格挡不吸收。
- [ ] 构造一次中毒致死场景，确认战斗以胜利结束且敌人未攻击。
- [ ] 运行全部 EditMode 测试，确认 Phase 1/2a 遗留测试无需修改断言即可通过。
- [ ] 确认 Unity Console 无错误。

**阶段门槛：** 满足游戏设计文档第 6 节的全部验收标准。

## 6. 文档与交付（约 0.5h）

- [ ] 更新 `README.md`：牌组数量、`Core/Buffs` 目录说明。
- [ ] 提交并推送 Phase 2b 成果。

## 建议日程

| 开发日 | 当日交付 |
| --- | --- |
| Day 1 | BuffSystem 基础设施、新增 Action/Effect 与单元测试 |
| Day 2 | `BattleSession` 接入、ScriptableObject 配置 |
| Day 3 | 联调、验收、文档提交 |
| Day 4 | 仅作为问题修复缓冲，不追加需求 |

## 范围控制

开发中出现以下想法时，记录到 Phase 2c 或更后候选清单，不立即实现：

- "顺便给力量加一个消耗层数的机制。"
- "顺便做一个 Buff 图标 UI。"
- "先把虚弱、易伤也加进来。"（这两个属于 Phase 2c，依赖本阶段的容器但不在本阶段实现）

只有阻止本阶段验收的问题才进入当日任务。
