using CardRPGFramework.Core.Combatants;
using CardRPGFramework.Core.Effects;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 只验证 Effect 是否正确转发到 CombatantState，不重复 CombatantStateTests 已覆盖的边界行为
    /// （格挡吸收、生命值下限/上限钳位等），也不经过 Action/ActionQueue。
    /// </summary>
    public class EffectTests
    {
        [Test]
        public void DamageEffect_Apply_ForwardsToTakeDamage()
        {
            var target = new CombatantState(30);

            DamageEffect.Apply(target, 6);

            Assert.AreEqual(24, target.CurrentHp);
        }

        [Test]
        public void BlockEffect_Apply_ForwardsToGainBlock()
        {
            var target = new CombatantState(30);

            BlockEffect.Apply(target, 5);

            Assert.AreEqual(5, target.Block);
        }

        [Test]
        public void HealEffect_Apply_ForwardsToHeal()
        {
            var target = new CombatantState(30);
            target.TakeDamage(10);

            HealEffect.Apply(target, 4);

            Assert.AreEqual(24, target.CurrentHp);
        }
    }
}
