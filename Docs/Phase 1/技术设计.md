# Phase 1 技术设计：核心战斗闭环

> **技术目标：让战斗规则成为可单元测试的纯 C# 代码，Unity 只负责配置、生命周期和 UI。**
>
> 优先打好“核心逻辑不依赖场景”的地基，但不建设 Phase 2 才需要的通用框架。

最终产品是简化版《杀戮尖塔》求职 Demo。Phase 1 的地基需要长期承载牌堆、手牌、能量、格挡、卡牌效果、Buff、规则修改和回放，但只实现当前闭环已经使用的部分。

## 1. 技术原则

优先级为：**正确性 > 可测试性 > 可读性 > 当前已知需求的扩展性**。

- 战斗规则不继承 `MonoBehaviour`，不直接访问 UI、资源或场景。
- Unity 组件只负责组装配置、接收输入和更新显示。
- 只抽象已经出现至少两次的变化点。
- 每完成一个可运行切片就测试和提交，不积累大批未验证代码。

## 2. 分层与依赖

```text
Views（BattleView/CardButtonView：只管显示与输入捕获，不持有战斗状态）
  ↓ 转发输入 / 接收刷新指令
Controllers（BattleController：Unity 适配与组装，持有 BattleSession）
  ├── Data（ScriptableObject）
  └── Core（纯 C# 战斗规则）
```

依赖约束：

- `Core` 不引用 `UnityEngine`、`MonoBehaviour`、ScriptableObject、`Data`、`Controllers` 或 `Views`。只用 `System.*`；`UnityEngine.Random` 改用注入的 `System.Random`，`Debug.Log` 改为返回 `bool`/结果对象。
- `Data` 负责将 Inspector 数据转换为 Core 可用的卡牌定义和战斗参数。
- `BattleController`（独立的 `Controllers` 层，因持有 `MonoBehaviour` 不能放进 `Core`，也不属于纯显示的 `Views`）创建并持有一场 `BattleSession`，不实现具体战斗规则。
- `Views` 不直接调用 `Core`/`Data`，只把玩家输入转发给 `BattleController`；命令调用返回后，`Views` 在同一调用栈里主动读取 `BattleController` 暴露的只读状态并刷新，不引入事件/观察者机制通知刷新。
- 不建立全局 Singleton、IOC 容器、消息总线或 Service Locator。

### 面向最终目标的地基边界

以下约束应长期保持：

- `BattleSession` 是战斗状态的唯一写入入口，UI、动画和配置不能绕过它直接改生命、格挡或牌堆。
- 卡牌配置使用稳定 ID，运行时逻辑不依赖资源路径或显示名称。
- 随机过程由战斗会话持有，后续才能支持固定种子测试和战斗回放。
- Unity 表现层只消费战斗状态；替换 `Views` 不应改动战斗规则。

以下实现允许后续替换，不为它们提前设计框架：

- Phase 1 的 `CardType` 分支后续可被 Effect System 替换。
- 固定敌人攻击后续可扩展为敌人意图与 AI。
- `Views` 主动刷新后续可改为事件驱动表现。
- ScriptableObject 后续可接入 Luban，但 Core 模型保持不变。

## 3. 目录与文件

```text
Assets/
├── Scripts/
│   ├── Runtime/（Runtime asmdef 所在的组装层文件夹，不计入命名空间）
│   │   ├── Core/
│   │   │   ├── Battle/
│   │   │   │   ├── BattlePhase.cs
│   │   │   │   └── BattleSession.cs
│   │   │   ├── Combatants/
│   │   │   │   └── CombatantState.cs
│   │   │   └── Cards/
│   │   │       ├── CardType.cs
│   │   │       ├── CardDefinition.cs
│   │   │       └── CardPile.cs
│   │   ├── Data/
│   │   │   ├── CardData.cs
│   │   │   └── BattleConfig.cs
│   │   ├── Controllers/
│   │   │   └── BattleController.cs
│   │   └── Views/
│   │       ├── BattleView.cs
│   │       └── CardButtonView.cs
│   └── Tests/
│       └── EditMode/（Tests asmdef 所在文件夹）
└── Data/
    ├── Cards/
    └── Battles/
```

