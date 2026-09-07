using System;
using System.Collections.Generic;
using System.Linq;
using CardRPGFramework.Core.Cards;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    public class CardPileTests
    {
        private static List<CardDefinition> BuildDeck(int count)
        {
            var deck = new List<CardDefinition>();
            for (var i = 0; i < count; i++)
            {
                deck.Add(new CardDefinition($"attack-{i}", "攻击", CardType.Attack, cost: 1, value: 6));
            }

            return deck;
        }

        [Test]
        public void Draw_MovesCardsFromDrawPileToHand()
        {
            var pile = new CardPile(BuildDeck(5), new Random(1));

            pile.Draw(3);

            Assert.AreEqual(3, pile.Hand.Count);
            Assert.AreEqual(2, pile.DrawPileCount);
        }

        [Test]
        public void Draw_ReshufflesDiscardPile_WhenDrawPileEmpty()
        {
            var pile = new CardPile(BuildDeck(3), new Random(1));
            pile.Draw(3);
            pile.DiscardHand();

            pile.Draw(2);

            Assert.AreEqual(2, pile.Hand.Count);
            Assert.AreEqual(0, pile.DiscardPileCount);
            Assert.AreEqual(1, pile.DrawPileCount);
        }

        [Test]
        public void Draw_StopsSafely_WhenBothPilesEmpty()
        {
            var pile = new CardPile(BuildDeck(2), new Random(1));
            pile.Draw(2);

            Assert.DoesNotThrow(() => pile.Draw(3));
            Assert.AreEqual(2, pile.Hand.Count);
            Assert.AreEqual(0, pile.DrawPileCount);
            Assert.AreEqual(0, pile.DiscardPileCount);
        }

        [Test]
        public void PlayCard_MovesCardFromHandToDiscardPile()
        {
            var pile = new CardPile(BuildDeck(3), new Random(1));
            pile.Draw(2);

            var played = pile.PlayCard(0);

            Assert.AreEqual(1, pile.Hand.Count);
            Assert.AreEqual(1, pile.DiscardPileCount);
            Assert.IsNotNull(played);
        }

        [Test]
        public void TakeFromHand_RemovesCardFromHand_WithoutTouchingDiscardPile()
        {
            var pile = new CardPile(BuildDeck(3), new Random(1));
            pile.Draw(2);
            var expected = pile.Hand[0];

            var taken = pile.TakeFromHand(0);

            Assert.AreSame(expected, taken);
            Assert.AreEqual(1, pile.Hand.Count);
            Assert.AreEqual(0, pile.DiscardPileCount);
            Assert.AreEqual(1, pile.DrawPileCount);
        }

        [Test]
        public void AddToDiscard_AddsCardToDiscardPile_WithoutTouchingHand()
        {
            var pile = new CardPile(BuildDeck(3), new Random(1));
            pile.Draw(2);
            var taken = pile.TakeFromHand(0);

            pile.AddToDiscard(taken);

            Assert.AreEqual(1, pile.Hand.Count);
            Assert.AreEqual(1, pile.DiscardPileCount);
        }

        [Test]
        public void AddToDiscard_NullCard_Throws()
        {
            var pile = new CardPile(BuildDeck(1), new Random(1));

            Assert.Throws<ArgumentNullException>(() => pile.AddToDiscard(null));
        }

        [Test]
        public void TakeFromHand_ThenDrawTriggersReshuffle_TakenCardIsNotReshuffled()
        {
            // 模拟 0.2 的打出时序：牌离手后、进弃牌堆前发生抽牌且抽牌堆为空。
            // 此时只有弃牌堆里的两张会被洗回去，"结算中"的那张既不在手牌也不在两堆。
            var pile = new CardPile(BuildDeck(3), new Random(1));
            pile.Draw(2);
            pile.DiscardHand();
            pile.Draw(1);
            var taken = pile.TakeFromHand(0);

            pile.Draw(1);

            Assert.AreEqual(1, pile.Hand.Count);
            Assert.AreNotSame(taken, pile.Hand[0]);
            Assert.AreEqual(1, pile.DrawPileCount);
            Assert.AreEqual(0, pile.DiscardPileCount);

            pile.AddToDiscard(taken);

            Assert.AreEqual(1, pile.DiscardPileCount);
        }

        [Test]
        public void DiscardHand_MovesAllHandCardsToDiscardPile()
        {
            var pile = new CardPile(BuildDeck(5), new Random(1));
            pile.Draw(4);

            pile.DiscardHand();

            Assert.AreEqual(0, pile.Hand.Count);
            Assert.AreEqual(4, pile.DiscardPileCount);
        }

        [Test]
        public void SameSeed_ProducesSameShuffleOrder()
        {
            var pileA = new CardPile(BuildDeck(5), new Random(42));
            var pileB = new CardPile(BuildDeck(5), new Random(42));

            pileA.Draw(5);
            pileB.Draw(5);

            var idsA = pileA.Hand.Select(c => c.Id).ToList();
            var idsB = pileB.Hand.Select(c => c.Id).ToList();
            CollectionAssert.AreEqual(idsA, idsB);
        }
    }
}
