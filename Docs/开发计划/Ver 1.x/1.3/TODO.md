# 1.3 TODO：战后三选一

> **预计 2～2.5 个有效开发日。** 风险：Victory 仍按 1.2 直接 `ShowMap` / `ShowResult`（跳过奖励页）；等待选奖励时 `EnterCurrentNode` 仍 `Begin`；把奖励接到 `BattleInput.Random`；给 `NodeType` 加 `Reward`。

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。

## 0. 开始前检查与复核

- [x] 1.2 已提交推送；对照技术设计第 9 节：`BattleInput` 五字段、`ApplyResult` 必须先发输入、`AddCard` 通关后仍允许、遗物字典合同、`NodeType` 只有 `Combat`。
- [x] 奖励种子 **[已定]**：`g = unchecked(runSeed * 389 ^ rewardIndex)`；`rewardIndex = 写回后 NodeIndex - 1`；抽样与 `CardPile` 同形 Fisher-Yates 取前 3。
- [x] 奖励池挂在 `RunConfig`，不进 `RunState` 构造。产品拒绝 Id `Attack` / `Defend`；Core 抽取不按卡名过滤。
- [x] `BattleController.Begin` / `DiscardSession` / `SetRelicDisplayNames` 相对 1.2 不改合同。

**阶段门槛：** 技术设计第 7 节无未拍的 **[待确认]**。**可以进第 1 节。**

## 1. 抽取与流程（约 3.5h）

- [x] `RunState.CreateRewardChoices`：按技术设计 4.1 / 4.2。构造签名不变。`CreateBattleInput` / `ApplyResult` / `AddCard` 语义相对 1.0 不变。**不要抄 `CreateBattleInput` 的 `if (IsOver)`**——第三场写回后 `IsOver` 已为 true，这时必须还能抽。
- [x] 测试：现有 `RunStateTests.cs` 加技术设计第 5 节的抽取项（含战斗 `Random.Next` 不改变候选、抽取不改变下一场战斗种子、**`IsCleared` 后仍能抽**、`NodeIndex == 0` / `IsFailed` 抛）。**不要重写 1.0 的 `IsCleared` / 三场写回 / `AddCard` 用例。** 不要往 EditMode 根目录加文件。这条 `IsCleared` 抽取不要省略。
- [x] `RunConfig`：`rewardPool` 字段、`RewardPool` getter、`TryValidate` 按 4.5（不足 3 张、重复、`Attack` / `Defend` 拒）。`CreateRunState` 仍不接收奖励池。生命 / 能量 / 手牌仍不写 80 / 3 / 5。
- [x] `Default.asset` 配齐 9 张可获得卡（痛击 / 双击 / 剑柄打击 / 坚不可摧 / 力量强化 / 剧毒 / 虚弱 / 易伤 / 治疗），不含打击 / 防御。
- [x] `RunController.ApplyOutcomeIfEnded`：Victory **无论是否通关** 都先 `ApplyResult`，再 `DiscardSession`（不清字典），再 `ShowReward`。**禁止 `Begin`，禁止在这里 `ShowMap` / `ShowResult`。** 失败仍 Result，不经奖励页。失败只认 `Phase == Defeat`。
- [x] `RewardChoices`：公开只读列表；未等待时为空列表，不要 null。`ShowReward` 赋值 `CreateRewardChoices`；`SelectReward` / `SkipReward` / `Restart` 清成空。`EnterCurrentNode` 在 `RewardChoices.Count != 0` 时 return。不要另做 `_awaitingReward` 再和缓存打架。Play 点不到隐藏的地图进入，不能当这道早退的锁。
- [x] `SelectReward` / `SkipReward`：按 4.4。产品侧 `AddCard` 只出现在 `SelectReward`（读 `RewardChoices[index]`）。选完 / 跳过：未通关 `ShowMap`，通关 `ShowResult`（这时才 `SetOutcome`）。`ShowReward` / `ShowMap` / `SelectReward` / `SkipReward` 都要能刷到顶栏（`RefreshShell`）。
- [x] `SetPages` 五页；`Restart` 回 Start 时奖励页隐藏。没有 `Update`。产品侧 `battleController.Begin` 只出现在 `EnterCurrentNode`。
- [x] `TestScene` **本节**加默认隐藏的 `RewardPage` 并接到 `rewardPage` / `rewardPageView`；**跳过按钮本节就挂上**（可以先没文案）。三张卡槽仍第 2 节；`Refresh` 在槽未接线时跳过。`RewardPageView.Start`：`skipButton == null` 则 return。不要等第 2 节才建物体，否则 `ShowReward` 没有对象。

**阶段门槛：** 产品侧 Victory 必须能看见奖励页（可以先是空页）；等待奖励时不能 `Begin` 下一场。

## 2. 奖励 UI（约 2.5h）

