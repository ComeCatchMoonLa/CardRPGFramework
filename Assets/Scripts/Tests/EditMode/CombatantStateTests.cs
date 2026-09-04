using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Combatants;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    public class CombatantStateTests
    {
        [Test]
        public void TakeDamage_AbsorbedByBlockFirst_RemainderHitsHp()
        {
            var state = new CombatantState(30);
            state.GainBlock(5);

            state.TakeDamage(8);

            Assert.AreEqual(0, state.Block);
            Assert.AreEqual(27, state.CurrentHp);
        }

        [Test]
        public void TakeDamage_FullyAbsorbedByBlock_NoHpLoss()
        {
            var state = new CombatantState(30);
            state.GainBlock(10);

            state.TakeDamage(6);

            Assert.AreEqual(4, state.Block);
            Assert.AreEqual(30, state.CurrentHp);
        }

        [Test]
        public void TakeDamage_ExceedsCurrentHp_ClampsAtZero()
        {
            var state = new CombatantState(10);

            state.TakeDamage(999);

            Assert.AreEqual(0, state.CurrentHp);
            Assert.IsTrue(state.IsDead);
        }

        [Test]
        public void Heal_ClampedAtMaxHp()
        {
            var state = new CombatantState(20);
            state.TakeDamage(5);

            state.Heal(999);

            Assert.AreEqual(20, state.CurrentHp);
        }

        [Test]
        public void Heal_AtFullHp_ValueIsZeroEffective()
        {
            var state = new CombatantState(20);

            state.Heal(4);

            Assert.AreEqual(20, state.CurrentHp);
        }

        [Test]
        public void ClearBlock_ResetsBlockToZero()
        {
            var state = new CombatantState(20);
            state.GainBlock(5);

            state.ClearBlock();

            Assert.AreEqual(0, state.Block);
        }

        [Test]
        public void LoseHp_IgnoresBlock()
        {
            var state = new CombatantState(30);
            state.GainBlock(10);

            state.LoseHp(6);

            Assert.AreEqual(24, state.CurrentHp);
            Assert.AreEqual(10, state.Block);
        }

        [Test]
        public void LoseHp_ExceedsCurrentHp_ClampsAtZero()
        {
            var state = new CombatantState(10);

            state.LoseHp(999);

            Assert.AreEqual(0, state.CurrentHp);
            Assert.IsTrue(state.IsDead);
        }

        [Test]
        public void ApplyBuff_FirstApplication_StoresStacks()
        {
            var state = new CombatantState(30);

            state.ApplyBuff(new StrengthBuff(2));

            Assert.AreEqual(2, state.GetBuffStacks("strength"));
            Assert.IsTrue(state.HasBuff("strength"));
        }

        [Test]
        public void ApplyBuff_SameId_StacksAdd()
        {
            var state = new CombatantState(30);
            state.ApplyBuff(new StrengthBuff(2));

            state.ApplyBuff(new StrengthBuff(3));

            Assert.AreEqual(5, state.GetBuffStacks("strength"));
        }

        [Test]
        public void HasBuff_WithoutApplying_ReturnsFalse()
        {
            var state = new CombatantState(30);

            Assert.IsFalse(state.HasBuff("poison"));
            Assert.AreEqual(0, state.GetBuffStacks("poison"));
        }
    }
}
