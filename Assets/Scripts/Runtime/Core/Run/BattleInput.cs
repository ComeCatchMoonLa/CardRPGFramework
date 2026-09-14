using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Enemies;
using CardRPGFramework.Core.Relics;

namespace CardRPGFramework.Core.Run
{
    /// <summary>
    /// CreateBattleInput 的返回值。五个字段：玩家标量、牌组浅拷贝、本场新建的遗物、当前节点敌人、交给调用方的 Random。
    /// </summary>
    public sealed class BattleInput
    {
        public BattleSetup Setup { get; }
        public IReadOnlyList<CardDefinition> Deck { get; }
        public IReadOnlyList<RelicState> Relics { get; }
        public EnemyDefinition Enemy { get; }
        public Random Random { get; }

        public BattleInput(BattleSetup setup, IReadOnlyList<CardDefinition> deck,
            IReadOnlyList<RelicState> relics, EnemyDefinition enemy, Random random)
        {
            Setup = setup;
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            Relics = relics ?? throw new ArgumentNullException(nameof(relics));
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            Random = random ?? throw new ArgumentNullException(nameof(random));
        }
    }
}
