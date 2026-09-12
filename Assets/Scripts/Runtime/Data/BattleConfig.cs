using System.Collections.Generic;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Enemies;
using CardRPGFramework.Core.Relics;
using UnityEngine;

namespace CardRPGFramework.Data
{
    /// <summary>
    /// 一场战斗的 Inspector 配置资产，转换为 Core 可用的 BattleSetup、敌人定义、初始牌组与玩家遗物栏。
    /// </summary>
    [CreateAssetMenu(fileName = "BattleConfig", menuName = "CardRPG/Battle Config")]
    public sealed class BattleConfig : ScriptableObject
    {
        // 80 是原版铁甲战士的初始生命；EditMode 测试里的 40 是测试自己的常量，不跟这里走。
        [SerializeField] private int playerMaxHp = 80;
        // 敌人整体由一份 EnemyData 描述（血量 + 行动表），换敌人 = 换这个引用。
        [SerializeField] private EnemyData enemy;
        [SerializeField] private int energyPerTurn = 3;
        [SerializeField] private int handSize = 5;
        [SerializeField] private List<CardData> deck = new();
        // 允许为空：没有遗物也是合法配置。1.0 起遗物栏改由 Run 提供，这里是否保留给 TestScene 单场调试到时一并决定。
        [SerializeField] private List<RelicData> relics = new();

        public IReadOnlyList<CardData> Deck => deck;
        public IReadOnlyList<RelicData> Relics => relics;

        public BattleSetup ToSetup()
        {
            return new BattleSetup(playerMaxHp, energyPerTurn, handSize);
        }

        public EnemyDefinition ToEnemyDefinition() => enemy.ToDefinition();

        public List<CardDefinition> ToDeckDefinitions()
        {
            var result = new List<CardDefinition>(deck.Count);
            foreach (var card in deck)
            {
                result.Add(card.ToDefinition());
            }

            return result;
        }

        public List<RelicState> ToRelicStates()
        {
            var result = new List<RelicState>(relics.Count);
            foreach (var relic in relics)
            {
                result.Add(relic.ToState());
            }

            return result;
        }

        /// <summary>启动期配置校验：空引用、非法数值、空 ID、不同资产间的重复 ID、遗物重复；敌人资产的字段交给 EnemyData 自己查。</summary>
        public bool TryValidate(out string error)
        {
            if (playerMaxHp <= 0)
            {
                error = "BattleConfig: playerMaxHp 必须大于 0";
                return false;
            }

            if (enemy == null)
            {
                error = "BattleConfig: enemy 不能为空";
                return false;
            }

            if (!enemy.TryValidate(out error))
            {
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

            return TryValidateRelics(out error);
        }

        // 与牌组的重复检查语义不同：同一张 CardData 引用多次是正常的（牌组里有 5 张攻击），
        // 而遗物唯一，同一资产引用两次和两份资产同 Id 一样都是配置错误。
        private bool TryValidateRelics(out string error)
        {
            var seenIds = new HashSet<string>();
            foreach (var relic in relics)
            {
                if (relic == null)
                {
                    error = "BattleConfig: relics 中存在空引用";
                    return false;
                }

                if (!relic.TryValidate(out error))
                {
                    return false;
                }

                if (!seenIds.Add(relic.Id))
                {
                    error = $"BattleConfig: 遗物 id '{relic.Id}' 重复（'{relic.name}'），同一件遗物不能持有两份";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
