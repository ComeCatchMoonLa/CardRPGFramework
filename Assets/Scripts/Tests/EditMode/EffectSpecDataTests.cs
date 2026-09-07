using System;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Data;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 校验的重点是 Core 静态工厂管不到的部分：kind 与 target 是否一致、buffId 是否已知。
    /// value 的范围 Core 也会拦，这里再测一次是因为 Inspector 配置错误应当在启动期报出，不是打出时抛异常。
    /// </summary>
    public class EffectSpecDataTests
    {
        [Test]
        public void TryValidate_UnknownBuffId_ReturnsError()
        {
            var data = new EffectSpecData(EffectKind.ApplyBuff, EffectTarget.Opponent, 2, "dexterity");

            Assert.IsFalse(data.TryValidate(out var error));
            StringAssert.Contains("dexterity", error);
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void TryValidate_ValueNotPositive_ReturnsError(int value)
        {
            var data = new EffectSpecData(EffectKind.Damage, EffectTarget.Opponent, value, null);

            Assert.IsFalse(data.TryValidate(out var error));
            StringAssert.Contains("value", error);
        }

        [Test]
        public void TryValidate_DamageTargetingSelf_ReturnsError()
        {
            var data = new EffectSpecData(EffectKind.Damage, EffectTarget.Self, 6, null);

            Assert.IsFalse(data.TryValidate(out var error));
            StringAssert.Contains("Opponent", error);
        }

        [TestCase(EffectKind.Block)]
        [TestCase(EffectKind.Heal)]
        [TestCase(EffectKind.Draw)]
        public void TryValidate_SelfOnlyKindTargetingOpponent_ReturnsError(EffectKind kind)
        {
            var data = new EffectSpecData(kind, EffectTarget.Opponent, 1, null);

            Assert.IsFalse(data.TryValidate(out var error));
            StringAssert.Contains("Self", error);
        }

        [TestCase(EffectKind.Damage, EffectTarget.Opponent, null)]
        [TestCase(EffectKind.Block, EffectTarget.Self, null)]
        [TestCase(EffectKind.Heal, EffectTarget.Self, null)]
        [TestCase(EffectKind.ApplyBuff, EffectTarget.Self, "strength")]
        [TestCase(EffectKind.ApplyBuff, EffectTarget.Opponent, "vulnerable")]
        [TestCase(EffectKind.Draw, EffectTarget.Self, null)]
        public void TryValidate_ConsistentEntry_ReturnsTrue(EffectKind kind, EffectTarget target, string buffId)
        {
            var data = new EffectSpecData(kind, target, 2, buffId);

            Assert.IsTrue(data.TryValidate(out var error));
            Assert.IsNull(error);
        }

        [Test]
        public void ToSpec_ApplyBuff_KeepsTargetBuffIdAndStacks()
        {
            var spec = new EffectSpecData(EffectKind.ApplyBuff, EffectTarget.Opponent, 3, "poison").ToSpec();

            Assert.AreEqual(EffectKind.ApplyBuff, spec.Kind);
            Assert.AreEqual(EffectTarget.Opponent, spec.Target);
            Assert.AreEqual("poison", spec.BuffId);
            Assert.AreEqual(3, spec.Value);
        }

        [Test]
        public void ToSpec_Damage_UsesFactoryTargetRegardlessOfConfiguredTarget()
        {
            // 这正是 TryValidate 必须查 kind / target 一致性的原因：工厂会静默把 Self 改回 Opponent。
            var spec = new EffectSpecData(EffectKind.Damage, EffectTarget.Self, 6, null).ToSpec();

            Assert.AreEqual(EffectTarget.Opponent, spec.Target);
        }

        [Test]
        public void ToSpec_ValueNotPositive_CoreFactoryStillThrows()
        {
            var data = new EffectSpecData(EffectKind.Block, EffectTarget.Self, 0, null);

            Assert.Throws<ArgumentOutOfRangeException>(() => data.ToSpec());
        }
    }
}
