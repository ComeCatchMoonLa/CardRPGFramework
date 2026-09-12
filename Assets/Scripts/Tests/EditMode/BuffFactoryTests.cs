using System;
using CardRPGFramework.Core.Buffs;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    public class BuffFactoryTests
    {
        [TestCase("strength", typeof(StrengthBuff))]
        [TestCase("poison", typeof(PoisonBuff))]
        [TestCase("weak", typeof(WeakBuff))]
        [TestCase("vulnerable", typeof(VulnerableBuff))]
        public void Create_KnownId_ReturnsMatchingBuffTypeWithStacks(string id, Type expectedType)
        {
            var buff = BuffFactory.Create(id, 3);

            Assert.IsInstanceOf(expectedType, buff);
            Assert.AreEqual(id, buff.Id);
            Assert.AreEqual(3, buff.Stacks);
        }

        [Test]
        public void Create_UnknownId_Throws()
        {
            Assert.Throws<ArgumentException>(() => BuffFactory.Create("dexterity", 1));
        }

        [Test]
        public void Create_WeakWithJustApplied_FirstRoundEndDoesNotDecrement_DefaultDoes()
        {
            var justApplied = BuffFactory.Create("weak", 1, justApplied: true);
            var plain = BuffFactory.Create("weak", 1);

            ((IRoundEndTrigger)justApplied).OnRoundEnd();
            ((IRoundEndTrigger)plain).OnRoundEnd();

            Assert.IsInstanceOf<WeakBuff>(justApplied);
            Assert.AreEqual(1, justApplied.Stacks);
            Assert.AreEqual(0, plain.Stacks);
        }

        [Test]
        public void Create_StrengthWithJustApplied_IgnoresFlag()
        {
            var buff = BuffFactory.Create("strength", 1, justApplied: true);

            Assert.IsInstanceOf<StrengthBuff>(buff);
            Assert.AreEqual(1, buff.Stacks);
            Assert.IsNotInstanceOf<IRoundEndTrigger>(buff);
        }

        [TestCase("strength")]
        [TestCase("poison")]
        [TestCase("weak")]
        [TestCase("vulnerable")]
        public void IsKnown_KnownId_ReturnsTrue(string id)
        {
            Assert.IsTrue(BuffFactory.IsKnown(id));
        }

        [TestCase("dexterity")]
        [TestCase("")]
        [TestCase(null)]
        public void IsKnown_UnknownId_ReturnsFalse(string id)
        {
            Assert.IsFalse(BuffFactory.IsKnown(id));
        }
    }
}
