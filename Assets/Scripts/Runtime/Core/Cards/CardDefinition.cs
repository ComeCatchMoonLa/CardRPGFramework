using System;
using System.Collections.Generic;

namespace CardRPGFramework.Core.Cards
{
    /// <summary>
    /// 不可变的卡牌纯 C# 数据，由 Data 层的 CardData 转换生成，Core 不保留 ScriptableObject 引用。
    /// 一张牌 = 类型 + 费用 + 效果列表；打出时按列表顺序逐条结算。
    /// </summary>
    public sealed class CardDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public CardType Type { get; }
        public int Cost { get; }

        /// <summary>至少一条；构造时拷贝，调用方之后改自己的列表不影响这里。</summary>
        public IReadOnlyList<EffectSpec> Effects { get; }

        public CardDefinition(string id, string displayName, CardType type, int cost, IReadOnlyList<EffectSpec> effects)
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

            if (effects == null || effects.Count == 0)
            {
                throw new ArgumentException("Effects 至少要有一条", nameof(effects));
            }

            var copy = new EffectSpec[effects.Count];
            for (var i = 0; i < copy.Length; i++)
            {
                // 静态工厂保证 Value > 0，所以 Value == 0 只可能是绕过工厂的 default(EffectSpec)，
                // 例如 new EffectSpec[n] 没填满；在这里拦住，别等到打出时才发现一段效果是空的。
                if (effects[i].Value <= 0)
                {
                    throw new ArgumentException($"Effects[{i}] 未经 EffectSpec 静态工厂构造", nameof(effects));
                }

                copy[i] = effects[i];
            }

            Id = id;
            DisplayName = displayName;
            Type = type;
            Cost = cost;
            Effects = copy;
        }
    }
}
