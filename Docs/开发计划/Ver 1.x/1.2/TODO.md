# 1.2 TODO：地图与节点分发

> **预计 1.5～2 个有效开发日。** 风险：`ApplyOutcomeIfEnded` 或 `StartRun` 仍 `Begin`（跳过地图）；回地图不 `DiscardSession`（`IsReady` 挡住进入）；把 `NodeType` 塞进 Session。

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。

## 0. 开始前检查与复核

- [x] 1.1 已提交推送；对照技术设计第 9 节：`BattleInput` 五字段、`ApplyResult` 必须先发输入、遗物字典合同。
- [x] `NodeType` **[已定]**：枚举只有 `Combat`；`RunState` 构造不变；不预声明 Shop / Rest / Event。
- [x] 地图进入按总设计第 6 节：**三个点只展示 + 一个进入按钮**（游戏设计已按此收口）。
- [x] `BattleController.Begin` / `DiscardSession` / `RunConfig` 相对 1.1 不改合同。

**阶段门槛：** 技术设计第 7 节无未拍的 **[待确认]**。**可以进第 1 节。**

## 1. `NodeType` 与流程（约 3h）

- [x] `Core/Run/NodeType.cs`：枚举只有 `Combat`。不要把枚举放到 `Controllers` / `Views`。
- [x] `RunState`：`NodeTypes` / `CurrentNodeType`（`IsOver` 抛）。构造签名不变。`CreateBattleInput` / `ApplyResult` / `AddCard` 语义相对 1.0 不变。
- [x] 测试：现有 `RunStateTests.cs` 加技术设计 4.2 的 NodeType 项（含 `IsFailed` 后 `CurrentNodeType` 抛）。**不要重写 1.0 的 `IsCleared` / 三场写回用例。** 不要往 EditMode 根目录加文件。
- [x] `RunController.StartRun`：校验 → `CreateRunState` → `SetRelicDisplayNames` → **`ShowMap`**。不 `CreateBattleInput`、不 `Begin`。
- [x] `EnterCurrentNode`：`Run == null` / `IsOver` / `IsReady` 则 return。`switch CurrentNodeType`：`Combat` → `CreateBattleInput` → `Begin` → 显示 Combat → `battleView.Refresh` → `RefreshShell`；未实现 → `LogError` 留在地图。禁止 `if (true)` 直接开战。
- [x] `ApplyOutcomeIfEnded`：Victory 未通关改为 **先 `ApplyResult`，再 `DiscardSession`（不清字典），再 `ShowMap`**。**禁止 `Begin`。** 通关 / 失败仍 Result，失败不回地图。失败只认 `Phase == Defeat`。
- [x] `SetPages` 四页；`Restart` 回 Start 时地图隐藏。没有 `Update`。产品侧 `battleController.Begin` 只出现在 `EnterCurrentNode`。
- [x] `TestScene` **本节**加默认隐藏的 `MapPage` 并接到 `mapPage` / `mapPageView`（1.1 的 Combat / Result 也是第 1 节就藏）。点和进入按钮仍第 2 节；`Refresh` 在点未接线时跳过。不要等第 2 节才建物体，否则 `ShowMap` 没有对象。

**阶段门槛：** 产品侧 `Begin` 只剩进入当前战斗节点；场间胜利必须能看见地图（可以先是空页）。

## 2. 地图 UI（约 2h）

- [ ] `MapPageView`：三个点（已完成灰 / 当前高亮 / 未到暗）+ 一条线 + 一个进入按钮。圆点不是按钮。只调 `EnterCurrentNode`。不印敌人名。绑定跟 `StartPageView` 一样走 `Start`。
- [ ] `MapPage` 与 Combat / Result 一样给顶栏留 inset。第 1 节已建物体并默认隐藏；本节只铺点和按钮。
- [ ] 顶栏：地图页读 Run（`!IsReady`）。Start 仍不读 `RunConfig`。战斗中生命仍跟局内。`ShowMap` / `EnterCurrentNode` / `PlayCard` / `EndTurn` / `Restart` 已含 `RefreshShell`，不要另写一套。

**阶段门槛：** 开始页点一次到地图，再点进入能开战；打完一场能回到地图。

## 3. 联调与验收（约 1h）

- [ ] Play：对照游戏设计第 4 节。场间无遮罩、**必须回地图**；第二场开局生命 = 第一场结束生命；第三场胜利去 Result；中途死亡不回地图；再来一局后 `遗物：无`。场间若仍 `Begin`，1.0 测试仍会绿，靠这一条 Play 锁。
- [ ] 不要重写 1.0 的 `IsCleared` 用例，也不要补「Victory 未通关自动 `Begin`」的 1.1 复述。
- [ ] grep：`RunController.cs` 无 `Update`；`battleController.Begin` 只在 `EnterCurrentNode`；`ApplyResult` 只在 `ApplyOutcomeIfEnded`；`Core/Battle/`、`BattleInput.cs`、`BattleSetup` 无 `NodeType`；`BattleView` 不调 `battleController.PlayCard` / `EndTurn`；`Begin` / `DiscardSession` 不碰 `_relicDisplayNames`。
- [ ] 全部 EditMode；0.x～1.1 断言未改。

## 4. 文档与交付（约 0.5h）

- [ ] 更新 [`../../../../README.md`](../../../../README.md)：当前阶段改为 1.2 已完成、下一步 1.3；运行方式改为开始页 → 地图 → 三场（场间回地图）。
- [ ] 更新 [`../README.md`](../README.md)：1.2 节标「（已完成）」并补摘要表。
- [ ] 更新 [`../../README.md`](../../README.md)：路线总览 1.2 标已完成；文档地图；第 2 节改为下一个 1.3、下下个按细化规则（1.3 的技术设计进入前再补，或进入 1.3 时写全）。
- [ ] 进入 1.3 前按细化规则核对 [`../1.3/游戏设计.md`](../1.3/游戏设计.md)：目前仅游戏设计。1.2 的「胜利回地图」将在写回与回地图之间插入奖励页；不要代写 1.3 技术设计。
- [ ] 提交。

## 范围控制

- 奖励三选一 / 奖励 RNG → 1.3
- 空商店 / 休息 / 事件页、预声明这些 `NodeType` 值、分支图 → 否（候选池）
- EventBus、拆场景、Addressables、抽 `RunLoop` → 否
- `NodeType` 进 Session / `BattleInput` / `BattleSetup` → 否
- `Update` / 协程里读 `Phase` 或 `ApplyResult` → 否
- 未通关 Victory 仍 `Begin`、`StartRun` 直接开战 → 否
- 场间先 `CreateBattleInput` 再 `ApplyResult` → 否
- 圆点当按钮、地图上印敌人名、公开遭遇表 → 否
- 为 1.3 预留空奖励页 / 空挂钩 → 否

只有阻止本版本验收的问题才进入当日任务。
