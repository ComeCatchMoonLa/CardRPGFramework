using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Relics;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 0.1 的回归用例：后续版本只改构造、不改断言。0.2 起的新场景写在 BattleSessionTests.*.cs 的 partial 文件里，
    /// 共用这里的 CreateSession 与卡牌构造辅助方法。
    /// </summary>
    public partial class BattleSessionTests
    {
        private static CardDefinition Attack(int value = 6) =>
            new("attack", "攻击", CardType.Attack, cost: 1, new[] { EffectSpec.Damage(value) });

        private static CardDefinition Defend(int value = 5) =>
            new("defend", "防御", CardType.Skill, cost: 1, new[] { EffectSpec.Block(value) });

        private static CardDefinition Heal(int value = 4) =>
            new("heal", "治疗", CardType.Skill, cost: 1, new[] { EffectSpec.Heal(value) });

        private static CardDefinition Strength(int value = 2) =>
            new("strength", "力量强化", CardType.Skill, cost: 1, new[] { EffectSpec.ApplyBuff(EffectTarget.Self, "strength", value) });

        private static CardDefinition Poison(int value = 3) =>
            new("poison", "剧毒", CardType.Skill, cost: 1, new[] { EffectSpec.ApplyBuff(EffectTarget.Opponent, "poison", value) });

        private static CardDefinition Weak(int value = 2) =>
            new("weak", "虚弱", CardType.Skill, cost: 1, new[] { EffectSpec.ApplyBuff(EffectTarget.Opponent, "weak", value) });

        private static CardDefinition Vulnerable(int value = 2) =>
            new("vulnerable", "易伤", CardType.Skill, cost: 1, new[] { EffectSpec.ApplyBuff(EffectTarget.Opponent, "vulnerable", value) });

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
            List<CardDefinition> deck = null, IEnumerable<RelicState> relics = null)
        {
            var setup = new BattleSetup(playerMaxHp, enemyMaxHp, enemyDamage, energyPerTurn, handSize);
            deck ??= BuildDeck(5, 3, 2);
            return new BattleSession(setup, deck, new Random(1), relics);
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

        [Test]
        public void TryPlayCard_Strength_AppliesStacksToPlayer()
        {
            var deck = new List<CardDefinition> { Strength(2), Strength(2), Strength(2), Strength(2), Strength(2) };
            var session = CreateSession(deck: deck);
            session.StartBattle();

            session.TryPlayCard(0);

            Assert.AreEqual(2, session.Player.GetBuffStacks("strength"));
        }

        [Test]
        public void TryPlayCard_Poison_AppliesStacksToEnemy()
        {
            var deck = new List<CardDefinition> { Poison(3), Poison(3), Poison(3), Poison(3), Poison(3) };
            var session = CreateSession(deck: deck);
            session.StartBattle();

            session.TryPlayCard(0);

            Assert.AreEqual(3, session.Enemy.GetBuffStacks("poison"));
        }

        [Test]
        public void PoisonedEnemy_LosesHpIgnoringBlockAtEnemyTurnStart_AndStackDecrements()
        {
            // 牌堆全用同一种卡：CardPile 构造时会打乱抽牌堆顺序，
            // 混合卡种类时 hand[0] 不保证是想测的那张牌，必须固定成单一类型。
            var deck = new List<CardDefinition> { Poison(3), Poison(3), Poison(3), Poison(3), Poison(3) };
            var session = CreateSession(deck: deck);
            session.StartBattle();
            session.TryPlayCard(0);
            var enemyHpBeforeEnemyTurn = session.Enemy.CurrentHp;
            session.Enemy.GainBlock(10);

            session.TryEndPlayerTurn();

            Assert.AreEqual(enemyHpBeforeEnemyTurn - 3, session.Enemy.CurrentHp);
            Assert.AreEqual(10, session.Enemy.Block);
            Assert.AreEqual(2, session.Enemy.GetBuffStacks("poison"));
        }

        [Test]
        public void PoisonKillsEnemy_EndsBattleInVictory_WithoutEnemyAttack()
        {
            var deck = new List<CardDefinition> { Poison(99), Poison(99), Poison(99), Poison(99), Poison(99) };
            var session = CreateSession(enemyMaxHp: 3, deck: deck);
            session.StartBattle();
            session.TryPlayCard(0);

            session.TryEndPlayerTurn();

            Assert.AreEqual(BattlePhase.Victory, session.Phase);
            Assert.AreEqual(40, session.Player.CurrentHp);
        }

        [Test]
        public void PoisonedPlayer_DiesAtOwnTurnStart_EndsBattleInDefeat()
        {
            var session = CreateSession(playerMaxHp: 3, enemyDamage: 0, deck: BuildDeck(10));
            session.StartBattle();
            session.Player.ApplyBuff(new PoisonBuff(99));

            session.TryEndPlayerTurn();

            Assert.AreEqual(BattlePhase.Defeat, session.Phase);
        }

        [Test]
        public void EnemyAttack_WithoutAnyBuff_AbsorbedByBlockThenHitsHp_SameAsPhase1()
        {
            // 敌人攻击迁进 DamageAction 管线后的回归锁定：无 Buff 时先扣格挡再扣血，数值与 Phase 1 一致。
            var session = CreateSession(deck: BuildDeck(0, defendCount: 10));
            session.StartBattle();
            session.TryPlayCard(0);

            session.TryEndPlayerTurn();

            Assert.AreEqual(39, session.Player.CurrentHp);
        }

        [Test]
        public void TryPlayCard_Attack_WithPlayerStrength_DamageIncreasedByStacks()
        {
            var session = CreateSession(deck: BuildDeck(5));
            session.StartBattle();
            session.Player.ApplyBuff(new StrengthBuff(3));

            session.TryPlayCard(0);

            Assert.AreEqual(27, session.Enemy.CurrentHp);
        }

        [Test]
        public void TryPlayCard_Attack_WithEnemyVulnerable_DamageMultiplied()
        {
            var session = CreateSession(deck: BuildDeck(5));
            session.StartBattle();
            session.Enemy.ApplyBuff(new VulnerableBuff(1));

            session.TryPlayCard(0);

            Assert.AreEqual(27, session.Enemy.CurrentHp);
        }

        [Test]
        public void EnemyAttack_WithEnemyWeak_DamageReduced()
        {
            // 敌人身上的虚弱降低的是敌人自己的攻击：40 - floor(6 × 0.75) = 36。
            var session = CreateSession(deck: BuildDeck(10));
            session.StartBattle();
            session.Enemy.ApplyBuff(new WeakBuff(2));

            session.TryEndPlayerTurn();

            Assert.AreEqual(36, session.Player.CurrentHp);
        }

        [Test]
        public void TryPlayCard_Weak_AppliesStacksToEnemy()
        {
            var deck = new List<CardDefinition> { Weak(), Weak(), Weak(), Weak(), Weak() };
            var session = CreateSession(deck: deck);
            session.StartBattle();

            session.TryPlayCard(0);

            Assert.AreEqual(2, session.Enemy.GetBuffStacks("weak"));
        }

        [Test]
        public void TryPlayCard_Vulnerable_AppliesStacksToEnemy()
        {
            var deck = new List<CardDefinition> { Vulnerable(), Vulnerable(), Vulnerable(), Vulnerable(), Vulnerable() };
            var session = CreateSession(deck: deck);
            session.StartBattle();

            session.TryPlayCard(0);

            Assert.AreEqual(2, session.Enemy.GetBuffStacks("vulnerable"));
        }

        [Test]
        public void PlayWeakCardOnEnemy_ThenEndTurn_EnemyAttackReducedToFour()
        {
            // 端到端场景：虚弱卡打在敌人身上，敌人下一次固定攻击从 6 降为 4，
            // 不是玩家自己的攻击变低（虚弱卡的效果目标是 Opponent，见 ToAction）。
            var deck = new List<CardDefinition> { Weak(), Weak(), Weak(), Weak(), Weak() };
            var session = CreateSession(deck: deck);
            session.StartBattle();
            session.TryPlayCard(0);

            session.TryEndPlayerTurn();

            Assert.AreEqual(36, session.Player.CurrentHp);
        }
    }
}
