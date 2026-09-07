using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Actions;
using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Combatants;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    public class ActionQueueTests
    {
        /// <summary>只记录自己被执行过的顺序，不改任何战斗状态，用来验证 ActionQueue 的执行顺序与重入行为。</summary>
        private sealed class RecordingAction : IAction
        {
            private readonly List<string> _log;
            private readonly string _name;
            private readonly IAction _followUp;

            public RecordingAction(List<string> log, string name, IAction followUp = null)
            {
                _log = log;
                _name = name;
                _followUp = followUp;
            }

            public void Execute(ActionContext context)
            {
                _log.Add(_name);
                if (_followUp != null)
                {
                    context.Queue.Enqueue(_followUp);
                }
            }
        }

        [Test]
        public void RunAll_ExecutesActionsInEnqueueOrder()
        {
            var queue = new ActionQueue();
            var log = new List<string>();
            queue.Enqueue(new RecordingAction(log, "A"));
            queue.Enqueue(new RecordingAction(log, "B"));

            queue.RunAll(new ActionContext(queue));

            Assert.AreEqual(new List<string> { "A", "B" }, log);
        }

        [Test]
        public void RunAll_ActionEnqueuedDuringExecution_IsProcessedInSameRun()
        {
            var queue = new ActionQueue();
            var log = new List<string>();
            var followUp = new RecordingAction(log, "B");
            queue.Enqueue(new RecordingAction(log, "A", followUp));

            queue.RunAll(new ActionContext(queue));

            Assert.AreEqual(new List<string> { "A", "B" }, log);
        }

        [Test]
        public void DamageAction_Execute_DamagesTarget()
        {
            var queue = new ActionQueue();
            var source = new CombatantState(30);
            var target = new CombatantState(30);
            queue.Enqueue(new DamageAction(source, target, 6));

            queue.RunAll(new ActionContext(queue));

            Assert.AreEqual(24, target.CurrentHp);
        }

        [Test]
        public void BlockAction_Execute_GrantsBlockToTarget()
        {
            var queue = new ActionQueue();
            var target = new CombatantState(30);
            queue.Enqueue(new BlockAction(target, 5));

            queue.RunAll(new ActionContext(queue));

            Assert.AreEqual(5, target.Block);
        }

        [Test]
        public void HealAction_Execute_RestoresTargetHp()
        {
            var queue = new ActionQueue();
            var target = new CombatantState(30);
            target.TakeDamage(10);
            queue.Enqueue(new HealAction(target, 4));

            queue.RunAll(new ActionContext(queue));

            Assert.AreEqual(24, target.CurrentHp);
        }

        [Test]
        public void HpLossAction_Execute_IgnoresTargetBlock()
        {
            var queue = new ActionQueue();
            var target = new CombatantState(30);
            target.GainBlock(10);
            queue.Enqueue(new HpLossAction(target, 6));

            queue.RunAll(new ActionContext(queue));

            Assert.AreEqual(24, target.CurrentHp);
            Assert.AreEqual(10, target.Block);
        }

        [Test]
        public void ApplyBuffAction_Execute_AppliesBuffToTarget()
        {
            var queue = new ActionQueue();
            var source = new CombatantState(30);
            var target = new CombatantState(30);
            queue.Enqueue(new ApplyBuffAction(source, target, new StrengthBuff(2)));

            queue.RunAll(new ActionContext(queue));

            Assert.AreEqual(2, target.GetBuffStacks("strength"));
        }

        [Test]
        public void DrawCardsAction_Execute_DrawsFromDrawPileIntoHand()
        {
            var queue = new ActionQueue();
            var deck = new List<CardDefinition>();
            for (var i = 0; i < 3; i++)
            {
                deck.Add(new CardDefinition($"attack-{i}", "攻击", CardType.Attack, cost: 1, value: 6));
            }

            var pile = new CardPile(deck, new Random(1));
            queue.Enqueue(new DrawCardsAction(pile, 2));

            queue.RunAll(new ActionContext(queue));

            Assert.AreEqual(2, pile.Hand.Count);
            Assert.AreEqual(1, pile.DrawPileCount);
        }
    }
}