- [x] `CardTypeDisplayNames.Of`：攻击 / 技能 / 能力。`BattleView` 删私有 `TypeLabel`，手牌改走它。文案与现网三字一致。
- [x] 测试：3 条文案写进现有 `CardDescriptionFormatterTests.cs`，不新增根目录文件。
- [x] `RewardPageView`：三个 `CardButtonView` + 跳过按钮。卡面三行，对齐 `BattleView.RefreshHand`：`名字`、`类型 · 费用 X`、无委托 `Format`；不要拼成一行。只调 `SelectReward` / `SkipReward`；`Refresh` 只读 `RewardChoices`。绑定跟 `MapPageView` 一样走 `Start`。
- [x] `RewardPage` 与 Combat / Map / Result 一样给顶栏留 inset。第 1 节已建物体并默认隐藏，跳过按钮已挂；本节只铺三张卡槽。
- [x] 顶栏：奖励页读 Run（`!IsReady`）。选一张后顶栏牌组张数 +1 再切页。Start 仍不读 `RunConfig`。战斗中生命仍跟局内。不要另写一套顶栏刷新。

**阶段门槛：** 打完一场能看见三张不同的可获得卡，选或跳过后能回到地图；第三场选完去结束页。

## 3. 联调与验收（约 1.5h）

- [x] Play：对照游戏设计第 4 节与总设计第 8 节。每场胜利都经奖励页；跳过张数不变；第一场选的卡第二场能抽到；第二场开局生命 = 第一场结束生命；第三场选完去 Result（摘要含新卡）；**第三场点跳过仍进通关结束页，摘要张数不含新卡**（停在奖励页则 1.0 测试仍会绿）；中途死亡不经奖励页；再来一局后 `遗物：无`；奖励池漏配进不了局。场间若仍直接 `ShowMap` / 通关仍直接 Result，1.0 测试仍会绿，靠这一条 Play 锁。
- [x] 不要重写 1.0 的 `IsCleared` / `AddCard` 用例，也不要补「Victory 未通关自动 `ShowMap`」的 1.2 复述。
- [x] grep：`RunController.cs` 无 `Update`；`battleController.Begin` 只在 `EnterCurrentNode`；`EnterCurrentNode` 早退能看到 `RewardChoices`；`ApplyResult` 只在 `ApplyOutcomeIfEnded`；产品侧 `AddCard` 只在 `SelectReward`；`NodeType.cs` 仍只有 `Combat`；`Core/Battle/`、`BattleInput.cs`、`BattleSetup` 无奖励类型；`BattleView` 不调 `battleController.PlayCard` / `EndTurn`；`Begin` / `DiscardSession` 不碰 `_relicDisplayNames`。
- [x] 全部 EditMode；0.x～1.2 断言未改。

## 4. 文档与交付（约 0.5h）

- [ ] 更新 [`../../../../README.md`](../../../../README.md)：当前阶段改为 1.3 已完成、下一步 1.4（可选）；运行方式改为开始页 → 地图 → 三场（场间经奖励页回地图，第三场经奖励页去结束页）；短 Run 完成定义已勾。
- [ ] 更新 [`../README.md`](../README.md)：1.3 节标「（已完成）」并补摘要表；总设计第 8 节可勾。
- [ ] 更新 [`../../README.md`](../../README.md)：路线总览 1.3 标已完成；文档地图；第 2 节改为下一个 1.4（草稿转正）、下下个按细化规则（1.4 之后进入候选池 / 演示前清单，不再写下一个小版本三件套，除非用户指定）。
- [ ] 勾 [`../游戏设计.md`](../游戏设计.md) 第 8 节。进入 1.4 前按细化规则复核 [`../1.4/`](../1.4/) 草稿第 9 节：1.3 的 `CreateRewardChoices` 仍在 `Core.Run`、无 `UnityEngine`；不要代改 1.4 接口。
- [ ] 提交。

## 范围控制

- `Core.asmdef` → 1.4（可选）
- 遗物奖 / 金币奖 / 升级 / 移除 / 稀有度 → 否（候选池或原版细节，本版不做）
- 空商店 / 休息 / 事件页、给 `NodeType` 加 `Reward` 或其它值 → 否
- EventBus、拆场景、Addressables、抽 `RunLoop` → 否
- 奖励接到 `BattleInput.Random` / `UnityEngine.Random` → 否
- `Update` / 协程里读 `Phase` 或 `ApplyResult` → 否
- 通关 Victory 仍直接 Result、未通关仍直接 `ShowMap`、等待奖励时仍 `Begin` → 否
- `RunState` 构造加奖励池参数、改 Session 构造 → 否
- 奖励卡面走预览委托、把三个字段拼成一行、为奖励页预填 80 / 10 → 否
- `Refresh(cards)` 当第二出口、View 自己调 `CreateRewardChoices`、另做 `_awaitingReward` 和 `RewardChoices` 两套等待标志 → 否

只有阻止本版本验收的问题才进入当日任务。
