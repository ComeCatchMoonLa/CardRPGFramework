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
        [SerializeField] private int value;

        public string Id => id;
        public string DisplayName => displayName;
        public CardType Type => type;
        public int Cost => cost;
        public int Value => value;

        public CardDefinition ToDefinition()
        {
            return new CardDefinition(id, displayName, type, cost, value);
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

            if (cost < 0)
            {
                error = $"CardData '{name}': cost 不能小于 0";
                return false;
            }

            if (value < 0)
            {
                error = $"CardData '{name}': value 不能小于 0";
                return false;
            }

            error = null;
            return true;
        }
    }
}