`Core/Actions`、`Core/Events`、`Core/Powers` 等文件夹对应 Phase 2 才实现的 ActionSystem/EventSystem/BuffSystem，Phase 1 阶段保持空文件夹即可，不提前放代码。

预期命名空间（`Assets/Scripts/Runtime` 视为组织层，不计入命名空间，与原先跳过 `Scripts` 的处理方式一致）：

- `CardRPGFramework.Core.Battle`
- `CardRPGFramework.Core.Combatants`
- `CardRPGFramework.Core.Cards`
- `CardRPGFramework.Data`
- `CardRPGFramework.Controllers`
- `CardRPGFramework.Views`
- `CardRPGFramework.Tests`

文件按实现进度创建，不一次性建立所有空文件。

## 4. 核心模型

### BattlePhase

纯数据枚举，没有任何方法或行为，只是一个可以存进字段的值。取值和转换规则见第 6 节；判断"当前阶段允许什么操作""何时切到下一阶段"的逻辑全部写在 `BattleSession` 里，不写在 `BattlePhase` 自己身上——给枚举加方法（如 `CanTransitionTo`）等于搭建状态模式框架，属于第 6 节明确排除的过度设计。

### BattleSession

一场战斗的唯一规则入口，负责：

- 当前阶段、回合数和玩家能量。
- 玩家与敌人的运行时状态。
- 抽牌堆、手牌、弃牌堆。
- 开始战斗、使用卡牌、结束玩家回合。
- 敌人固定攻击和胜负判断。

公开命令保持最少：

```csharp
void StartBattle();
bool TryPlayCard(int handIndex);
bool TryEndPlayerTurn();
```

无效操作返回 `false`，正常的玩家误操作不抛异常。

### CombatantState

保存 `MaxHp`、`CurrentHp` 和 `Block`，集中处理：

- 受伤时先扣格挡再扣生命。
- 治疗不超过最大生命值。
- 生命值不低于 0。
- 清除格挡。

### CardDefinition

不可变的纯 C# 数据：

```text
Id
DisplayName
Type
Cost
Value
```

Phase 1 使用 `CardType` 分支处理攻击、防御、治疗。现在不建立 `ICardEffect` 或 Effect 类层级；等 Phase 2 出现组合效果后再提炼 Effect System。

### CardPile

只处理牌的容器规则：

- 初始化并洗牌。
- 抽牌。
- 弃牌。
- 抽牌堆为空时重洗弃牌堆。
- 抽牌堆与弃牌堆同时为空时安全停止抽牌，不抛异常。

使用构造函数传入的 `System.Random`，测试可使用固定种子。

## 5. 配置模型

### CardData

ScriptableObject 字段：

| 字段 | 类型 | 约束 |
| --- | --- | --- |
| `id` | `string` | 非空；不同卡牌定义之间唯一 |
| `displayName` | `string` | 非空 |
| `type` | `CardType` | Attack / Defend / Heal |
| `cost` | `int` | 大于等于 0 |
| `value` | `int` | 大于等于 0 |

`CardData` 提供转换方法生成 `CardDefinition`。Core 不保留 ScriptableObject 引用。

### BattleConfig

ScriptableObject 字段：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `playerMaxHp` | `int` | 玩家最大生命值 |
| `enemyMaxHp` | `int` | 敌人最大生命值 |
| `enemyDamage` | `int` | 敌人固定伤害 |
| `energyPerTurn` | `int` | 每回合能量 |
| `handSize` | `int` | 每回合目标手牌数 |
| `deck` | `List<CardData>` | 初始牌组，可重复引用 |

配置引用方案：

- 场景中的 `BattleController` 显式引用 `BattleConfig`。
- 配置资产放在 `Assets/Data/`，不依赖 `Resources` 路径字符串。

## 6. 战斗阶段

```text
NotStarted
PlayerTurn
EnemyTurn
Victory
Defeat
```

阶段只表达当前允许的操作，不搭建通用状态机框架。`BattleSession` 内部用明确的方法切换阶段。

## 7. 结算顺序

### 使用卡牌

```text
检查 PlayerTurn、手牌索引和能量
  ↓
扣除能量
  ↓
按 CardType 同步结算
  ↓
卡牌从手牌进入弃牌堆
  ↓
检查敌人是否死亡
```

