using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Cards;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 0.2 三张锚点卡的行为测试。三张卡都只用 EffectSpec 列表构造，Session 里没有任何按卡的分支：
    /// 痛击证明多条效果按顺序结算，双击证明同效果两段各自走公式与取整，剑柄打击证明"先离手、再结算、再进弃牌"的时序。
    /// </summary>
    public partial class BattleSessionTests
    {
        // 数值取原版：痛击 2 费、8 伤 + 2 层易伤；双击 1 费、5 + 5；剑柄打击 1 费、9 伤 + 抽 1。
        private static CardDefinition Bash() =>
            new("bash", "痛击", CardType.Attack, cost: 2, new[]
            {
                EffectSpec.Damage(8),
                EffectSpec.ApplyBuff(EffectTarget.Opponent, "vulnerable", 2),
            });

        private static CardDefinition TwinStrike() =>
            new("twin-strike", "双击", CardType.Attack, cost: 1, new[] { EffectSpec.Damage(5), EffectSpec.Damage(5) });

        private static CardDefinition PommelStrike() =>
            new("pommel-strike", "剑柄打击", CardType.Attack, cost: 1, new[] { EffectSpec.Damage(9), EffectSpec.Draw(1) });

        private static List<CardDefinition> DeckOf(Func<CardDefinition> card, int count)
        {
            var deck = new List<CardDefinition>(count);
            for (var i = 0; i < count; i++) deck.Add(card());
            return deck;
        }

        [Test]
        public void Bash_DealsEightThenAppliesVulnerable_OwnHitNotAmplifiedByItsOwnDebuff()
        {
            var session = CreateSession(deck: DeckOf(Bash, 5));
            session.StartBattle();

            session.TryPlayCard(0);

            // 若易伤先于伤害结算，会是 36 - floor(8 × 1.5) = 24。
            Assert.AreEqual(28, session.Enemy.CurrentHp);
            Assert.AreEqual(2, session.Enemy.GetBuffStacks("vulnerable"));
            Assert.AreEqual(1, session.Energy);
        }

        [Test]
        public void TwinStrike_WithoutBuffs_DealsTenInTotal()
        {
            var session = CreateSession(deck: DeckOf(TwinStrike, 5));
            session.StartBattle();

            session.TryPlayCard(0);

            Assert.AreEqual(26, session.Enemy.CurrentHp);
        }

        [Test]
        public void TwinStrike_AgainstVulnerableEnemy_EachHitFlooredSeparately_DealsFourteenNotFifteen()
        {
            var session = CreateSession(deck: DeckOf(TwinStrike, 5));
            session.StartBattle();
            session.Enemy.ApplyBuff(new VulnerableBuff(1));

            session.TryPlayCard(0);

            // 每段 floor(5 × 1.5) = 7，合计 14；若把 10 当一段算会是 floor(10 × 1.5) = 15。
            Assert.AreEqual(22, session.Enemy.CurrentHp);
        }

        [Test]
        public void PommelStrike_HandSizeUnchanged_DiscardPlusOne_LastCardComesFromDrawPile()
        {
            // 6 张牌、手牌 5：开局抽牌堆剩 1 张。打出第 0 张后手牌应仍是 5 张，末张就是抽牌堆那一张。
            var session = CreateSession(deck: DeckOf(PommelStrike, 6));
            session.StartBattle();
            var initialHand = new List<CardDefinition>(session.Hand);
            var played = session.Hand[0];

            session.TryPlayCard(0);

            Assert.AreEqual(27, session.Enemy.CurrentHp);
            Assert.AreEqual(5, session.Hand.Count);
            Assert.AreEqual(1, session.DiscardPileCount);
            Assert.AreEqual(0, session.DrawPileCount);
            CollectionAssert.DoesNotContain(session.Hand, played);
            CollectionAssert.DoesNotContain(initialHand, session.Hand[4]);
        }

        [Test]
        public void PommelStrike_WhenDrawPileEmpty_ReshuffleDoesNotIncludeTheCardBeingPlayed()
        {
            // 5 张牌全在手上、抽牌堆为空：先打一张攻击进弃牌堆，再打剑柄打击。
            // 抽牌触发重洗时弃牌堆里只有那张攻击；正在结算的剑柄打击不在任何牌区，不会被洗回去再抽到。
            // 旧时序（先进弃牌堆再结算）下重洗会把两张都洗回去：抽完后 DrawPileCount 是 1、DiscardPileCount 是 0。
            var attack = Attack();
            var deck = DeckOf(PommelStrike, 4);
            deck.Add(attack);
            var session = CreateSession(deck: deck);
            session.StartBattle();
            session.TryPlayCard(new List<CardDefinition>(session.Hand).IndexOf(attack));
            var pommel = session.Hand[0];

            session.TryPlayCard(0);

            Assert.AreEqual(21, session.Enemy.CurrentHp);
            Assert.AreEqual(4, session.Hand.Count);
            Assert.AreSame(attack, session.Hand[3]);
            CollectionAssert.DoesNotContain(session.Hand, pommel);
            Assert.AreEqual(1, session.DiscardPileCount);
            Assert.AreEqual(0, session.DrawPileCount);
        }
    }
}
