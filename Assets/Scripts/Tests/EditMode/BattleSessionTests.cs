using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Cards;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    public class BattleSessionTests
    {
        private static CardDefinition Attack(int value = 6) => new("attack", "攻击", CardType.Attack, cost: 1, value: value);
        private static CardDefinition Defend(int value = 5) => new("defend", "防御", CardType.Defend, cost: 1, value: value);
        private static CardDefinition Heal(int value = 4) => new("heal", "治疗", CardType.Heal, cost: 1, value: value);

        private static List<CardDefinition> BuildDeck(int attackCount, int defendCount = 0, int healCount = 0)
        {
            var deck = new List<CardDefinition>();
            for (var i = 0; i < attackCount; i++) deck.Add(Attack());
            for (var i = 0; i < defendCount; i++) deck.Add(Defend());
            for (var i = 0; i < healCount; i++) deck.Add(Heal());
            return deck;
        }

        private static BattleSession CreateSession(
            int playerMaxHp = 40, int enemyMaxHp = 36, int enemyDamage = 6, int energyPerTurn = 3, int handSize = 5,
            List<CardDefinition> deck = null)
        {
            var setup = new BattleSetup(playerMaxHp, enemyMaxHp, enemyDamage, energyPerTurn, handSize);
            deck ??= BuildDeck(5, 3, 2);
            return new BattleSession(setup, deck, new Random(1));
        }

        [Test]
        public void StartBattle_EntersPlayerTurn_WithFullEnergyAndHand()
        {
            var session = CreateSession();

            session.StartBattle();

            Assert.AreEqual(BattlePhase.PlayerTurn, session.Phase);
            Assert.AreEqual(1, session.TurnNumber);
            Assert.AreEqual(3, session.Energy);
            Assert.AreEqual(5, session.Hand.Count);
        }

        [Test]
        public void StartBattle_CalledTwice_DoesNotReinitialize()
        {
            var session = CreateSession();
            session.StartBattle();
            session.TryPlayCard(0);

            session.StartBattle();

            Assert.AreEqual(1, session.TurnNumber);
        }

        [Test]
        public void TryPlayCard_BeforeBattleStarts_ReturnsFalse()
        {
            var session = CreateSession();

            Assert.IsFalse(session.TryPlayCard(0));
        }

        [Test]
        public void TryPlayCard_Attack_DamagesEnemyAndDeductsEnergy()
        {
            var session = CreateSession(deck: BuildDeck(5));
            session.StartBattle();

            var result = session.TryPlayCard(0);

            Assert.IsTrue(result);
            Assert.AreEqual(2, session.Energy);
            Assert.AreEqual(30, session.Enemy.CurrentHp);
        }

        [Test]
        public void TryPlayCard_Defend_GrantsBlockToPlayer()
        {
            var session = CreateSession(deck: BuildDeck(0, defendCount: 5));
            session.StartBattle();

            session.TryPlayCard(0);

            Assert.AreEqual(5, session.Player.Block);
        }

        [Test]
        public void TryPlayCard_Heal_RestoresPlayerHp()
        {
            var session = CreateSession(deck: BuildDeck(0, healCount: 5));
            session.StartBattle();
            session.Player.TakeDamage(10);

            session.TryPlayCard(0);

            Assert.AreEqual(34, session.Player.CurrentHp);
        }

        [Test]
        public void TryPlayCard_PlayedCard_MovesToDiscardPile()
        {
            var session = CreateSession(deck: BuildDeck(5));
            session.StartBattle();

            session.TryPlayCard(0);

            Assert.AreEqual(4, session.Hand.Count);
            Assert.AreEqual(1, session.DiscardPileCount);
        }

        [Test]
        public void TryPlayCard_NotEnoughEnergy_ReturnsFalseAndDoesNotConsumeCard()
        {
            var session = CreateSession(energyPerTurn: 0, deck: BuildDeck(5));
            session.StartBattle();

            var result = session.TryPlayCard(0);

            Assert.IsFalse(result);
            Assert.AreEqual(5, session.Hand.Count);
            Assert.AreEqual(36, session.Enemy.CurrentHp);
        }

        [Test]
        public void TryPlayCard_InvalidHandIndex_ReturnsFalse()
        {
            var session = CreateSession(deck: BuildDeck(5));
            session.StartBattle();

            Assert.IsFalse(session.TryPlayCard(-1));
            Assert.IsFalse(session.TryPlayCard(99));
        }

        [Test]
        public void TryEndPlayerTurn_DiscardsHandAndAppliesEnemyAttack()
        {
            var session = CreateSession(deck: BuildDeck(10));
            session.StartBattle();

            var result = session.TryEndPlayerTurn();

            Assert.IsTrue(result);
            Assert.AreEqual(BattlePhase.PlayerTurn, session.Phase);
            Assert.AreEqual(2, session.TurnNumber);
            Assert.AreEqual(34, session.Player.CurrentHp);
        }

        [Test]
        public void TryEndPlayerTurn_ClearsBlockBeforeNextTurn()
        {
            var session = CreateSession(deck: BuildDeck(0, defendCount: 10));
            session.StartBattle();
            session.TryPlayCard(0);

            session.TryEndPlayerTurn();

            Assert.AreEqual(0, session.Player.Block);
        }

        [Test]
        public void TryEndPlayerTurn_StartsNextTurnAndRefillsHand()
        {
            var session = CreateSession(handSize: 5, deck: BuildDeck(10));
            session.StartBattle();
            session.TryPlayCard(0);
            session.TryPlayCard(0);

            session.TryEndPlayerTurn();

            Assert.AreEqual(5, session.Hand.Count);
        }

        [Test]
        public void TryEndPlayerTurn_WhenNotPlayerTurn_ReturnsFalse()
        {
            var session = CreateSession();

            Assert.IsFalse(session.TryEndPlayerTurn());
        }

        [Test]
        public void EnemyDeath_DuringCardPlay_EndsBattleInVictory_WithoutEnemyAttack()
        {
            var session = CreateSession(enemyMaxHp: 6, deck: BuildDeck(5));
            session.StartBattle();

            session.TryPlayCard(0);

            Assert.AreEqual(BattlePhase.Victory, session.Phase);
            Assert.AreEqual(40, session.Player.CurrentHp);
        }

        [Test]
        public void PlayerDeath_AfterEnemyAttack_EndsBattleInDefeat()
        {
            var session = CreateSession(playerMaxHp: 5, enemyDamage: 6, deck: BuildDeck(10));
            session.StartBattle();

            session.TryEndPlayerTurn();

            Assert.AreEqual(BattlePhase.Defeat, session.Phase);
        }

        [Test]
        public void DrawPile_ReshufflesDiscardPile_AfterEnoughTurnsWithoutPlayingCards()
        {
            // 10 张牌、手牌上限 5：每回合结束弃掉整手牌，第 2 次结束回合后抽牌堆刚好耗尽，
            // 第 3 回合开始时必须靠重洗弃牌堆才能把手牌补满。
            var session = CreateSession(handSize: 5, deck: BuildDeck(5, 3, 2));
            session.StartBattle();

            session.TryEndPlayerTurn();
            session.TryEndPlayerTurn();

            Assert.AreEqual(3, session.TurnNumber);
            Assert.AreEqual(5, session.Hand.Count);
            Assert.AreEqual(5, session.DrawPileCount);
            Assert.AreEqual(0, session.DiscardPileCount);
        }

        [Test]
        public void ActionsAfterBattleEnds_AreRejected()
        {
            var session = CreateSession(enemyMaxHp: 6, deck: BuildDeck(5));
            session.StartBattle();
            session.TryPlayCard(0);
            Assert.AreEqual(BattlePhase.Victory, session.Phase);

            Assert.IsFalse(session.TryPlayCard(0));
            Assert.IsFalse(session.TryEndPlayerTurn());
        }
    }
}
