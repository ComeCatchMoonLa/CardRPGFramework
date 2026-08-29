# CardRPGFramework

基于 Unity 与 C# 的数据驱动、事件驱动回合制卡牌 RPG 战斗框架 Demo，用于展示战斗模块设计与工程化能力。

## 当前阶段

项目目前处于 Phase 0 技术验证阶段，目标是跑通 Unity、C#、配置读取和最小战斗流程。

- 开发计划：[`Docs/MVP计划与迭代计划.MD`](Docs/MVP计划与迭代计划.MD)
- 当前任务：[`Docs/Phase0-TODO.md`](Docs/Phase0-TODO.md)
- 架构思路：[`Docs/设计思路.MD`](Docs/设计思路.MD)

## 开发工作流

个人开发阶段采用简单的 trunk-based 工作流，只维护 `main` 分支并保持小步提交；暂不创建 `dev` 分支。

## 运行方式

1. 使用 Unity Hub 安装 `ProjectSettings/ProjectVersion.txt` 中指定的 Unity 版本。
2. 在 Unity Hub 中打开本项目根目录。
3. 打开 `Assets/Scenes/TestScene.unity`。
4. 点击 Play 运行。

## 目录说明

- `Assets/Scripts/`：C# 代码，按 Core、Battle、Config、UI 等模块组织。
- `Assets/Configs/`：运行时配置数据。
- `Assets/Scenes/`：Unity 场景。
- `Docs/`：设计文档、开发计划与阶段任务。
- `Packages/`：Unity Package Manager 依赖清单。
- `ProjectSettings/`：Unity 工程设置。
