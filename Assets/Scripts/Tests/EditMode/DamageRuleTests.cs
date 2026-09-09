using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Combatants;
using CardRPGFramework.Core.Relics;
using CardRPGFramework.Core.Rules;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 规则和计算器都只读 CombatantState 上的 Buff、只写 DamageContext.Damage，
    /// 这里不经过 Action/Effect，也不检查目标生命值有没有变。
    /// </summary>
    public class DamageRuleTests
    {
        private const double Tolerance = 1e-9;

        private static DamageContext Context(int baseDamage, CombatantState source, CombatantState target)
        {
            return new DamageContext(baseDamage, source, target);
        }

        private static CombatantState Plain() => new(30);

        private static CombatantState With(BuffState buff)
        {
            var state = new CombatantState(30);
            state.ApplyBuff(buff);
            return state;
        }

        // ---------- StrengthDamageRule ----------

        [Test]
        public void StrengthRule_SourceWithoutStrength_DamageUnchanged()
        {
            var context = Context(6, Plain(), Plain());

            new StrengthDamageRule().Apply(context);

            Assert.AreEqual(6, context.Damage, Tolerance);
        }

        [Test]
        public void StrengthRule_AddsSourceStrengthStacks()
        {
            var context = Context(6, With(new StrengthBuff(3)), Plain());

            new StrengthDamageRule().Apply(context);

            Assert.AreEqual(9, context.Damage, Tolerance);
        }

        [Test]
        public void StrengthRule_IgnoresTargetStrength()
        {
            var context = Context(6, Plain(), With(new StrengthBuff(3)));

            new StrengthDamageRule().Apply(context);

            Assert.AreEqual(6, context.Damage, Tolerance);
        }

        // ---------- WeakDamageRule ----------

        [Test]
        public void WeakRule_SourceWithoutWeak_DamageUnchanged()
        {
            var context = Context(6, Plain(), Plain());

            new WeakDamageRule().Apply(context);

            Assert.AreEqual(6, context.Damage, Tolerance);
        }

        [Test]
        public void WeakRule_SourceHasWeak_Multiplies075()
        {
            var context = Context(6, With(new WeakBuff(1)), Plain());

            new WeakDamageRule().Apply(context);

            Assert.AreEqual(4.5, context.Damage, Tolerance);
        }

        [Test]
        public void WeakRule_TwoStacksSameAsOne()
        {
            var context = Context(6, With(new WeakBuff(2)), Plain());

            new WeakDamageRule().Apply(context);

            Assert.AreEqual(4.5, context.Damage, Tolerance);
        }

        [Test]
        public void WeakRule_IgnoresTargetWeak()
        {
            var context = Context(6, Plain(), With(new WeakBuff(1)));

            new WeakDamageRule().Apply(context);

            Assert.AreEqual(6, context.Damage, Tolerance);
        }

        // ---------- WeakDamageRule × 纸鹤（改常数，读目标的遗物） ----------

        private static CombatantState WithPaperKrane(CombatantState state)
        {
            state.AddRelic(new PaperKraneRelic());
            return state;
        }

        [Test]
        public void WeakRule_TargetHoldsPaperKrane_Multiplies06()
        {
            var context = Context(6, With(new WeakBuff(1)), WithPaperKrane(Plain()));

            new WeakDamageRule().Apply(context);

            Assert.AreEqual(3.6, context.Damage, Tolerance);
        }

        [Test]
        public void WeakRule_TargetHoldsPaperKrane_TwoStacksSameAsOne()
        {
            var context = Context(6, With(new WeakBuff(2)), WithPaperKrane(Plain()));

            new WeakDamageRule().Apply(context);

            Assert.AreEqual(3.6, context.Damage, Tolerance);
        }

        [Test]
        public void WeakRule_SourceHoldsPaperKrane_StillMultiplies075()
        {
            // 纸鹤是"打你的虚弱敌人更弱"：持有者自己带虚弱去打人，目标没有纸鹤，仍是 0.75。
            var context = Context(6, WithPaperKrane(With(new WeakBuff(1))), Plain());

            new WeakDamageRule().Apply(context);

            Assert.AreEqual(4.5, context.Damage, Tolerance);
        }

        [Test]
        public void WeakRule_TargetHoldsPaperKrane_SourceWithoutWeak_DamageUnchanged()
        {
            // 纸鹤只改常数，不给攻击方加虚弱：没有虚弱就没有任何减伤。
            var context = Context(6, Plain(), WithPaperKrane(Plain()));

            new WeakDamageRule().Apply(context);

            Assert.AreEqual(6, context.Damage, Tolerance);
        }

        // ---------- VulnerableDamageRule ----------

        [Test]
        public void VulnerableRule_TargetWithoutVulnerable_DamageUnchanged()
        {
            var context = Context(6, Plain(), Plain());

            new VulnerableDamageRule().Apply(context);

            Assert.AreEqual(6, context.Damage, Tolerance);
        }

        [Test]
        public void VulnerableRule_TargetHasVulnerable_Multiplies15()
        {
            var context = Context(6, Plain(), With(new VulnerableBuff(1)));

            new VulnerableDamageRule().Apply(context);

            Assert.AreEqual(9, context.Damage, Tolerance);
        }

        [Test]
        public void VulnerableRule_IgnoresSourceVulnerable()
        {
            var context = Context(6, With(new VulnerableBuff(1)), Plain());

            new VulnerableDamageRule().Apply(context);

            Assert.AreEqual(6, context.Damage, Tolerance);
        }

        // ---------- DamageCalculator ----------

        [Test]
        public void Calculator_NoBuffs_ReturnsBaseDamage()
        {
            var result = DamageCalculator.CalculateFinalDamage(Context(6, Plain(), Plain()));

            Assert.AreEqual(6, result);
        }

        [Test]
        public void Calculator_StrengthAppliedBeforeVulnerable()
        {
            // (6 + 2) × 1.5 = 12；若乘法先跑则是 6 × 1.5 + 2 = 11，用这个差值锁定"加法在前"。
            var result = DamageCalculator.CalculateFinalDamage(
                Context(6, With(new StrengthBuff(2)), With(new VulnerableBuff(1))));

            Assert.AreEqual(12, result);
        }

        [Test]
        public void Calculator_WeakAndVulnerable_FloorsOnceAtEnd()
        {
            // 5 × 0.75 × 1.5 = 5.625 → 5；若虚弱后先取整再乘易伤：floor(3.75)=3 → 4.5 → 4。
            var result = DamageCalculator.CalculateFinalDamage(
                Context(5, With(new WeakBuff(1)), With(new VulnerableBuff(1))));

            Assert.AreEqual(5, result);
        }

        [Test]
        public void Calculator_AllThreeRules_MatchesDesignFormula()
        {
            // floor((6 + 3) × 0.75 × 1.5) = floor(10.125) = 10；中间取整会得 floor(6.75)=6 → 9。
            var source = With(new StrengthBuff(3));
            source.ApplyBuff(new WeakBuff(1));

            var result = DamageCalculator.CalculateFinalDamage(Context(6, source, With(new VulnerableBuff(1))));

            Assert.AreEqual(10, result);
        }

        [Test]
        public void Calculator_DesignDocExample_StrengthAndVulnerable()
        {
            // 游戏设计第 3 节例子：floor((6 + 3) × 1.5) = 13。
            var result = DamageCalculator.CalculateFinalDamage(
                Context(6, With(new StrengthBuff(3)), With(new VulnerableBuff(1))));

            Assert.AreEqual(13, result);
        }
    }
}
