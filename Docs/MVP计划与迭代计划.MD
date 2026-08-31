# MVP 计划与迭代计划

如果目标是**求职 Unity 客户端岗位**，这个项目不要按“做一个完整游戏”的思路管理，而应该按“做一个可展示的技术作品”管理。

核心目标：

> 在有限时间内证明：我能进入商业手游项目，负责战斗模块开发。

所以 MVP 要控制范围。

---

# 项目定位

**Unity 数据驱动卡牌 RPG 战斗框架 Demo**

目标：

* 可运行
* 有完整战斗流程
* 有扩展性
* 能展示架构设计

不是目标：

* 做完整养成游戏
* 做精美美术
* 做几十小时内容

---

# MVP（4～6周）

## Phase 0：技术验证（2～3天）

目标：

确认技术路线。

完成：

* Unity工程建立
* C#代码结构
* Git仓库
* 基础场景
* 简单UI

验证：

```text
Unity
+
C#
+
配置读取
+
战斗流程
```

风险：

如果一开始就设计十几个系统，很容易架构过度。

---

## Phase 1：核心战斗闭环（1～2周）

这是 MVP 核心。

实现：

### 战斗流程

```text
BattleStart

↓

PlayerTurn

↓

PlayCard

↓

ResolveEffect

↓

EnemyTurn

↓

BattleEnd
```

包含：

* 回合系统
* 卡牌使用
* 伤害计算
* 胜负判断

> 修订：行动队列移至 Phase 2 与 Effect System 同期引入。Phase 1 全部效果同步结算，队列没有真实客户；取舍理由见 `Docs/Phase1-技术设计.md` 第 7 节。

技术体现：

★★★★★

关键词：

* 状态机
* 战斗流程管理

---

### Card System

实现：

基础卡牌：

* 攻击
* 防御
* 治疗

> 修订：Buff 卡移至 Phase 2，避免临时 Buff 规则与 Buff System 重复实现；见 `Docs/Phase1-游戏设计.md` 第 5 节。

结构：

```text
CardData

↓

CardRuntime

↓

Effect
```

不要：

一个卡一个类。

---

## Phase 2：框架化（约 2 周，拆成 2a/2b/2c 三个独立可验收阶段）

这是拉开和普通 Demo 差距的阶段。原计划把 ActionSystem/EffectSystem/BuffSystem/RuleSystem 四个系统放在同一段文档里一起做，但这个体量和 Phase 1（只做一个 `BattleSession`）明显不在一个粒度上，所以拆成三个独立阶段，每阶段一套游戏设计/技术设计/TODO 文档，结构和验收粒度对齐 Phase 1：

- **Phase 2a：ActionSystem + EffectSystem** —— [`Docs/Phase 2a/游戏设计.md`](Phase%202a/游戏设计.md) / [`技术设计.md`](Phase%202a/技术设计.md) / [`TODO.md`](Phase%202a/TODO.md)。把出牌结算从"直接改状态"重构成"生成 Action 入队、Action 执行时调用 Effect"，玩家可见行为不变，是后两阶段的地基。杀戮尖塔原版同构：`AbstractGameAction` + `GameActionManager`，见 `Docs/杀戮尖塔机制研究.md`。
- **Phase 2b：BuffSystem** —— [`Docs/Phase 2b/游戏设计.md`](Phase%202b/游戏设计.md) / [`技术设计.md`](Phase%202b/技术设计.md) / [`TODO.md`](Phase%202b/TODO.md)。新增力量（纯数据）、中毒（数据+触发器）两个 Buff，验证"Buff 是数据容器，部分 Buff 附带触发行为"的边界。
- **Phase 2c：RuleSystem（你的核心优势）** —— [`Docs/Phase 2c/游戏设计.md`](Phase%202c/游戏设计.md) / [`技术设计.md`](Phase%202c/技术设计.md) / [`TODO.md`](Phase%202c/TODO.md)。新增虚弱、易伤两个 Buff 并接入 3 条具体伤害规则（力量加伤害、虚弱减伤害、易伤增伤害），证明数值计算和状态执行分离。这个方向迁移自 Roblox 项目的 RuleSystem 设计，技术体现 ★★★★★，是简历上的差异化卖点，不因为拆分而弱化。

