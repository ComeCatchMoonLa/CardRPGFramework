using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Cards;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    public class CardDefinitionTests
    {
        [Test]
        public void Constructor_EmptyEffects_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new CardDefinition("attack", "攻击", CardType.Attack, cost: 1, Array.Empty<EffectSpec>()));
        }

        [Test]
        public void Constructor_NullEffects_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new CardDefinition("attack", "攻击", CardType.Attack, cost: 1, null));
        }

        [Test]
        public void Constructor_DefaultEffectSpecInList_Throws()
        {
            // new EffectSpec[n] 没填满的那一格是 default(EffectSpec)，绕过了静态工厂的校验，必须在这里被拦住。
            var effects = new EffectSpec[2];
            effects[0] = EffectSpec.Damage(6);

            Assert.Throws<ArgumentException>(() =>
                new CardDefinition("attack", "攻击", CardType.Attack, cost: 1, effects));
        }

        [Test]
        public void Constructor_CopiesEffects_LaterMutationOfSourceListDoesNotLeak()
        {
            var source = new List<EffectSpec> { EffectSpec.Damage(6) };
            var card = new CardDefinition("attack", "攻击", CardType.Attack, cost: 1, source);

            source[0] = EffectSpec.Heal(4);
            source.Add(EffectSpec.Block(5));

            Assert.AreEqual(1, card.Effects.Count);
            Assert.AreEqual(EffectKind.Damage, card.Effects[0].Kind);
            Assert.AreEqual(6, card.Effects[0].Value);
        }

        [Test]
        public void Constructor_KeepsEffectOrder()
        {
            var card = new CardDefinition("bash", "痛击", CardType.Attack, cost: 2, new[]
            {
                EffectSpec.Damage(8),
                EffectSpec.ApplyBuff(EffectTarget.Opponent, "vulnerable", 2),
            });

            Assert.AreEqual(2, card.Effects.Count);
            Assert.AreEqual(EffectKind.Damage, card.Effects[0].Kind);
            Assert.AreEqual(EffectKind.ApplyBuff, card.Effects[1].Kind);
        }

        [Test]
        public void Exhaust_DefaultsToFalse_AttackGoesToDiscardPile()
        {
            var card = new CardDefinition("attack", "攻击", CardType.Attack, cost: 1, new[] { EffectSpec.Damage(6) });

            Assert.IsFalse(card.Exhaust);
            Assert.IsFalse(card.ExhaustsWhenPlayed);
        }

        [Test]
        public void ExhaustsWhenPlayed_Power_TrueByTypeWithoutKeyword()
        {
            var card = new CardDefinition("inflame", "力量强化", CardType.Power, cost: 1,
                new[] { EffectSpec.ApplyBuff(EffectTarget.Self, "strength", 2) });

            Assert.IsFalse(card.Exhaust);
            Assert.IsTrue(card.ExhaustsWhenPlayed);
        }

        [Test]
        public void ExhaustsWhenPlayed_SkillWithExhaustKeyword_True()
        {
            var card = new CardDefinition("impervious", "坚不可摧", CardType.Skill, cost: 2, new[] { EffectSpec.Block(30) },
                exhaust: true);

            Assert.IsTrue(card.Exhaust);
            Assert.IsTrue(card.ExhaustsWhenPlayed);
        }
    }
}
