# CardRPGFramework

使用 Unity 与 C# 开发的**简化版《杀戮尖塔》求职 Demo**，重点展示数据驱动、战斗规则与客户端工程化能力，不追求商业级内容量。1.4 之后没有下一个小版本。版本路线见 [`Docs/开发计划/README.md`](Docs/开发计划/README.md)。

**下载。** [Windows 包 `v1.0-demo`](https://github.com/ComeCatchMoonLa/CardRPGFramework/releases/tag/v1.0-demo)（`CardRPGFramework.7z`，Windows 10/11 x64）。解压后运行 `CardRPGFramework.exe`。默认窗口 1600×900，可缩放，Alt+Enter 全屏。演示视频 `demo.mp4` 在同一 Release。本地成片在 `Demo/demo.mp4`，该目录不进 git。发布包 tag 是 `v1.0-demo`。文档版本到 1.4，版本记录在 [`Docs/开发计划/README.md`](Docs/开发计划/README.md) 的路线总览。两套编号不是一回事。

**运行。** 打开 `Assets/Scenes/TestScene.unity` 点 Play。走法见「运行方式」。

**测试。** EditMode **320** 项。怎么跑见「运行测试」。

**边界。** Effect 做事，Rule 改数字。Buff 不是总线。遗物不是 Buff。局外 `RunState` 和局内 `BattleSession` 是两套状态。Core 程序集不引用 `UnityEngine`。

**架构。**

| 层 | 位置 |
| --- | --- |
| Core | `Assets/Scripts/Runtime/Core/`，`Core.asmdef`（`CardRPGFramework.Core`） |
| Data | `Assets/Scripts/Runtime/Data/` |
| Controllers | `Assets/Scripts/Runtime/Controllers/` |
| Views | `Assets/Scripts/Runtime/Views/` |

## 开发工作流

个人开发阶段采用简单的 trunk-based 工作流，只维护 `main` 分支并保持小步提交；暂不创建 `dev` 分支。

## 运行方式

1. 使用 Unity Hub 安装 `ProjectSettings/ProjectVersion.txt` 中指定的版本（当前为 **Unity 6000.5.9f1**）。
2. 在 Unity Hub 中打开本项目根目录。
3. 打开 `Assets/Scenes/TestScene.unity`。
4. 开局配置。点 Play 先到开始页，顶栏是 `生命 --/--`、`金币 --`、`牌组 --`、`遗物：无`。`Assets/Data/Runs/Default.asset` 是这一局的配置：80 血、10 张起始牌（5 打击 + 4 防御 + 1 痛击）、1 件金刚杵、三场 颚虫 / 蓝奴隶贩子 / 颚虫，奖励池 9 张可获得卡（不含打击 / 防御）。在 Relics / Deck / Encounters / Reward Pool 里增删即可换配置，不用改代码。蛇颅骨、纸鹤、剧毒等资产还在，默认短 Run 没配进牌组。
5. 地图和场间。点「开始一局」先到地图：三个点一条线，当前高亮，点「进入」才开战。胜利经奖励页再回地图（已完成变灰、当前前进），不自动下一场。第三场胜利仍先经奖励页再去结束页。中途失败不经奖励页，直接结束页；「再来一局」回到开始页，顶栏重新是 `遗物：无`。奖励页三张互不相同，选一张则顶栏牌组 +1，跳过则张数不变。
6. 战斗页。HUD 是双方生命与格挡、双方 Buff 层数、能量、三个牌堆数量、遗物行、回合数、敌人意图。手牌费用行带类型，攻击伤害是当前力量 / 易伤修正后的数值。点击手牌使用卡牌，点击「结束回合」推进战斗，不要等结束遮罩。
7. 两场敌人。第一场颚虫，状态栏 `颚虫 HP 42/42  格挡 0`。意图按行动表循环：`攻击 11` → `攻击 7 · 防御` → `增益 · 防御` → `攻击 14`（咆哮给了自己 3 层力量）→ …。开局 Buff 行有金刚杵给的 `力量 1`，攻击卡卡面 7。痛击给的 `易伤 2` 下一回合变 `易伤 1`，再下一回合消失。第二场蓝奴隶贩子：`攻击 12` → `攻击 7 · 减益`。耙之后玩家 Buff 行 `虚弱 1`，下一回合攻击卡卡面 5（金刚杵后的 7 × 0.75 向下取整）。

![Phase1局内战斗截图](image/README/Phase1局内战斗截图.png)

## 运行测试

1. 打开 Unity 菜单 `Window` → `General` → `Test Runner`。
2. 切换到 **EditMode** 页签。
3. 点击 **Run All**。应全部通过，共 320 项。

每一版加了哪些断言，见 [`Docs/开发计划/README.md`](Docs/开发计划/README.md) 的路线总览，以及 [`Ver 0.x`](Docs/开发计划/Ver%200.x/README.md) / [`Ver 1.x`](Docs/开发计划/Ver%201.x/README.md) 各版的「测试」行。壳、地图、奖励页和跨场生命靠 Play，见开发计划第 1 节。

## 目录说明

- `Assets/Scripts/Runtime/`：C# 代码。四层见上表。`Core/Core.asmdef`（`CardRPGFramework.Core`，`noEngineReferences: true`）只包住 `Core/`。`Runtime.asmdef` 引用 `Unity.TextMeshPro`、`UnityEngine.UI` 与 `CardRPGFramework.Core`。
  - `Core/Battle/`：`BattleSession` 管回合、出牌、敌人行动和胜负。
  - `Core/Combatants/`：生命、格挡、Buff 容器、遗物列表。
  - `Core/Cards/`：卡牌定义、效果原语、四个牌区。
  - `Core/Actions/`：行动队列，执行时可以继续入队。
  - `Core/Effects/`：只改 `CombatantState`。
  - `Core/Buffs/`：Buff 状态；要生命周期的子类自己实现触发。
  - `Core/Rules/`：伤害公式，固定两段管线。
  - `Core/Relics/`：遗物状态和钩子，与 Buff 分开存放。
  - `Core/Enemies/`：敌人定义和行动表，只描述不执行。
  - `Core/Run/`：`RunState` 输出战斗快照、写回结果、抽奖励。
- `Data/`：ScriptableObject 定义，经 `ToDefinition` / `CreateRunState` 进 Core。必填漏填在启动校验拒绝。`BattleConfig` 已删除。
- `Controllers/`：`RunController` 持有 `RunConfig` 与 `RunState`，是开局、进节点、出牌、结束回合、选奖励和再来一局的入口。`BattleController` 负责 `Begin` / `DiscardSession`，并把局内只读状态和遗物显示名交出去。
- `Views/`：开始、地图、战斗、奖励、结束和顶栏。只显示和接输入，不改战斗数据，不复算伤害。
- `Assets/Scripts/Tests/EditMode/`：测 Core、Data 校验和纯函数文案。
- `Assets/Data/Cards/`、`Assets/Data/Relics/`、`Assets/Data/Enemies/`、`Assets/Data/Runs/`：运行时配置资产。
- `Assets/Scenes/`：`TestScene` 是 Run 壳。
- `Docs/`：见 [`Docs/开发计划/README.md`](Docs/开发计划/README.md)。
- `Packages/`：Unity Package Manager 依赖清单。
- `ProjectSettings/`：Unity 工程设置。

## 已知限制

- **中文字体**：界面用动态字体 Maple Mono NF CN Regular（`Assets/TextMesh Pro/Fonts/MAPLEMONO-NF-CN-REGULAR SDF.asset`，源文件是同目录的 `MAPLEMONO-NF-CN-REGULAR.TTF`）。图集 8192×8192，Multi Atlas 开着，采样点大小 96、Padding 10。`Clear Dynamic Data On Build` 开着，播放器里的字在运行时从这份 TTF 光栅。
- **打出中的牌不在任何牌区**。出现「复制打出」的消费者时，再给牌堆加结算中的位置。见 [0.2 技术设计](Docs/开发计划/Ver%200.x/0.2/技术设计.md) 第 4.4、4.6 节。
- **消耗只在打出时生效**。留在手里的消耗牌，回合结束进弃牌堆。被效果消耗手牌里的一张，以及虚无，都还没有消费者。`CardType` 追加能力牌之后，旧资产里残留的整数 2 会通过校验。见 [0.4 技术设计](Docs/开发计划/Ver%200.x/0.4/技术设计.md) 第 6 节。
- **遗物钩子只有开战入队和改这一次施加**。开战钩子不带对手，施加钩子不能取消施加。没有回合开始 / 结束钩子，`LoseHp` 前没有挂钩。三件标本都在玩家身上。见 [0.5 技术设计](Docs/开发计划/Ver%200.x/0.5/技术设计.md) 第 6 节。
- **卡面和意图显示格挡前的伤害**。见 [0.3 技术设计](Docs/开发计划/Ver%200.x/0.3/技术设计.md) 第 7 节。
- **敌人回合一次跑完，行动表固定循环**。见 [0.6 技术设计](Docs/开发计划/Ver%200.x/0.6/技术设计.md) 第 6 节。
- **回合型 Buff 只有虚弱和易伤，只在轮结束减层**。没有 `OnTurnEnd`、负层、取消施加、叠加刷新保护。见 [0.7 技术设计](Docs/开发计划/Ver%200.x/0.7/技术设计.md) 第 6 节。
- **`ToAction` 留在 `BattleSession`**。它依赖的全是 Session 字段，Session 以外还没有调用方。见 [Phase 2a 技术设计](Docs/开发计划/Ver%200.x/0.1/Phase%202a/技术设计.md) 第 5 节。