三个子阶段都不做通用组合能力（不建 `CompositeAction`/`ConditionalAction` 等 Action 变体，不建规则注册引擎），只做当前已确定要证明的最小内容，具体排除项见各自技术设计文档的"排除项"小节。

---

## Phase 3：数据驱动（1周）

接入：

Luban 或类似配置方案。

配置：

```text
Card.xlsx

Skill.xlsx

Buff.xlsx

Rule.xlsx
```

实现：

Excel

↓

生成配置

↓

运行时读取

---

展示：

新增卡牌：

只改配置，不改代码。

这个面试非常好讲。

---

# MVP完成标准

达到：

✅ 战斗可玩
✅ 10张卡牌
✅ 5种效果
✅ 5种Buff
✅ 规则系统
✅ 配置驱动
✅ 简单AI

即可。

不要继续扩内容。

---

# 后续迭代计划

## V1.1 工程化增强（2周）

优先级最高：

### Replay系统

利用事件系统：

记录：

```text
TurnEvent
ActionEvent
DamageEvent
```

实现：

* 战斗回放
* 战斗日志

价值：

★★★★★

---

### 战斗表现层

增加：

* 动画
* 特效
* 伤害数字
* 卡牌动画

注意：

最后做。

程序价值低于系统。

---

## V1.2 商业化能力（2～4周）

### 热更新

了解：

* HybridCLR

体现：

国内手游开发经验。

---

### 资源管理

学习：

* Addressables

实现：

* 卡牌资源加载
* 配置资源管理

---

### UI框架

例如：

MVP/MVVM：

用于：

```text
BattleView

CardView

CharacterPanel
```

不要一开始做。

---

# 时间管理

建议：

## 每周目标

不要按功能驱动：

错误：

> 今天写卡牌，明天写Buff

容易无限扩展。

应该：

按展示目标：

---

第1周：

> 能打一场完整战斗

---

第2周：

> 卡牌和效果可扩展

---

第3周：

> 装备/规则系统体现架构

---

第4周：

> 配置驱动

---

第5～6周：

> 打磨、文档、视频、简历

---

# 风险控制

## 风险1：架构过度 ⭐⭐⭐⭐⭐

最大风险。

你有经验，反而容易设计太复杂。

例如：

一开始：

* ECS
* IOC
* MVVM
* 消息总线
* 多线程

全部加。

结果：

一个月还没有战斗。

解决：

原则：

> 先跑通，再抽象。

---

## 风险2：美术资源拖累 ⭐⭐⭐⭐

不要做：

* 角色动画
* 大地图
* 剧情

使用：

* 简单UI
* 免费资源
* 方块/图标

面试看代码。

---

## 风险3：Unity细节消耗时间 ⭐⭐⭐

例如：

* UI布局
* Animator
* Shader

控制：

“能展示即可”。

---

## 风险4：系统边界反复修改 ⭐⭐⭐⭐

建议：

第一版：

```text
BattleCore
Card
Effect
Buff
Rule
```

稳定后再拆。

---

# 最终优先级

如果时间只有1个月：

必须：

1. 战斗流程
2. Card System
3. Effect System
4. Rule System
5. Buff System
6. 配置驱动

第二梯队：

7. Replay
8. AI
9. Addressables
10. 热更新

不要碰：

* ECS
* 大型UI框架
* 联机
* 大地图

---

以你的背景，我觉得合理目标是：

**6周做出 MVP → 开始投 Unity 客户端 → 面试过程中继续迭代。**

不要等“完美项目”完成再投。你已经有游戏开发经历，Demo 的作用是证明“Unity迁移完成”，不是证明你能独立做一款商业游戏。
