using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Enemies;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 0.6 Core.Enemies 的构造校验与拷贝语义。与 CardDefinitionTests 同一套关注点：拦 default(EffectSpec)、拷贝不泄漏；
    /// 敌人特有的是"Draw 在 Core 拒绝、空行动允许、行动元素引用不变"。
    /// </summary>
    public class EnemyDefinitionTests
    {
        private static EnemyAction Chomp() => new(new[] { EffectSpec.Damage(11) });

        [Test]
        public void EnemyAction_NullEffects_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new EnemyAction(null));
        }

        [Test]
        public void EnemyAction_ContainsDraw_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new EnemyAction(new[] { EffectSpec.Damage(6), EffectSpec.Draw(1) }));
        }

        [Test]
        public void EnemyAction_DefaultEffectSpecInList_Throws()
        {
            // new EffectSpec[n] 没填满的那一格绕过了静态工厂，必须像 CardDefinition 一样在构造期拦住。
            var effects = new EffectSpec[2];
            effects[0] = EffectSpec.Damage(6);

            Assert.Throws<ArgumentException>(() => new EnemyAction(effects));
        }

        [Test]
        public void EnemyAction_EmptyEffects_IsAllowed_AsIdle()
        {
            var action = new EnemyAction(Array.Empty<EffectSpec>());

            Assert.AreEqual(0, action.Effects.Count);
        }

        [Test]
        public void EnemyAction_CopiesEffects_LaterMutationOfSourceListDoesNotLeak()
        {
            var source = new List<EffectSpec> { EffectSpec.Damage(7) };
            var action = new EnemyAction(source);

            source[0] = EffectSpec.Heal(4);
            source.Add(EffectSpec.Block(5));

            Assert.AreEqual(1, action.Effects.Count);
            Assert.AreEqual(EffectKind.Damage, action.Effects[0].Kind);
            Assert.AreEqual(7, action.Effects[0].Value);
        }

        [TestCase(null)]
        [TestCase("")]
        public void EnemyDefinition_EmptyId_Throws(string id)
        {
            Assert.Throws<ArgumentException>(() => new EnemyDefinition(id, "颚虫", 42, new[] { Chomp() }));
        }

        [TestCase(null)]
        [TestCase("")]
        public void EnemyDefinition_EmptyDisplayName_Throws(string displayName)
        {
            Assert.Throws<ArgumentException>(() => new EnemyDefinition("jaw_worm", displayName, 42, new[] { Chomp() }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void EnemyDefinition_NonPositiveMaxHp_Throws(int maxHp)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyDefinition("jaw_worm", "颚虫", maxHp, new[] { Chomp() }));
        }

        [Test]
        public void EnemyDefinition_EmptyOrNullActions_Throws()
        {
            Assert.Throws<ArgumentException>(() => new EnemyDefinition("jaw_worm", "颚虫", 42, Array.Empty<EnemyAction>()));
            Assert.Throws<ArgumentException>(() => new EnemyDefinition("jaw_worm", "颚虫", 42, null));
        }

        [Test]
        public void EnemyDefinition_NullActionElement_Throws()
        {
            Assert.Throws<ArgumentException>(() => new EnemyDefinition("jaw_worm", "颚虫", 42, new[] { Chomp(), null }));
        }

        [Test]
        public void EnemyDefinition_CopiesActions_ElementsKeepReference()
        {
            // 列表拷贝、元素不拷贝：Session 的 CurrentEnemyAction 和测试的 Assert.AreSame 都靠元素引用认"是哪一条行动"。
            var chomp = Chomp();
            var thrash = new EnemyAction(new[] { EffectSpec.Damage(7), EffectSpec.Block(5) });
            var source = new List<EnemyAction> { chomp, thrash };
            var enemy = new EnemyDefinition("jaw_worm", "颚虫", 42, source);

            source.RemoveAt(0);
            source.Add(new EnemyAction(Array.Empty<EffectSpec>()));

            Assert.AreEqual("jaw_worm", enemy.Id);
            Assert.AreEqual("颚虫", enemy.DisplayName);
            Assert.AreEqual(42, enemy.MaxHp);
            Assert.AreEqual(2, enemy.Actions.Count);
            Assert.AreSame(chomp, enemy.Actions[0]);
            Assert.AreSame(thrash, enemy.Actions[1]);
        }
    }
}
