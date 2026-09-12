# 1.0 TODO：`RunState`

> **预计 1～2 个有效开发日。** 本版无 UI。风险：把 `CreateBattleInput` 写成返回 `BattleSession`，或把 `ApplyResult` 第二次当成 no-op（`nodeIndex` 会 +2）。验收时对照技术设计第 8 节。
>
> 需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。接口形状已标 **[已定]**（`BattleInput` 五字段、`ApplyResult` 拒绝语义、`AddCard` 通关后仍允许、`BattleSetup` 三参/四参重载、战斗种子公式），实现时按已定执行。

## 0. 开始前检查（约 0.5h）

- [ ] 0.7 已提交推送，工作区干净，Unity Console 无错误，263 项 EditMode 全绿。
- [ ] 读过技术设计第 4.1 / 4.2 / 4.3 / 4.4 / 7 节的 [已定]：`BattleInput` 五字段、`AddCard` 通关后仍允许、`ApplyResult` 拒绝、`BattleSetup` 两个重载、种子公式、`CreateBattleInput` 不洗牌。
- [ ] 确认 grep 范围：只扫 `Core/Run/` 与本刀新增 Runtime 文件，**不是**全 `Runtime`。`BattleController` 那一处不算本刀。

**阶段门槛：** 有干净的回归基线。

## 1. 残血开战（约 1～1.5h）

与第 2 节可同一提交，但先改 Setup / `CombatantState` / Session，0.x 测试才能继续绿。

- [ ] `BattleSetup`：三参重载当前生命 = `playerMaxHp`；四参校验 `current > 0` 且 `≤ max`（`0` / 负值 / `> max` 抛）。不要用 `< 0` 当未传哨兵。
- [ ] `CombatantState`：保留单参满血；新增 `(maxHp, currentHp)`，校验同上。
- [ ] `BattleSession` 构造：`Player = new CombatantState(setup.PlayerMaxHp, setup.PlayerCurrentHp)`。其余签名不变。
- [ ] 测试：`CombatantStateTests` 残血 / 越界 / 单参满血；`BattleSetup` 三参满血、四参残血、四参越界（同文件或 `BattleSessionTests`，不新增根目录文件）；`BattleSessionTests` 残血开战 `CurrentHp == 20`。跑全部 0.x 用例，**不改断言**。

**阶段门槛：** 0.x 263 项不改断言仍绿；`new BattleSetup(hp, energy, hand)` 与 `BattleConfig.ToSetup()` 不用改调用点。

## 2. `Core.Run`（约 3h）

- [ ] 新增 `Core/Run/BattleInput.cs`、`Core/Run/RunState.cs`（命名空间 `CardRPGFramework.Core.Run`）。不引用 `UnityEngine` / `Core.Actions`。
- [ ] `RunState` 构造：5 参数（`runSeed, playerSetup, deck, relicIds, encounters`）；列表浅拷贝；`encounters` 恰好 3 条；`relicIds` / `deck` 允许空列表、null 抛；遗物 Id 无重复且 `RelicFactory.IsKnown`；`Gold` 恒 0。
- [ ] `CreateBattleInput`：按技术设计第 4.2 节组五个字段；`Random` 所有权交给调用方；`f = unchecked(runSeed * 397 ^ nodeIndex)`；`IsOver` 抛。同一节点多次调用不推进。**不**调用 `Random.Next`、**不**洗返回牌组。
- [ ] `ApplyResult`：按技术设计第 4.4 节拒绝语义实现（必须先 `CreateBattleInput`、每节点一次、失败不写生命、胜利才推进、`remainingHp <= 0` 或 `> MaxHp` 抛）。通关用 `_encounters.Count`，不要写 `== 3`。
- [ ] `AddCard`：null 抛；`IsFailed` 抛；`IsCleared` 后仍允许追加。不要写成 `IsOver` 就抛。
- [ ] 测试：新建 `Tests/EditMode/Run/RunStateTests.cs`（不要往 EditMode 根目录加第 16 个文件）。覆盖技术设计第 5 节全部条目，含：快照与 Run 解耦、残血第二场、`AddCard`、消耗不写回、多种子稳定、牌组两次输入顺序相同（未洗）、`ApplyResult` 重复 / 未发输入 / 胜利+0 血 / `remainingHp > MaxHp`、**三场胜利后 `AddCard` 成功且再 `CreateBattleInput` 仍抛**、失败后 `AddCard` 抛、`relicIds: []` 通过 / `null` 抛。消耗不写回用例允许测试文件 `new BattleSession`（测试辅助，不算产品路径）。

**阶段门槛：** 游戏设计第 4 节验收有用例通过；`Core/Run/` 里 grep 不到 `new BattleSession`。

## 3. 联调与验收（约 0.5h）

- [ ] 跑全部 EditMode；0.x 断言未改。
- [ ] grep：`Assets/Scripts/Runtime/Core/Run/` 无 `new BattleSession`；本刀新增的其它 Runtime 文件同样没有。`BattleController.cs` 仍有一处，这是预期。
- [ ] `BattleSession` 构造仍是 `(setup, enemy, deck, random, relics = null)`。

**阶段门槛：** 技术设计第 8 节全部满足。

## 4. 文档（约 0.5h）

- [ ] 本版 TODO 勾完。1.x README 的 1.0 节标「（已完成）」只在实现提交时做，本次文档落盘不勾。
- [ ] 进入 1.1 前按细化规则复核 [`../1.1/技术设计.md`](../1.1/技术设计.md) 草稿。

## 范围控制

开发中出现以下想法时，记录到 1.x 候选或后续小版本，不立即实现：

- "顺便做 `RunConfig` SO，测试也好配。" → 1.1，且与 `BattleConfig` 取舍还是 **[待确认]**
- "顺便把 `BattleController.Awake` 改成 `Begin`。" → 1.1
- "顺便加 `NodeType`，反正要三个敌人。" → 1.2
- "顺便做奖励三选一，`AddCard` 都有了。" → 1.3
- "第二次 `ApplyResult` 当 no-op 比较温和。" → 否，见技术设计第 4.4 节
- "通关后 `AddCard` 也禁掉，状态机更干净。" → 否，第三场胜利先写回，1.3 还要加卡；见技术设计第 4.3 节
- 全 `Runtime` 禁 `new BattleSession` → 否，会让 1.0 验收误红

只有阻止本版本验收的问题才进入当日任务。
