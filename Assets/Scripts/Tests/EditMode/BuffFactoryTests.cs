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
