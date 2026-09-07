using System;
using CardRPGFramework.Core.Cards;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 只验证静态工厂产出的四元组与构造校验。EffectSpec 不带执行逻辑，
    /// "变成 Action 后打出去是多少"由 BattleSessionTests 负责。
    /// </summary>
    public class EffectSpecTests
    {
        [Test]
        public void Damage_TargetsOpponent_WithoutBuffId()
        {
            var spec = EffectSpec.Damage(6);

            Assert.AreEqual(EffectKind.Damage, spec.Kind);
            Assert.AreEqual(EffectTarget.Opponent, spec.Target);
            Assert.AreEqual(6, spec.Value);
            Assert.IsNull(spec.BuffId);
        }

        [Test]
        public void Block_TargetsSelf()
        {
            var spec = EffectSpec.Block(5);

            Assert.AreEqual(EffectKind.Block, spec.Kind);
            Assert.AreEqual(EffectTarget.Self, spec.Target);
            Assert.AreEqual(5, spec.Value);
        }

        [Test]
        public void Heal_TargetsSelf()
        {
            var spec = EffectSpec.Heal(4);

            Assert.AreEqual(EffectKind.Heal, spec.Kind);
            Assert.AreEqual(EffectTarget.Self, spec.Target);
            Assert.AreEqual(4, spec.Value);
        }

        [Test]
        public void ApplyBuff_KeepsTargetBuffIdAndStacks()
        {
            var spec = EffectSpec.ApplyBuff(EffectTarget.Opponent, "poison", 3);

            Assert.AreEqual(EffectKind.ApplyBuff, spec.Kind);
            Assert.AreEqual(EffectTarget.Opponent, spec.Target);
            Assert.AreEqual("poison", spec.BuffId);
            Assert.AreEqual(3, spec.Value);
        }

        [Test]
        public void Draw_AlwaysTargetsSelf()
        {
            // Draw 工厂不接收目标参数，"Draw 非 Self"在类型上就构造不出来，这里锁定的是工厂写死的值。
            var spec = EffectSpec.Draw(1);

            Assert.AreEqual(EffectKind.Draw, spec.Kind);
            Assert.AreEqual(EffectTarget.Self, spec.Target);
            Assert.AreEqual(1, spec.Value);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void AllFactories_ValueNotPositive_Throw(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EffectSpec.Damage(value));
            Assert.Throws<ArgumentOutOfRangeException>(() => EffectSpec.Block(value));
            Assert.Throws<ArgumentOutOfRangeException>(() => EffectSpec.Heal(value));
            Assert.Throws<ArgumentOutOfRangeException>(() => EffectSpec.ApplyBuff(EffectTarget.Opponent, "poison", value));
            Assert.Throws<ArgumentOutOfRangeException>(() => EffectSpec.Draw(value));
        }

        [TestCase(null)]
        [TestCase("")]
        public void ApplyBuff_MissingBuffId_Throws(string buffId)
        {
            Assert.Throws<ArgumentException>(() => EffectSpec.ApplyBuff(EffectTarget.Self, buffId, 2));
        }
    }
}
