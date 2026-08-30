using System;

namespace CardRPGFramework.Core.Cards
{
    /// <summary>
    /// 不可变的卡牌纯 C# 数据，由 Data 层的 CardData 转换生成，Core 不保留 ScriptableObject 引用。
    /// Phase 1 只有单一效果卡牌，用 Type 分支处理；出现复合效果卡牌时，
    /// Type/Value 会被 EffectSystem 的效果列表替换（见 Phase1-技术设计.md 第 4 节）。
    /// </summary>
    public sealed class CardDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public CardType Type { get; }
        public int Cost { get; }

        /// <summary>
        /// 含义取决于 Type：Attack 是伤害值，Defend 是格挡值，Heal 是治疗量。
        /// 三种含义共用一个字段是 Phase 1 的临时形状，见上方类注释。
        /// </summary>
        public int Value { get; }

        public CardDefinition(string id, string displayName, CardType type, int cost, int value)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Id 不能为空", nameof(id));
            }

            if (string.IsNullOrEmpty(displayName))
            {
                throw new ArgumentException("DisplayName 不能为空", nameof(displayName));
            }

            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), "Cost 不能小于 0");
            }

            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value 不能小于 0");
            }

            Id = id;
            DisplayName = displayName;
            Type = type;
            Cost = cost;
            Value = value;
        }
    }
}
