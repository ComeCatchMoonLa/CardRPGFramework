using System;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Views;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 纯函数，直接锁定文案：卡面描述与效果列表逐条一致是 0.2 的验收标准之一。
    /// 0.3 的带委托重载只换 Damage 行的数字、其余行不动，是卡面预览的生成器契约；无委托用例不改断言。
    /// </summary>
    public class CardDescriptionFormatterTests
    {
        private static CardDefinition Card(params EffectSpec[] effects) =>
            new("card", "卡", CardType.Attack, cost: 1, effects);

        [Test]
        public void Format_Damage() =>
            Assert.AreEqual("造成 6 点伤害", CardDescriptionFormatter.Format(Card(EffectSpec.Damage(6))));

        [Test]
        public void Format_Block() =>
            Assert.AreEqual("获得 5 点格挡", CardDescriptionFormatter.Format(Card(EffectSpec.Block(5))));

        [Test]
        public void Format_Heal() =>
            Assert.AreEqual("恢复 4 点生命", CardDescriptionFormatter.Format(Card(EffectSpec.Heal(4))));

        [Test]
        public void Format_ApplyBuffToSelf_UsesGainWording() =>
            Assert.AreEqual("获得 2 层力量",
                CardDescriptionFormatter.Format(Card(EffectSpec.ApplyBuff(EffectTarget.Self, "strength", 2))));

        [TestCase("poison", 3, "施加 3 层中毒")]
        [TestCase("weak", 2, "施加 2 层虚弱")]
        [TestCase("vulnerable", 2, "施加 2 层易伤")]
        public void Format_ApplyBuffToOpponent_UsesInflictWordingAndChineseName(string buffId, int stacks, string expected) =>
            Assert.AreEqual(expected,
                CardDescriptionFormatter.Format(Card(EffectSpec.ApplyBuff(EffectTarget.Opponent, buffId, stacks))));

        [Test]
        public void Format_Draw() =>
            Assert.AreEqual("抽 1 张牌", CardDescriptionFormatter.Format(Card(EffectSpec.Draw(1))));

        [Test]
        public void Format_MultipleEffects_OneLineEachInOrder()
        {
            var card = Card(EffectSpec.Damage(8), EffectSpec.ApplyBuff(EffectTarget.Opponent, "vulnerable", 2));

            Assert.AreEqual("造成 8 点伤害\n施加 2 层易伤", CardDescriptionFormatter.Format(card));
        }

        [Test]
        public void FormatWithPreview_TwinStrike_EachDamageLineConverted()
        {
            var card = Card(EffectSpec.Damage(5), EffectSpec.Damage(5));

            // 只换第一行 Damage 的实现在这里会露馅。
            Assert.AreEqual("造成 7 点伤害\n造成 7 点伤害", CardDescriptionFormatter.Format(card, _ => 7));
        }

        [Test]
        public void FormatWithPreview_Bash_DelegateReceivesBaseDamage_BuffLineUnchanged()
        {
            var card = Card(EffectSpec.Damage(8), EffectSpec.ApplyBuff(EffectTarget.Opponent, "vulnerable", 2));

            Assert.AreEqual("造成 12 点伤害\n施加 2 层易伤",
                CardDescriptionFormatter.Format(card, baseDamage => baseDamage + 4));
        }

        [Test]
        public void FormatWithPreview_BlockLineNotConverted()
        {
            var card = Card(EffectSpec.Damage(6), EffectSpec.Block(5));

            Assert.AreEqual("造成 99 点伤害\n获得 5 点格挡", CardDescriptionFormatter.Format(card, _ => 99));
        }

        [Test]
        public void FormatWithPreview_NullDelegate_Throws() =>
            Assert.Throws<ArgumentNullException>(() => CardDescriptionFormatter.Format(Card(EffectSpec.Damage(6)), null));
    }
}
