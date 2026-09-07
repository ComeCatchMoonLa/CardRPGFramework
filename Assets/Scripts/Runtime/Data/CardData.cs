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
        [SerializeField] private int cost;
        [SerializeField] private List<EffectSpecData> effects = new();

        public string Id => id;
        public string DisplayName => displayName;
        public CardType Type => type;
        public int Cost => cost;

        public CardDefinition ToDefinition()
        {
            var specs = new EffectSpec[effects.Count];
            for (var i = 0; i < specs.Length; i++)
            {
                specs[i] = effects[i].ToSpec();
            }

            return new CardDefinition(id, displayName, type, cost, specs);
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
            if (!Enum.IsDefined(typeof(CardType), type))
            {
                error = $"CardData '{name}': type 值 {(int)type} 不是合法的 CardType，需要在 Inspector 里重设";
                return false;
            }

            if (cost < 0)
            {
                error = $"CardData '{name}': cost 不能小于 0";
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
