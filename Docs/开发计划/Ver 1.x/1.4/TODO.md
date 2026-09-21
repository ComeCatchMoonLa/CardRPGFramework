# 1.4 TODO：Core.asmdef（草稿）

> **草稿。** 1.3 的 TODO 全部勾完并提交推送之前，不要做本节。进入本版前先勾技术设计第 9 节。
>
> **预计 0.5～1 个有效开发日。** 风险：asmdef 放在 `Runtime/` 根（把 UI 卷进零引擎程序集）；Tests 漏引 Core（测试工程找不到 `RunState`）；为通过编译去清 `Library`。

需求以 [`游戏设计.md`](游戏设计.md) 为准，技术边界以 [`技术设计.md`](技术设计.md) 为准。

## 0. 开始前检查与复核

- [ ] 1.3 已提交推送；总设计第 8 节已勾；短 Run Play 路径含奖励页。
- [ ] 对照技术设计第 9 节：Core 全树无 `UnityEngine`；`CreateRewardChoices` 用 `System.Random`；`RewardPageView` 不在 `Core/`。
- [ ] `noEngineReferences: true` 与程序集名 `CardRPGFramework.Core` **[已定]**。不要改成短名 `Core`，也不要用「不写引擎引用」代替该字段。

**阶段门槛：** 第 9 节清单已按 1.3 实现勾完。**可以进第 1 节。**

## 1. 程序集拆分（约 2h）

- [ ] 在 `Assets/Scripts/Runtime/Core/` **内**新建 `Core.asmdef`，JSON 按技术设计 4.1。不要放在 `Runtime/` 根，不要放在 `Core/Run/`。
- [ ] `Runtime.asmdef` 的 `references` 追加 `"CardRPGFramework.Core"`，不删 `Unity.TextMeshPro` / `UnityEngine.UI`。
- [ ] `Tests.asmdef` 的 `references` 追加 `"CardRPGFramework.Core"`，不删 `Runtime`。
- [ ] 等 Unity 自己 recompile。**禁止**删除或清空本仓库 `Library`（含为认新 asmdef 而清缓存）。
- [ ] 不移动任何 `.cs`，不改命名空间。若编译报 Core 用了 `UnityEngine`，只改那一处引用（见技术设计 4.4）。
- [ ] 负例：临时在某个 Core 文件加 `using UnityEngine;`，确认编不过，立刻还原，不要提交。

**阶段门槛：** Editor 无编译错误；Core 程序集 Inspector 上 No Engine References 已勾。

## 2. 回归（约 1h）

- [ ] 全部 EditMode；0.x～1.3 断言未改。若缺类型，先补 Tests 对 Core 的引用，不要把测试文件挪出 EditMode。
- [ ] Play：1.3 短 Run 走一遍（奖励页仍在，流程不变）。
- [ ] grep：`Assets/Scripts/Runtime/Core/` 无 `using UnityEngine`；`Core.asmdef` 含 `"noEngineReferences": true`。

## 3. 文档与交付（约 0.5h）

- [ ] 更新 [`../../../../README.md`](../../../../README.md)：当前阶段改为 1.4 已完成（可选）；目录说明加上 `Core.asmdef`。
- [ ] 更新 [`../README.md`](../README.md)：1.4 节标「（已完成）」并补摘要。
- [ ] 更新 [`../../README.md`](../../README.md)：路线总览 1.4 标已完成；文档地图；第 2 节按细化规则——1.4 之后无下一个小版本三件套，只剩候选池与演示前清单。
- [ ] 本文与游戏设计 / 技术设计文首去掉「草稿」，改标已转正。
- [ ] 提交。

## 范围控制

- 改奖励 / 地图 / 战斗结算 → 否（1.3 已收口；有缺陷单开，不绑在拆程序集上）
- 按命名空间再拆 Core、搬 `Views` / `Data` → 否
- 清 `Library` / `Temp` 当修复手段 → 否
- HybridCLR、Addressables、给 Tests 开 `noEngineReferences` → 否
- 为「看起来更干净」改 namespace 或文件夹 → 否

只有阻止本版本验收的问题才进入当日任务。
