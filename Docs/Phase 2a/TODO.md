# Phase 2a TODO：ActionSystem + EffectSystem

> **预计 1.5～2 个有效开发日。本阶段是纯重构，不新增玩法，每步完成后跑一次全量 EditMode 测试确认行为未变。**

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。

## 0. 开始前检查（约 0.5h）

- [ ] Phase 1 分支干净，Unity Console 无错误，EditMode 测试全部通过。
- [ ] 确认本阶段不新增任何卡牌/数值，出现新想法记录到本文末尾"范围控制"，不立即加入。

**阶段门槛：** Phase 1 成果已提交，作为本阶段重构的对照基线。

## 1. 实现 ActionSystem 基础设施（约 3～4h）

- [ ] 实现 `IAction`、`ActionContext`、`ActionQueue`（入队、`RunAll`、执行中可继续入队）。
- [ ] 实现 `DamageAction`、`BlockAction`、`HealAction`。
- [ ] 编写 `ActionQueue` 的 EditMode 测试：按序执行、执行中追加的 Action 会被处理完。

**阶段门槛：** `Core.Actions` 不引用 `UnityEngine`，独立于 `BattleSession` 可单独测试。

## 2. 实现 EffectSystem（约 1～2h）

- [ ] 实现 `DamageEffect`、`BlockEffect`、`HealEffect`，转发到 `CombatantState` 现有方法。
- [ ] 编写对应 EditMode 测试（伤害/格挡/治疗的边界行为已在 `CombatantStateTests` 覆盖，这里只需验证 Effect 正确转发）。

**阶段门槛：** `Core.Effects` 不引用 `Core.Actions`、`Core.Battle`。

## 3. 迁移 BattleSession（约 2～3h）

- [ ] `BattleSession` 新增 `ActionQueue` 字段，构造时创建，生命周期贯穿整场战斗。
- [ ] 把 `ResolveCard` 的 `switch(CardType)` 直接结算，改成 `EnqueueCardAction` 入队 + `RunAll` 执行。
- [ ] 确认 `TryPlayCard` 的其余逻辑（能量扣除、进弃牌堆、胜利判定）顺序不变。

**阶段门槛：** 不看内部实现，仅从 `BattleSession` 的公开行为看不出任何变化。

## 4. 回归测试（约 1h）

- [ ] 运行全部 `BattleSessionTests`，确认攻击/防御/治疗、能量校验、胜负判定等测试无需修改断言即可通过。
- [ ] 确认 Unity Console 无错误。

**阶段门槛：** 满足游戏设计文档第 5 节的全部验收标准。

## 5. 文档与交付（约 0.5h）

- [ ] 更新 `README.md`：如果目录结构章节需要补充 `Core/Actions`、`Core/Effects`。
- [ ] 提交并推送 Phase 2a 成果。

## 建议日程

| 开发日 | 当日交付 |
| --- | --- |
| Day 1 | ActionSystem + EffectSystem 基础设施与单元测试 |
| Day 2 | `BattleSession` 迁移、回归测试、文档提交 |

## 范围控制

开发中出现以下想法时，记录到 Phase 2b/2c 候选清单，不立即实现：

- "顺便把 Action 做成可组合的。"
- "给 Effect 加一个通用接口方便以后扩展。"
- "先把 Buff 触发的挂载点也做出来。"

只有阻止本阶段验收的问题才进入当日任务。
