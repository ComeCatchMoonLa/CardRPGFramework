using System.Collections.Generic;
using System.Linq;
using CardRPGFramework.Core.Cards;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 0.4 消耗堆：能力牌靠类型、坚不可摧靠关键词，两条路径落到同一个牌区；Session 里没有按类型或卡的分支。
    /// 0.1 的 Strength() 辅助方法保持 Skill 不动——它继续覆盖"技能施加 Buff 后进弃牌"，能力牌路径用这里的 Inflame()。
    /// </summary>
    public partial class BattleSessionTests
    {
        // 力量强化改为能力牌（原版 Inflame）：1 费、自身 2 层力量；坚不可摧（Impervious）：2 费技能、30 格挡、消耗。
        private static CardDefinition Inflame() =>
            new("inflame", "力量强化", CardType.Power, cost: 1, new[] { EffectSpec.ApplyBuff(EffectTarget.Self, "strength", 2) });

        private static CardDefinition Impervious() =>
            new("impervious", "坚不可摧", CardType.Skill, cost: 2, new[] { EffectSpec.Block(30) }, exhaust: true);

        [Test]
        public void PlayPower_GoesToExhaustPile_NotDiscardPile()
        {
            var inflame = Inflame();
            var deck = BuildDeck(4);
            deck.Add(inflame);
            var session = CreateSession(deck: deck);
            session.StartBattle();

            session.TryPlayCard(new List<CardDefinition>(session.Hand).IndexOf(inflame));

            Assert.AreEqual(2, session.Player.GetBuffStacks("strength"));
            Assert.AreEqual(1, session.ExhaustPileCount);
            Assert.AreEqual(0, session.DiscardPileCount);
            Assert.AreEqual(4, session.Hand.Count);
        }

        [Test]
        public void PlayPower_AfterReshuffles_FourZonesSumToDeckSize_AndPowerNeverComesBack()
        {
            // 5 张牌、手牌 5：打出能力牌后每次结束回合都把剩下 4 张洗回抽牌堆再抽满——重洗只洗弃牌堆，
            // 所以每一手都是那 4 张，能力牌不再出现。四区之和恒等于牌组张数，只写三区之和分不清"进了消耗堆"和"牌丢了"。
            var inflame = Inflame();
            var deck = BuildDeck(4);
            deck.Add(inflame);
            var session = CreateSession(deck: deck);
            session.StartBattle();
            session.TryPlayCard(new List<CardDefinition>(session.Hand).IndexOf(inflame));

            for (var i = 0; i < 3; i++)
            {
                session.TryEndPlayerTurn();

                Assert.AreEqual(4, session.Hand.Count);
                CollectionAssert.DoesNotContain(session.Hand, inflame);
                Assert.AreEqual(1, session.ExhaustPileCount);
                Assert.AreEqual(deck.Count,
                    session.Hand.Count + session.DrawPileCount + session.DiscardPileCount + session.ExhaustPileCount);
            }
        }

        [Test]
        public void PlayImpervious_GainsThirtyBlock_AndGoesToExhaustPile()
        {
            var session = CreateSession(deck: DeckOf(Impervious, 5));
            session.StartBattle();

            session.TryPlayCard(0);

            Assert.AreEqual(30, session.Player.Block);
            Assert.AreEqual(1, session.Energy);
            Assert.AreEqual(1, session.ExhaustPileCount);
            Assert.AreEqual(0, session.DiscardPileCount);
        }

        [Test]
        public void ImperviousLeftInHand_AtEndOfTurn_GoesToDiscardPile_NotExhaustPile()
        {
            // 10 张牌（5 坚不可摧 + 5 攻击）、手牌 5：结束回合时整手进弃牌堆，下一手从剩下的 5 张抽、不触发重洗，
            // 所以弃牌堆里正好是刚才那一手。带消耗关键词但没打出的牌也在里面，消耗堆为 0。
            var deck = DeckOf(Impervious, 5);
            deck.AddRange(BuildDeck(5));
            var session = CreateSession(deck: deck);
            session.StartBattle();
            Assert.Greater(session.Hand.Count(card => card.Exhaust), 0, "固定种子下这一手应至少有一张坚不可摧，否则本用例测不到东西");

            session.TryEndPlayerTurn();

            Assert.AreEqual(5, session.DiscardPileCount);
            Assert.AreEqual(0, session.ExhaustPileCount);
            Assert.AreEqual(5, session.Hand.Count);
        }

        [Test]
        public void PlaySkillWithoutKeyword_StillGoesToDiscardPile()
        {
            // 0.1 的力量强化辅助方法仍是技能：技能施加 Buff 后进弃牌堆。资产把力量强化改成能力牌，不能把这条覆盖一起带走。
            var deck = new List<CardDefinition> { Strength(2), Strength(2), Strength(2), Strength(2), Strength(2) };
            var session = CreateSession(deck: deck);
            session.StartBattle();

            session.TryPlayCard(0);

            Assert.AreEqual(CardType.Skill, deck[0].Type);
            Assert.AreEqual(1, session.DiscardPileCount);
            Assert.AreEqual(0, session.ExhaustPileCount);
        }
    }
}
