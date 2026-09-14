# 1.1 TODO：Run 壳与战斗接入

> **预计 2～3 个有效开发日。** 风险：把 `RunState` 传入 Session；在 `Awake` 与 `Begin` 两处都 `new Session`；`Update` 或第二入口再 `ApplyResult`（同一节点两次会打穿 1.0）。

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。

## 0. 开始前检查与复核

- [x] 1.0 已提交推送；对照技术设计第 9 节：`BattleInput` 五字段、`ApplyResult` 必须先发输入。
- [x] `RunConfig` / 单场开战已拍板：Play 只走 `RunConfig`；TestScene 改走 Run，单场删除；`BattleConfig` 不再开战。
- [x] 结束写回时序单通道；View 命令入口收成只调 `RunController`。
- [x] `RunConfig` / `RunController` / `BattleController.Begin` / `DiscardSession` 已写成与 1.0 同级的类型合同。
- [x] 遗物显示名 **[已定]**：`SetRelicDisplayNames`；`StartRun` 在第一次 `Begin` 前调一次；场间不调；`DiscardSession` 不清；`Restart` 空列表清空。
- [x] `EndScreenView` **[已定]** 删除（含 `RefreshEndScreen`、场景遮罩物体）。不用恒 `Hide` 当删除。

**阶段门槛：** 技术设计第 7 节无未拍的 **[待确认]**。**可以进第 1 节。**

## 1. `Begin` 与流程（约 3h）

- [x] `BattleController.Begin(BattleInput)`：用五个字段构造 Session 并 `StartBattle`；`Awake` 不再 `new BattleSession`。`DiscardSession` 丢掉 Session、不清遗物字典。不挂 `RunConfig`，不在 `Begin` 里填遗物字典。
- [x] `SetRelicDisplayNames(IReadOnlyList<RelicData>)`：整表替换；null 抛；空列表清空。**先 Clear 再填**；现网 `Awake` 没有 Clear，不要按它抄。`RelicDisplayName` 一字不改。
- [x] `Begin` **只用 `input.Random`**；删掉 `useFixedSeed` / `seed`。
- [x] `RunConfig`：按技术设计 4.4 的字段、`TryValidate`、`CreateRunState`。生命 / 能量 / 手牌不写 80 / 3 / 5。产品空牌组拒；恰好 3 敌人；遗物不重复；`runSeed == 0` 合法。
- [x] `RunController`：按技术设计 4.5。唯一 SerializeField 持有 `RunConfig`。`StartRun`（校验 → `CreateRunState` → `SetRelicDisplayNames(runConfig.Relics)` → 第一次 `Begin`）/ `PlayCard` / `EndTurn` / `Restart`（`DiscardSession` 后空列表清空字典）。没有 `Update`。`ApplyOutcomeIfEnded` 只从两个命令方法调用；**场间先 `ApplyResult`，未通关再 `Begin(CreateBattleInput())`**，不要倒过来；通关 / 失败 `DiscardSession` 不清字典；场间 `Begin` 不再填字典。
- [x] `BattleView` 只调 `RunController.PlayCard` / `EndTurn`；`StartPageView` 只调 `StartRun`；`ResultPageView` 只调 `Restart`。每次 `Begin` 之后打战斗页 `Refresh`。未就绪文案不要再写「请检查 BattleConfig」。
- [x] Combat / Result **本节就默认隐藏**（不要等第 2 节）；进 Play 不要先刷出旧的战斗未初始化。删除 `EndScreenView.cs`（及 meta）、`BattleView.endScreenView` 与 `RefreshEndScreen`、`TestScene` 结束遮罩物体。场间 Victory 立刻下一场；通关或 `Defeat` 才切 Result。失败只认 `Phase == Defeat`。

**阶段门槛：** 产品侧创建 Session 只剩 `Begin`；产品侧 `ApplyResult` 只在 `ApplyOutcomeIfEnded`、每节点一次。

## 2. 壳 UI（约 2h）

- [x] 顶栏：生命 / 金币 / 牌组张数 / 遗物；战斗中生命跟局内。Start 页（`Run == null`）不读 `RunConfig`：遗物写 `遗物：无`，不要预填 80 / 0 / 10。Result 用 `Run.RelicIds` + `RelicDisplayName`。
- [x] Start / Combat / Result 三页显隐；`TestScene` 改成 Run 壳，不拆场景。
- [x] `RunConfig` 资产接线（10 张 / 1 遗物 / 80 血写在资产上，不写进类型默认值）。`BattleConfig` 不再开战；无引用则删。

**阶段门槛：** 开始页点一次能连打三场到结束页（无奖励、无地图、场间无结束遮罩）。

## 3. 联调与验收（约 1h）

- [ ] Play：连打三场（场间无遮罩；**第二场开局生命 = 第一场结束生命**）、中途死亡、再来一局后 Start 顶栏 `遗物：无`、漏配进不了战斗；对照游戏设计第 4 节。场间顺序写反时 1.0 测试仍会绿，靠这一条 Play 锁。
- [ ] 不要重写 1.0 的 `IsCleared` / 自动下一场复述用例。壳靠 Play。
- [ ] grep：`RunController.cs` 无 `Update`；`BattleView` 不调 `battleController.PlayCard` / `EndTurn`，无 `RefreshEndScreen` / `EndScreenView`；`Begin` / `DiscardSession` 不碰 `_relicDisplayNames`。
- [ ] 全部 EditMode；0.x 与 1.0 断言未改。

## 4. 文档与交付（约 0.5h）

- [ ] 更新根 `README.md`、[`../../README.md`](../../README.md)（开发计划总览）、[`../README.md`](../README.md)（1.1 节标「（已完成）」）。
- [ ] 提交。

## 范围控制

- 地图 / `NodeType` → 1.2
- 奖励三选一 → 1.3
- EventBus、拆场景、Addressables、抽 `RunLoop` → 否
- 保留 `BattleConfig` 开战、单场旁路、快捷键、命令框、插件入口 → 否
- 保留 `EndScreenView`、用恒 `Hide` 当删除 → 否
- `Update` / 协程里读 `Phase` 或 `ApplyResult` → 否
- 为 Start 页预览另开一条读 `RunConfig` 的顶栏路径（含预填 80 / 10）→ 否
- 场间先 `CreateBattleInput` 再 `ApplyResult` → 否

只有阻止本版本验收的问题才进入当日任务。
