using System.Collections.Generic;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Enemies;
using CardRPGFramework.Core.Run;
using UnityEngine;

namespace CardRPGFramework.Data
{
    /// <summary>
    /// 一局短 Run 的开战配置。产品路径先 TryValidate 再 CreateRunState；生命 / 能量 / 手牌不写类型默认值。
    /// </summary>
    [CreateAssetMenu(fileName = "RunConfig", menuName = "CardRPG/Run Config")]
    public sealed class RunConfig : ScriptableObject
    {
        [SerializeField] private int runSeed;
        [SerializeField] private int playerMaxHp;
        [SerializeField] private int energyPerTurn;
        [SerializeField] private int handSize;
        [SerializeField] private List<CardData> deck = new();
        [SerializeField] private List<RelicData> relics = new();
        [SerializeField] private List<EnemyData> encounters = new();

        public int RunSeed => runSeed;
        public IReadOnlyList<RelicData> Relics => relics;

        /// <summary>假定已通过校验；不默默填 80 / 10 张 / 1 遗物。</summary>
        public RunState CreateRunState()
        {
            var setup = new BattleSetup(playerMaxHp, energyPerTurn, handSize);
            var deckDefs = new List<CardDefinition>(deck.Count);
            foreach (var card in deck)
            {
                deckDefs.Add(card.ToDefinition());
            }

            var relicIds = new List<string>(relics.Count);
            foreach (var relic in relics)
            {
                relicIds.Add(relic.Id);
            }

            var encounterDefs = new List<EnemyDefinition>(encounters.Count);
            foreach (var enemy in encounters)
            {
                encounterDefs.Add(enemy.ToDefinition());
            }

            return new RunState(runSeed, setup, deckDefs, relicIds, encounterDefs);
        }

        /// <summary>进战斗前的拒绝合同。SO 校验不写 EditMode。</summary>
        public bool TryValidate(out string error)
        {
            if (playerMaxHp <= 0)
            {
                error = "RunConfig: playerMaxHp 必须大于 0";
                return false;
            }

            if (energyPerTurn < 0)
            {
                error = "RunConfig: energyPerTurn 不能小于 0";
                return false;
            }

            if (handSize < 0)
            {
                error = "RunConfig: handSize 不能小于 0";
                return false;
            }

            if (deck == null || deck.Count == 0)
            {
                error = "RunConfig: deck 不能为空";
                return false;
            }

            var seenCardIds = new Dictionary<string, CardData>();
            foreach (var card in deck)
            {
                if (card == null)
                {
                    error = "RunConfig: deck 中存在空引用";
                    return false;
                }

                if (!card.TryValidate(out error))
                {
                    return false;
                }

                if (seenCardIds.TryGetValue(card.Id, out var existing) && existing != card)
                {
                    error = $"RunConfig: 卡牌 id '{card.Id}' 被不同的 CardData 资产重复使用（'{existing.name}' 与 '{card.name}'）";
                    return false;
                }

                seenCardIds[card.Id] = card;
            }

            if (!TryValidateRelics(out error))
            {
                return false;
            }

            if (encounters == null || encounters.Count != 3)
            {
                error = "RunConfig: encounters 必须恰好 3 个";
                return false;
            }

            for (var i = 0; i < encounters.Count; i++)
            {
                if (encounters[i] == null)
                {
                    error = $"RunConfig: encounters[{i}] 不能为空";
                    return false;
                }

                if (!encounters[i].TryValidate(out error))
                {
                    return false;
                }
            }

            error = null;
            return true;
        }

        private bool TryValidateRelics(out string error)
        {
            var seenIds = new HashSet<string>();
            foreach (var relic in relics)
            {
                if (relic == null)
                {
                    error = "RunConfig: relics 中存在空引用";
                    return false;
                }

                if (!relic.TryValidate(out error))
                {
                    return false;
                }

                if (!seenIds.Add(relic.Id))
                {
                    error = $"RunConfig: 遗物 id '{relic.Id}' 重复（'{relic.name}'），同一件遗物不能持有两份";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
