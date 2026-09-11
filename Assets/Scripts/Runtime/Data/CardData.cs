using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Cards;
using UnityEngine;

namespace CardRPGFramework.Data
{
    /// <summary>
    /// 卡牌的 Inspector 配置资产，转换为 Core 可用的 CardDefinition。Core 不保留对本类的引用。
    /// </summary>
    [CreateAssetMenu(fileName = "CardData", menuName = "CardRPG/Card Data")]
    public sealed class CardData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private CardType type;
        // -1 是"未填"：0 费合法（原版有 0 费牌），拿 0 当默认会让忘了填费用的新牌当 0 费打出去、启动不报错。
        [SerializeField] private int cost = -1;
        // 能力牌不用勾：类型已决定进消耗堆（见 CardDefinition.ExhaustsWhenPlayed）；勾上的能力牌不报错也无额外效果。
        [SerializeField] private bool exhaust;
        [SerializeField] private List<EffectSpecData> effects = new();

        public string Id => id;
        public string DisplayName => displayName;
        public CardType Type => type;
        public int Cost => cost;
        public bool Exhaust => exhaust;

        public CardDefinition ToDefinition()
        {
            var specs = new EffectSpec[effects.Count];
            for (var i = 0; i < specs.Length; i++)
            {
                specs[i] = effects[i].ToSpec();
            }

            return new CardDefinition(id, displayName, type, cost, specs, exhaust);
        }

        /// <summary>启动期配置校验：字段本身是否合法。跨资产的重复 ID 由 BattleConfig 统一检查。</summary>
        public bool TryValidate(out string error)
        {
            if (string.IsNullOrEmpty(id))
            {
                error = $"CardData '{name}': id 不能为空";
                return false;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                error = $"CardData '{name}': displayName 不能为空";
                return false;
            }

            // 0.1 的 type 按 Attack=0 … Vulnerable=6 序列化；收成 { Attack, Skill } 后旧资产会留下越界整数，
            // Inspector 显示为空白而不报错，这里拦住，逼迫每份资产都被显式重设过。
            // 0.4 追加 Power = 2 后，残留的旧整数 2 会被当成合法的能力牌——这条校验拦不住它，只能靠人在 Inspector 里核对。
            if (!Enum.IsDefined(typeof(CardType), type))
            {
                error = $"CardData '{name}': type 值 {(int)type} 不是合法的 CardType，需要在 Inspector 里重设";
                return false;
            }

            if (cost < 0)
            {
                error = $"CardData '{name}': cost 不能小于 0（新建资产默认 -1，需要显式填写；0 费合法）";
                return false;
            }

            if (effects == null || effects.Count == 0)
            {
                error = $"CardData '{name}': effects 不能为空";
                return false;
            }

            for (var i = 0; i < effects.Count; i++)
            {
                if (!effects[i].TryValidate(out var effectError))
                {
                    error = $"CardData '{name}': effects[{i}] {effectError}";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
