using System;
using System.Collections.Generic;

namespace CardRPGFramework.Core.Enemies
{
    /// <summary>
    /// 不可变的敌人纯 C# 内容数据，与 CardDefinition 同形：由 Data 层的 EnemyData 转换生成，Core 不保留 ScriptableObject 引用。
    /// 没有 EnemyIds / EnemyFactory：Buff 与遗物有工厂是因为它们各自是一个 C# 子类，敌人只是数据，没有任何代码引用某一只具体敌人；
    /// Id 为"配置经稳定 Id 转换为运行时定义"的约定与将来的节点 / 存档引用保留，本版本只在校验里用。
    /// </summary>
    public sealed class EnemyDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int MaxHp { get; }

        /// <summary>至少一条、无 null 元素；构造时拷贝列表，元素引用不变（Session 与测试用引用相等认当前行动）。</summary>
        public IReadOnlyList<EnemyAction> Actions { get; }

        public EnemyDefinition(string id, string displayName, int maxHp, IReadOnlyList<EnemyAction> actions)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Id 不能为空", nameof(id));
            }

            if (string.IsNullOrEmpty(displayName))
            {
                throw new ArgumentException("DisplayName 不能为空", nameof(displayName));
            }

            if (maxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp), "MaxHp 必须大于 0");
            }

            if (actions == null || actions.Count == 0)
            {
                throw new ArgumentException("Actions 至少要有一条", nameof(actions));
            }

            var copy = new EnemyAction[actions.Count];
            for (var i = 0; i < copy.Length; i++)
            {
                copy[i] = actions[i] ?? throw new ArgumentException($"Actions[{i}] 为 null", nameof(actions));
            }

            Id = id;
            DisplayName = displayName;
            MaxHp = maxHp;
            Actions = copy;
        }
    }
}
