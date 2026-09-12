using System;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Enemies;
using CardRPGFramework.Views;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 纯函数：只认 EnemyAction 与一个预览数字。攻击数字由调用方（Core 预览）给出，这里不验证公式。
    /// </summary>
    public class IntentFormatterTests
    {
        private static EnemyAction Action(params EffectSpec[] effects) => new(effects);

        [Test]
        public void SingleDamage_ShowsAttackWithPreviewedNumber()
        {
            Assert.AreEqual("攻击 11", IntentFormatter.Format(Action(EffectSpec.Damage(11)), 11));
        }

        [Test]
        public void DamageThenBlock_ShowsAttackThenDefend()
        {
            Assert.AreEqual("攻击 7 · 防御", IntentFormatter.Format(Action(EffectSpec.Damage(7), EffectSpec.Block(5)), 7));
        }

        [Test]
        public void SelfBuffThenBlock_ShowsBuffThenDefend()
        {
            var bellow = Action(EffectSpec.ApplyBuff(EffectTarget.Self, "strength", 3), EffectSpec.Block(6));

            Assert.AreEqual("增益 · 防御", IntentFormatter.Format(bellow, 0));
        }

        [Test]
        public void DamageThenOpponentBuff_ShowsAttackThenDebuff()
        {
            var action = Action(EffectSpec.Damage(7), EffectSpec.ApplyBuff(EffectTarget.Opponent, "weak", 1));

            Assert.AreEqual("攻击 7 · 减益", IntentFormatter.Format(action, 7));
        }

        [Test]
        public void TwoDamageSegments_MergeIntoOneAttackItem()
        {
            Assert.AreEqual("攻击 8", IntentFormatter.Format(Action(EffectSpec.Damage(3), EffectSpec.Damage(3)), 8));
        }

        [Test]
        public void Heal_ShowsBuff()
        {
            Assert.AreEqual("增益", IntentFormatter.Format(Action(EffectSpec.Heal(5)), 0));
        }

        [Test]
        public void TwoBlocks_DeduplicateToOneDefend()
        {
            Assert.AreEqual("防御", IntentFormatter.Format(Action(EffectSpec.Block(5), EffectSpec.Block(5)), 0));
        }

        [Test]
        public void EmptyAction_ShowsIdle()
        {
            Assert.AreEqual("待机", IntentFormatter.Format(Action(), 0));
        }

        [Test]
        public void Order_FollowsFirstAppearance()
        {
            Assert.AreEqual("防御 · 攻击 7", IntentFormatter.Format(Action(EffectSpec.Block(5), EffectSpec.Damage(7)), 7));
        }

        [Test]
        public void NullAction_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => IntentFormatter.Format(null, 0));
        }
    }
}
