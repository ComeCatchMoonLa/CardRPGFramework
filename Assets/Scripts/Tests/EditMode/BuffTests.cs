using CardRPGFramework.Core.Actions;
using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Combatants;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    public class BuffTests
    {
        [Test]
        public void AddStacks_IncreasesStacks()
        {
            var buff = new StrengthBuff(2);

            buff.AddStacks(3);

            Assert.AreEqual(5, buff.Stacks);
        }

        [Test]
        public void RemoveStacks_ClampsAtZero()
        {
            var buff = new StrengthBuff(2);

            buff.RemoveStacks(5);

            Assert.AreEqual(0, buff.Stacks);
        }

        [Test]
        public void StrengthBuff_Id_IsStrength()
        {
            Assert.AreEqual("strength", new StrengthBuff(1).Id);
        }

        [Test]
        public void PoisonBuff_Id_IsPoison()
        {
            Assert.AreEqual("poison", new PoisonBuff(1).Id);
        }

        [Test]
        public void PoisonBuff_OnTurnStart_StacksZero_DoesNotEnqueueHpLoss()
        {
            var queue = new ActionQueue();
            var owner = new CombatantState(30);
            var poison = new PoisonBuff(0);

            poison.OnTurnStart(owner, queue);
            queue.RunAll(new ActionContext(queue));

            Assert.AreEqual(30, owner.CurrentHp);
            Assert.AreEqual(0, poison.Stacks);
        }

        [Test]
        public void PoisonBuff_OnTurnStart_StacksGreaterThanZero_DealsHpLossIgnoringBlockAndDecrementsStack()
        {
            var queue = new ActionQueue();
            var owner = new CombatantState(30);
            owner.GainBlock(10);
            var poison = new PoisonBuff(3);

            poison.OnTurnStart(owner, queue);
            queue.RunAll(new ActionContext(queue));

            Assert.AreEqual(27, owner.CurrentHp);
            Assert.AreEqual(10, owner.Block);
            Assert.AreEqual(2, poison.Stacks);
        }
    }
}
