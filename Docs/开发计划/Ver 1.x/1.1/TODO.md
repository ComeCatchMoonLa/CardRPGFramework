# 1.1 TODO：Run 壳与战斗接入（草稿）

> **草稿。** 第 0 节（对照 1.0 实际代码复核转正）在 1.0 完成后再做。条目基于草稿 [`技术设计.md`](技术设计.md)，转正时按复核结果改。
>
> **预计 2～3 个有效开发日。** 风险：把 `RunState` 传入 Session；或在 `Awake` 与 `Begin` 两处都 `new Session`。`RunConfig` 与 `BattleConfig` 未拍板前不要先做两套 SO。

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。

## 0. 开始前检查与复核（约 1h，1.0 完成后）

- [ ] 1.0 已提交推送，工作区干净，EditMode 全绿。
- [ ] 走完技术设计第 9 节复核清单：`BattleInput` 字段、`ApplyResult` 是否要求先发输入、`RunConfig` 待确认项拍板。
- [ ] 去掉三份文档的草稿头（拍板写入技术设计后再去）。

**阶段门槛：** 技术设计与 1.0 实际代码一致，待确认项已定。

## 1. `Begin` 与流程（约 3h）

- [ ] `BattleController.Begin(BattleInput)`：用五个字段构造 Session 并 `StartBattle`；`Awake` 不再 `new BattleSession`。
- [ ] `RunController`：持有 `RunState` 与当前页；开始一局 / 胜利自动下一场 / 失败或通关去结束页。不把 `RunState` 传入 Session。
- [ ] `EndScreenView` 通知流程，不再自己拦住整局。

**阶段门槛：** 产品侧创建 Session 只剩 `Begin` 一处。

## 2. 壳 UI（约 2h）

- [ ] 顶栏：生命 / 金币 / 牌组张数 / 遗物；战斗中生命跟局内。
- [ ] Start / Combat / Result 三页显隐；再来一局回 Start。
- [ ] 内容配置按已拍板的 `RunConfig` / `BattleConfig` 方案接线，不留两套真相。

**阶段门槛：** 开始页点一次能连打三场到结束页（无奖励、无地图）。

## 3. 联调与验收（约 1h）

- [ ] Play：连打三场、中途死亡、再来一局；对照游戏设计第 4 节。
- [ ] 全部 EditMode；0.x 与 1.0 断言未改。

## 4. 文档与交付（约 0.5h）

- [ ] 1.x README 的 1.1 节标「（已完成）」。
- [ ] 提交。

## 范围控制

- 地图 / `NodeType` → 1.2
- 奖励三选一 → 1.3
- EventBus、拆场景、Addressables → 候选池或不做
- 未拍板就同时做 `RunConfig` 与保留完整 `BattleConfig` 开战路径 → 否

只有阻止本版本验收的问题才进入当日任务。
