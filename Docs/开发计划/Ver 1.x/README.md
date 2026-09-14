# Ver 1.x：第一次短 Run，然后交付

> **大版本目标**：1.0～1.3 把 0.x 的局内战斗接进一个能从开局打到结束的短 Run——三场战斗（至少两种敌人）、战后选卡、血量 / 牌组 / 遗物跨场保留、结束页。这是项目第一次成为"一个游戏"而不是"一场战斗"，也是 `RunState` 与 `BattleSession` 两套状态第一次同时存在。出包、字体、演示不是版本，见文末演示前清单。再往后是候选池，有消费者才排。
>
> **进入 1.0 的门槛**：[`../Ver 0.x/README.md`](../Ver%200.x/README.md) 末尾的 0.x 完成定义全部满足。局内没有复合卡、消耗、遗物、多种敌人之前，战后三选一只是"再来一张打击"，证明不了局内外联调。
>
> 短 Run 总设计见 [`游戏设计.md`](游戏设计.md)。**第 8 节完成定义到 1.3 完成后才勾。**

版本号约定与细化深度规则见 [`../README.md`](../README.md)。

## 每一版只证明一条边界

| 版本 | 模块 | 证明什么 | 本版没有 |
| --- | --- | --- | --- |
| **1.0** | `Core.Run` | 局外是状态，不是规则系统。进战斗是快照，出战斗只写回约定结果 | 任何 Run UI；`Core.Run` 与本刀新文件不 `new BattleSession` |
| **1.1** | 壳 + 流程 | 战斗页只管局内；Session 生命周期在流程层 | 地图、奖励 |
| **1.2** | 地图 | 新节点类型 = 新页 + 流程一个分支；不改 `BattleSession` | 空商店页；分支图 |
| **1.3** | 奖励 | 奖励抽卡发生在战斗页之外；奖励 RNG 独立于战斗洗牌 | 遗物奖、升级、移除 |
| **1.4** | 程序集 | Core 零 `UnityEngine` 是编译约束 | 不为它倒逼 1.0～1.3 |

不拆 Start / 顶栏 / Result 成单独版本：没有其它页也不成壳。不把 1.0～1.3 合成一刀。不把 1.2 与 1.3 合并。

## 1.0 `RunState`（已完成）

文档：[`1.0/游戏设计.md`](1.0/游戏设计.md) / [`1.0/技术设计.md`](1.0/技术设计.md) / [`1.0/TODO.md`](1.0/TODO.md)。

- `RunState`：当前生命、最大生命、牌组、遗物 Id、金币（恒 0）、当前节点（`int`）、主种子、开局传入的三场敌人。
- 输出 `BattleInput`（含当前生命的 `BattleSetup` + 牌组拷贝 + 遗物新实例 + 当前节点敌人 + 一次性 `Random`）；接收 `won` + `remainingHp`。`AddCard` 本版就有，1.3 才抽三张。
- 无 `RunConfig` SO、无任何 Run UI。Play 仍是 `TestScene` + `BattleConfig`。
- `BattleController` 里 0.x 那一处 `new BattleSession` 原样保留，1.1 才改成 `Begin`。

| 已有 | 说明 |
| --- | --- |
| 残血开战 | `BattleSetup` 三参仍满血，四参当前生命 `> 0` 且 `≤ max`；`CombatantState(max, current)` 同一条边界；Session 用快照当前生命开战，敌人仍满血，构造签名不变 |
| `Core.Run` | `RunState` / `BattleInput`；`CreateBattleInput` 浅拷贝牌组、每场 `RelicFactory.Create`、种子 `unchecked(runSeed * 397 ^ nodeIndex)`，不洗牌、不预支 `Random.Next` |
| 写回 | `ApplyResult` 必须先发输入、每节点一次；胜利写血并推进，失败不写不推进；`AddCard` 只拒 null / 失败，通关后仍允许 |
| 测试 | 302 项 EditMode（0.x 的 263 项不改断言；残血构造 11 项；`Tests/EditMode/Run/RunStateTests.cs` 28 项）。`Core/Run/` 无 `new BattleSession` |

**证明的边界**：局外是状态；进战斗是快照，出战斗只写回约定结果。

## 1.1 Run 壳与战斗接入（已完成）

文档：[`1.1/游戏设计.md`](1.1/游戏设计.md) / [`1.1/技术设计.md`](1.1/技术设计.md) / [`1.1/TODO.md`](1.1/TODO.md)。

- `RunController` + 顶栏 + Start / Combat / Result。`TestScene` 改成 Run 壳。
- 流程层创建 / 销毁 Session：`BattleController.Begin`；不把 `RunState` 传入 Session。Play 只走 `RunConfig`；`BattleConfig` 已删。局内命令只打 `RunController`。
- 胜利自动下一场（场间无结束遮罩）；通关或失败去结束页。结束写回：`PlayCard` / `EndTurn` 内读一次 `Phase`，禁止 `Update` 轮询。删除 `EndScreenView`。
- 遗物显示名：`SetRelicDisplayNames`；`StartRun` 在第一次 `Begin` 前调一次；场间不调；`Restart` 清空。
- 无地图、无奖励。