### 结束回合

```text
弃掉剩余手牌
  ↓
进入 EnemyTurn
  ↓
敌人攻击玩家
  ↓
检查玩家是否死亡
  ↓
未死亡则开始下一玩家回合
```

Phase 1 不建立独立 Action Queue。当前效果全部同步且没有表现等待，队列只会增加调试成本；出现多段效果、触发器或动画等待后再引入。

> `BattleSession` 内结算发生在卡牌移入弃牌堆之前。Phase 1 的三种效果都不修改牌堆/手牌，所以顺序当前没有影响；以后出现"打出时触发抽牌"等会改变手牌的效果时，需要重新核对 `handIndex` 在结算时刻的语义是否仍然有效。

## 8. UI 方案

- 使用 Unity uGUI 的 Canvas、Button 和文本组件。
- `BattleView` 负责整体状态显示和结束回合按钮。
- `CardButtonView` 负责显示一张手牌并把索引回传给 `BattleView`。
- `BattleView` 显示敌人下一步的固定攻击伤害，为后续敌人意图系统保留用户体验入口，但当前不抽象意图模型。
- 初期可固定准备 5 个卡牌按钮并复用，避免现在引入动态列表和对象池。
- UI 在命令执行后主动刷新，不建立全局事件系统。
- 不保留 Phase 0 的 IMGUI 和运行时自动创建入口。

## 9. 测试策略

先创建一个 Runtime asmdef 和一个 EditMode Tests asmdef，测试纯 C# Core，不测试 Unity UI。

最低测试集合：

- 伤害优先扣除格挡，生命值边界正确。
- 治疗不超过最大生命值。
- 使用卡牌正确扣除能量并移动到弃牌堆。
- 能量不足或阶段错误时拒绝使用卡牌。
- 结束回合后敌人攻击并进入下一玩家回合。
- 抽牌堆耗尽时正确重洗弃牌堆。
- 抽牌堆与弃牌堆同时为空时抽牌安全停止。
- 敌人或玩家生命归零后进入正确结束阶段。

## 10. 错误处理

- 配置缺失、牌组为空、不同卡牌资产使用重复 ID 等开发期错误：启动时验证，记录明确错误并停止战斗初始化。牌组重复引用同一卡牌资产是合法的。
- 能量不足、索引无效、战斗已结束等运行时正常拒绝：返回 `false`，不刷异常日志。
- 不使用异常控制正常战斗流程。

## 11. 关键技术决策

| 决策 | Phase 1 选择 | 原因 |
| --- | --- | --- |
| 配置 | ScriptableObject | Inspector 直接编辑，当前成本最低 |
| 核心逻辑 | 纯 C# 对象 | 可测试、与 Unity 生命周期解耦 |
| 卡牌效果 | `CardType` 分支 | 只有三种效果，暂不需要多态框架 |
| 结算方式 | 同步调用 | 没有动画等待或触发链 |
| 随机数 | 注入 `System.Random` | 测试可复现，无需额外接口 |
| UI | uGUI + 固定按钮槽 | 足够展示，接线和调试成本低 |
| 模块通信 | 直接调用 | 当前规模不需要 EventSystem |
| UI 架构模式 | 不套用命名模式，只按已锁定约束反推依赖方向 | 结果最接近 MVP 的 Passive View 变体（`Views` 不知道 `Core`/`Data`，`Controllers` 主导刷新），但不引入 `IView` 接口等 MVP 标准设施；MVVM 因需要数据绑定/可观察属性，与“不建立全局事件系统”直接冲突，排除；经典 MVC 因允许 View 直接读 Model，与“`Views` 不直接调用 `Core`/`Data`”冲突，排除 |

## 12. 完成定义

- 游戏设计中的完整战斗规则全部可运行。
- Core 测试通过，Unity Console 无错误。
- Core 不引用 Unity API。
- 修改 ScriptableObject 数值即可改变牌组与战斗参数。
- 项目中不存在 Phase 0 临时 IMGUI、自动启动逻辑和 Resources 配置。
- 没有实现 Phase 2 范围。

玩法规则见 [`Phase1-游戏设计.md`](Phase1-游戏设计.md)，执行顺序见 [`Phase1-TODO.md`](Phase1-TODO.md)。
