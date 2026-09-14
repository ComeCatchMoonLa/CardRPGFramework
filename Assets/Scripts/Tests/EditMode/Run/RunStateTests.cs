using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Enemies;
using CardRPGFramework.Core.Relics;
using CardRPGFramework.Core.Run;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    public class RunStateTests
    {
        private static CardDefinition Attack(string id) =>
            new(id, id, CardType.Attack, cost: 1, new[] { EffectSpec.Damage(6) });

        private static CardDefinition ExhaustAttack() =>
            new("exhaust_atk", "消耗打击", CardType.Attack, cost: 1, new[] { EffectSpec.Damage(6) }, exhaust: true);

        private static List<CardDefinition> TenAttacks()
        {
            var deck = new List<CardDefinition>(10);
            for (var i = 0; i < 10; i++)
            {
                deck.Add(Attack($"atk_{i}"));
            }

            return deck;
        }

        private static EnemyDefinition Enemy(string id) =>
            new(id, id, 20, new[] { new EnemyAction(new[] { EffectSpec.Damage(6) }) });

        private static EnemyDefinition[] ThreeEnemies() => new[] { Enemy("e0"), Enemy("e1"), Enemy("e2") };

        private static List<EnemyDefinition> Enemies(int count)
        {
            var list = new List<EnemyDefinition>(count);
            for (var i = 0; i < count; i++)
            {
                list.Add(Enemy($"e{i}"));
            }

            return list;
        }

        private static RunState CreateRun(
            int seed = 1,
            IReadOnlyList<CardDefinition> deck = null,
            IReadOnlyList<string> relicIds = null,
            IReadOnlyList<EnemyDefinition> encounters = null)
        {
            return new RunState(
                seed,
                new BattleSetup(80, 3, 5),
                deck ?? TenAttacks(),
                relicIds ?? new[] { RelicIds.Vajra },
                encounters ?? ThreeEnemies());
        }

        private static void WinCurrent(RunState run, int remainingHp)
        {
            run.CreateBattleInput();
            run.ApplyResult(true, remainingHp);
        }

        [Test]
        public void Constructor_StartsAtFullHp_NodeZero_GoldZero_NotOver()
        {
            var run = CreateRun();

            Assert.AreEqual(80, run.CurrentHp);
            Assert.AreEqual(80, run.MaxHp);
            Assert.AreEqual(0, run.NodeIndex);
            Assert.AreEqual(0, run.Gold);
            Assert.IsFalse(run.IsOver);
            Assert.IsFalse(run.IsFailed);
            Assert.IsFalse(run.IsCleared);
        }

        [Test]
        public void CreateBattleInput_SetupMatchesRun_DeckIsCopySameOrder()
        {
            var run = CreateRun();
            var input = run.CreateBattleInput();

            Assert.AreEqual(run.CurrentHp, input.Setup.PlayerCurrentHp);
            Assert.AreEqual(run.MaxHp, input.Setup.PlayerMaxHp);
            Assert.AreNotSame(run.Deck, input.Deck);
            CollectionAssert.AreEqual(run.Deck, input.Deck);

            ((List<CardDefinition>)input.Deck).Clear();
            Assert.AreEqual(10, run.Deck.Count);
        }

        [Test]
        public void CreateBattleInput_SameNodeTwice_DeckOrderUnchanged_DoesNotAdvance()
        {
            var run = CreateRun();
            var first = run.CreateBattleInput();
            var second = run.CreateBattleInput();

            CollectionAssert.AreEqual(run.Deck, first.Deck);
            CollectionAssert.AreEqual(first.Deck, second.Deck);
            Assert.AreEqual(0, run.NodeIndex);
        }

        [Test]
        public void CreateBattleInput_SameNodeTwice_RelicsAreNewInstances()
        {
            var run = CreateRun();
            var first = run.CreateBattleInput();
            var second = run.CreateBattleInput();

            Assert.AreEqual(1, first.Relics.Count);
            Assert.AreEqual(RelicIds.Vajra, first.Relics[0].Id);
            Assert.AreNotSame(first.Relics[0], second.Relics[0]);
        }

        [Test]
        public void CreateBattleInput_EnemyIsEncounterAtNodeIndex()
        {
            var encounters = ThreeEnemies();
            var run = CreateRun(encounters: encounters);

            Assert.AreSame(encounters[0], run.CreateBattleInput().Enemy);
            run.ApplyResult(true, 80);
            Assert.AreSame(encounters[1], run.CreateBattleInput().Enemy);
        }

        [Test]
        public void ApplyResult_Victory_NextInputHasRemainingHp()
        {
            var run = CreateRun();
            WinCurrent(run, 50);

            Assert.AreEqual(50, run.CurrentHp);
            Assert.AreEqual(1, run.NodeIndex);
            Assert.AreEqual(50, run.CreateBattleInput().Setup.PlayerCurrentHp);
        }

        [Test]
        public void AddCard_NextInputContainsNewCard_OriginalCardsRemain()
        {
            var original = TenAttacks();
            var run = CreateRun(deck: original);
            var reward = Attack("reward");

            run.AddCard(reward);
            var input = run.CreateBattleInput();

            Assert.AreEqual(11, input.Deck.Count);
            CollectionAssert.Contains(input.Deck, reward);
            CollectionAssert.IsSubsetOf(original, input.Deck);
        }

        [Test]
        public void ThreeVictories_AddCardSucceeds_CreateBattleInputAndApplyResultThrow()
        {
            var run = CreateRun();
            WinCurrent(run, 70);
            WinCurrent(run, 60);
            WinCurrent(run, 50);

            Assert.IsTrue(run.IsCleared);
            Assert.AreEqual(3, run.NodeIndex);
            run.AddCard(Attack("reward"));
            Assert.AreEqual(11, run.Deck.Count);
            Assert.Throws<InvalidOperationException>(() => run.CreateBattleInput());
            Assert.Throws<InvalidOperationException>(() => run.ApplyResult(true, 50));
        }

        [Test]
        public void Failed_AddCardThrows_HpAndNodeUnchanged()
        {
            var run = CreateRun();
            run.CreateBattleInput();
            run.ApplyResult(false, 1);

            Assert.IsTrue(run.IsFailed);
            Assert.AreEqual(80, run.CurrentHp);
            Assert.AreEqual(0, run.NodeIndex);
            Assert.Throws<InvalidOperationException>(() => run.AddCard(Attack("reward")));
            Assert.Throws<InvalidOperationException>(() => run.CreateBattleInput());
            Assert.Throws<InvalidOperationException>(() => run.ApplyResult(true, 50));
        }

        [Test]
        public void ExhaustAndDiscard_DoNotWriteBackToRunDeck()
        {
            var exhaust = ExhaustAttack();
            var strike = Attack("atk_0");
            var deck = new List<CardDefinition> { exhaust, strike, Attack("atk_1"), Attack("atk_2"), Attack("atk_3") };
            var run = CreateRun(deck: deck, relicIds: Array.Empty<string>());
            var input = run.CreateBattleInput();
            var session = new BattleSession(input.Setup, input.Enemy, input.Deck, input.Random, input.Relics);
            session.StartBattle();

            Assert.IsTrue(session.TryPlayCard(new List<CardDefinition>(session.Hand).IndexOf(exhaust)));
            Assert.IsTrue(session.TryPlayCard(new List<CardDefinition>(session.Hand).IndexOf(strike)));
            Assert.AreEqual(1, session.ExhaustPileCount);
            Assert.AreEqual(1, session.DiscardPileCount);

            run.ApplyResult(true, run.CurrentHp);

            Assert.AreEqual(5, run.Deck.Count);
            CollectionAssert.AreEqual(deck, run.Deck);
        }

        [Test]
        public void CreateBattleInput_SameSeedAndNode_RandomSequenceMatchesFormula()
        {
            const int seed = 42;
            var run = CreateRun(seed: seed);
            var first = run.CreateBattleInput();
            var second = run.CreateBattleInput();
            var expected = new Random(unchecked(seed * 397 ^ 0));

            for (var i = 0; i < 8; i++)
            {
                var value = expected.Next();
                Assert.AreEqual(value, first.Random.Next());
                Assert.AreEqual(value, second.Random.Next());
            }

            Assert.AreEqual(0, run.NodeIndex);
        }

        [Test]
        public void DifferentNodes_BattleSeedMatchesFormulaForNodeOne()
        {
            const int seed = 42;
            var run = CreateRun(seed: seed);
            WinCurrent(run, 80);
            var input = run.CreateBattleInput();
            var expected = new Random(unchecked(seed * 397 ^ 1));

            Assert.AreEqual(1, run.NodeIndex);
            Assert.AreEqual(expected.Next(), input.Random.Next());
        }

        [Test]
        public void ApplyResult_TwiceOnSameNode_Throws_NodeIndexStaysOne()
        {
            var run = CreateRun();
            WinCurrent(run, 50);

            Assert.Throws<InvalidOperationException>(() => run.ApplyResult(true, 40));
            Assert.AreEqual(1, run.NodeIndex);
            Assert.AreEqual(50, run.CurrentHp);
        }

        [Test]
        public void ApplyResult_BeforeCreateBattleInput_Throws()
        {
            var run = CreateRun();

            Assert.Throws<InvalidOperationException>(() => run.ApplyResult(true, 80));
            Assert.AreEqual(0, run.NodeIndex);
            Assert.IsFalse(run.IsFailed);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ApplyResult_WonWithNonPositiveHp_Throws_NotFailed_NodeUnchanged(int remainingHp)
        {
            var run = CreateRun();
            run.CreateBattleInput();

            Assert.Throws<ArgumentOutOfRangeException>(() => run.ApplyResult(true, remainingHp));
            Assert.IsFalse(run.IsFailed);
            Assert.AreEqual(0, run.NodeIndex);
            Assert.AreEqual(80, run.CurrentHp);
        }

        [Test]
        public void ApplyResult_RemainingHpAboveMax_Throws_StateUnchanged()
        {
            var run = CreateRun();
            run.CreateBattleInput();

            Assert.Throws<ArgumentOutOfRangeException>(() => run.ApplyResult(true, run.MaxHp + 1));
            Assert.IsFalse(run.IsFailed);
            Assert.AreEqual(0, run.NodeIndex);
            Assert.AreEqual(80, run.CurrentHp);
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(4)]
        public void Constructor_EncountersNotThree_Throws(int count)
        {
            Assert.Throws<ArgumentException>(() => CreateRun(encounters: Enemies(count)));
        }

        [Test]
        public void Constructor_EncounterNullElement_Throws()
        {
            var encounters = ThreeEnemies();
            encounters[1] = null;

            Assert.Throws<ArgumentException>(() => CreateRun(encounters: encounters));
        }

        [Test]
        public void Constructor_DuplicateRelicId_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateRun(relicIds: new[] { RelicIds.Vajra, RelicIds.Vajra }));
        }

        [Test]
        public void Constructor_UnknownRelicId_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateRun(relicIds: new[] { "tungsten_rod" }));
        }

        [Test]
        public void Constructor_NullDeck_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new RunState(1, new BattleSetup(80, 3, 5), null, Array.Empty<string>(), ThreeEnemies()));
        }

        [Test]
        public void Constructor_NullRelicIds_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new RunState(1, new BattleSetup(80, 3, 5), TenAttacks(), null, ThreeEnemies()));
        }

        [Test]
        public void Constructor_EmptyRelicIds_Succeeds()
        {
            var run = CreateRun(relicIds: Array.Empty<string>());

            Assert.AreEqual(0, run.RelicIds.Count);
            Assert.AreEqual(0, run.CreateBattleInput().Relics.Count);
        }

        [Test]
        public void Constructor_EmptyDeck_Succeeds()
        {
            var run = CreateRun(deck: Array.Empty<CardDefinition>());

            Assert.AreEqual(0, run.Deck.Count);
        }

        [Test]
        public void AddCard_Null_Throws()
        {
            var run = CreateRun();

            Assert.Throws<ArgumentNullException>(() => run.AddCard(null));
        }
    }
}
