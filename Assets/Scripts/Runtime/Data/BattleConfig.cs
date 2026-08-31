using System.Collections.Generic;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Cards;
using UnityEngine;

namespace CardRPGFramework.Data
{
    /// <summary>
    /// 一场战斗的 Inspector 配置资产，转换为 Core 可用的 BattleSetup 与初始牌组。
    /// </summary>
    [CreateAssetMenu(fileName = "BattleConfig", menuName = "CardRPG/Battle Config")]
    public sealed class BattleConfig : ScriptableObject
    {
        [SerializeField] private int playerMaxHp = 40;
        [SerializeField] private int enemyMaxHp = 36;
        [SerializeField] private int enemyDamage = 6;
        [SerializeField] private int energyPerTurn = 3;
        [SerializeField] private int handSize = 5;
        [SerializeField] private List<CardData> deck = new();

        public IReadOnlyList<CardData> Deck => deck;

        public BattleSetup ToSetup()
        {
            return new BattleSetup(playerMaxHp, enemyMaxHp, enemyDamage, energyPerTurn, handSize);
        }

        public List<CardDefinition> ToDeckDefinitions()
        {
            var result = new List<CardDefinition>(deck.Count);
            foreach (var card in deck)
            {
                result.Add(card.ToDefinition());
            }

            return result;
        }

        /// <summary>启动期配置校验：空引用、非法数值、空 ID、不同资产间的重复 ID。</summary>
        public bool TryValidate(out string error)
        {
            if (playerMaxHp <= 0)
            {
                error = "BattleConfig: playerMaxHp 必须大于 0";
                return false;
            }

            if (enemyMaxHp <= 0)
            {
                error = "BattleConfig: enemyMaxHp 必须大于 0";
                return false;
            }

            if (enemyDamage < 0)
            {
                error = "BattleConfig: enemyDamage 不能小于 0";
                return false;
            }

            if (energyPerTurn < 0)
            {
                error = "BattleConfig: energyPerTurn 不能小于 0";
                return false;
            }

            if (handSize < 0)
            {
                error = "BattleConfig: handSize 不能小于 0";
                return false;
            }

            if (deck == null || deck.Count == 0)
            {
                error = "BattleConfig: deck 不能为空";
                return false;
            }

            var seenIds = new Dictionary<string, CardData>();
            foreach (var card in deck)
            {
                if (card == null)
                {
                    error = "BattleConfig: deck 中存在空引用";
                    return false;
                }

                if (!card.TryValidate(out error))
                {
                    return false;
                }

                if (seenIds.TryGetValue(card.Id, out var existing) && existing != card)
                {
                    error = $"BattleConfig: 卡牌 id '{card.Id}' 被不同的 CardData 资产重复使用（'{existing.name}' 与 '{card.name}'）";
                    return false;
                }

                seenIds[card.Id] = card;
            }

            error = null;
            return true;
        }
    }
}