| 已有 | 说明 |
| --- | --- |
| `Begin` / `DiscardSession` | `Awake` 不再 `new BattleSession`；只用 `input.Random`；`Begin` 不填遗物字典 |
| `RunConfig` | Play 唯一开战真相；空牌组拒；恰好 3 敌人；生命 / 能量 / 手牌不写类型默认值 |
| `RunController` | 无 `Update`；场间先 `ApplyResult` 再 `Begin`；通关 / 失败才 `DiscardSession` |
| 壳 UI | `RunShellView` 常驻；Start 不读 `RunConfig`；Combat / Result 默认隐藏；删 `EndScreenView` |
| 测试 | 302 项 EditMode，0.x 与 1.0 断言未改。壳与「第二场开局生命」靠 Play |

**证明的边界**：战斗页只管局内；Session 生命周期在流程层。

## 1.2 地图与节点分发

文档：[`1.2/游戏设计.md`](1.2/游戏设计.md)（进入前补技术设计与 TODO）。

- 三个战斗节点一条线，只能进当前节点；胜利回地图，最后一场去结束页。
- 节点带 `NodeType`，流程按类型分发；`BattleSession` 里没有 `NodeType`。
- 不加空商店、不做分支图。

**证明的边界**：新节点类型 = 新页 + 流程一个分支，不改 `BattleSession`。

## 1.3 战后三选一

文档：[`1.3/游戏设计.md`](1.3/游戏设计.md)（进入前补技术设计与 TODO）。

- 胜利 → `ApplyResult` 写回 → 奖励页 → `AddCard` / 跳过 → 地图或结束。`g(runSeed, rewardIndex)` 与战斗 `Random` 无关。第三场通关后仍走奖励页。
- 不做遗物奖 / 升级 / 移除。

**证明的边界**：奖励抽卡发生在战斗页之外；奖励 RNG 独立于战斗洗牌。

短 Run 完成定义见总设计 [第 8 节](游戏设计.md)。勾完 1.3 才勾那一节。

## 1.4 Core.asmdef（可选）

把 `Core` 拆成独立程序集（`Core.asmdef`，零 `UnityEngine` 引用），让"纯 C# 战斗规则"从约定变成编译约束；`Data` / `Controllers` / `Views` 留在 Unity 侧。不为它倒逼 1.0～1.3 改引用。不建本版空文件夹，排进来再写三件套。

## 演示前清单（不是版本）

面试演示前再做，不加玩法、不占版本号：

- Windows 可运行包（一个 zip，双击开局）。
- 中文 TMP 字体改为**静态图集**并入库，结束 Dynamic 图集被 Play 写脏、出包方块字的问题。
- README 收口：架构图、边界说明、测试数、运行方式。
- 2～3 分钟演示视频。
- 面试讲解稿（Action / Effect 为什么拆、Buff 为什么不是总线、Rule 为什么不注册、遗物为什么不是 Buff、局内外为什么两套状态、为什么暂不上 Luban）。

不做：HybridCLR / 热更（没有线上包与配表更新流程，只剩关键词）。

## 候选池（不排序）

每一项都只在出现对应消费者、或某类岗位明确要问时才排进版本；排进来之前不写文档。

| 候选 | 触发条件 | 备注 |
| --- | --- | --- |
| Luban 导表（卡牌 / 敌人） | 效果列表与敌人行动表字段已冻住、连续两个版本没改形状 | 只导内容，不导 Rule；SO 降为编辑器预览或删除，避免两套真相 |
| 多敌人 + 目标选择 | 想做全体 / 随机目标的卡 | `EffectTarget` 扩值、`ToAction` 多一个目标参数、战斗页敌人槽与点选 |
| 药水槽 | 想证明"不进牌堆、不占手牌"的道具 | 0～2 瓶标本，独立槽位，不计"打出一张牌" |
| 金属化 / `OnTurnEnd` | 想要第二个触发时机的 Buff 标本 | 与 `IBuffTrigger.OnTurnStart` 同形 |
| 钨条 / 鸟居 / 无实体 | 想证明伤害管线的三个后段挂钩点 | 每种一件，位置见扩展边界批注第 2 节 |
| 商店 / 休息 / 事件节点 | 短 Run 想要选择感 | 新 `NodeType` + 新页；地图从线改成图 |
| 遗物奖励、卡牌移除、升级 | 局外成长维度 | 依赖节点类型扩展 |
| 存档 | 想关掉再开继续 | `RunState` 序列化即可，UI 不变 |
| Addressables | 有第二个场景 / 资源体积成为问题 | 只做配置加载一条路径 |
| 敌人带约束的随机行动 | 想更像原版 | 用战斗种子，保证可复现 |

## 默认不做

EventBus、ECS、Attribute 面板、Rule 注册中心、通用 BuffModifier、大型 UI 框架（MVP / MVVM）、HybridCLR、战斗回放、联网、四角色 / 满卡池 / 满遗物 / 满药水。理由见 [`../../Phase 2 扩展边界批注.md`](../../Phase%202%20扩展边界批注.md) 第 5 节与 [`../Ver 0.x/0.1/MVP计划与迭代计划.MD`](../Ver%200.x/0.1/MVP计划与迭代计划.MD) 的"风险 1：架构过度"。
