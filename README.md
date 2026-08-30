# CardRPGFramework

使用 Unity 与 C# 开发的**简化版《杀戮尖塔》求职 Demo**，重点展示数据驱动、战斗规则与客户端工程化能力，不追求商业级内容量。

## 当前阶段

Phase 1 进行中：可测试的纯 C# Core 基础模型已完成并通过 EditMode 测试。下一步是实现 `BattleSession` 完整战斗闭环。

**已完成**

- `BattlePhase`：战斗阶段枚举（`NotStarted` / `PlayerTurn` / `EnemyTurn` / `Victory` / `Defeat`）。
- `CombatantState`：生命值、格挡、治疗与边界。
- `CardType` / `CardDefinition`：攻击、防御、治疗三种不可变卡牌定义。
- `CardPile`：抽牌堆、手牌、弃牌堆；抽空重洗；两堆皆空时安全停止。
- EditMode 测试覆盖伤害/格挡/治疗边界，以及抽牌、弃牌、重洗与固定种子洗牌。

**尚未完成（本阶段后续）**

- `BattleSession` 完整回合循环与胜负判断。
- ScriptableObject 配置、`BattleController`、最小战斗 UI。
- 场景 Play 模式目前还不能打一场完整战斗。

## 文档

- 开发计划：[`Docs/MVP计划与迭代计划.MD`](Docs/MVP计划与迭代计划.MD)
- 游戏设计：[`Docs/Phase1-游戏设计.md`](Docs/Phase1-游戏设计.md)
- 技术设计：[`Docs/Phase1-技术设计.md`](Docs/Phase1-技术设计.md)
- 当前任务：[`Docs/Phase1-TODO.md`](Docs/Phase1-TODO.md)
- 架构思路：[`Docs/设计思路.MD`](Docs/设计思路.MD)
- 杀戮尖塔机制研究（Phase 2+ 设计输入）：[`Docs/杀戮尖塔机制研究.md`](Docs/杀戮尖塔机制研究.md)
- AI 协作分工：[`Docs/AI协作分工.md`](Docs/AI协作分工.md)

## 开发工作流

个人开发阶段采用简单的 trunk-based 工作流，只维护 `main` 分支并保持小步提交；暂不创建 `dev` 分支。

## 运行方式

1. 使用 Unity Hub 安装 `ProjectSettings/ProjectVersion.txt` 中指定的版本（当前为 **Unity 6000.5.9f1**）。
2. 在 Unity Hub 中打开本项目根目录。
3. 打开 `Assets/Scenes/TestScene.unity`。
4. 点击 Play 运行场景。完整战斗闭环尚未接入，Play 模式目前只能确认工程可打开。

## 运行测试

1. 打开 Unity 菜单 `Window` → `General` → `Test Runner`。
2. 切换到 **EditMode** 页签。
3. 点击 **Run All**。`CombatantStateTests` 与 `CardPileTests` 应全部通过。

## 目录说明

- `Assets/Scripts/Runtime/`：C# 代码，`Runtime.asmdef` 单一程序集，按依赖方向分四层：
  - `Core/`：纯 C# 战斗规则，不引用 `UnityEngine`。
  - `Data/`：ScriptableObject 配置定义（尚未实现）。
  - `Controllers/`：Unity 适配与组装（尚未实现）。
  - `Views/`：纯显示与输入捕获（尚未实现）。
- `Assets/Scripts/Tests/EditMode/`：`Core` 的 EditMode 单元测试（`Tests.asmdef`）。
- `Assets/Data/`：运行时 ScriptableObject 配置资产（配置接入后使用）。
- `Assets/Scenes/`：Unity 场景。
- `Docs/`：设计文档、开发计划与阶段任务。
- `Packages/`：Unity Package Manager 依赖清单。
- `ProjectSettings/`：Unity 工程设置。
