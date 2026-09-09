using System;
using CardRPGFramework.Core.Relics;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>Id 用字面量而不用 RelicIds：这里锁的是 RelicData 资产里要填的字符串契约，常量改错时应当在这里红。</summary>
    public class RelicFactoryTests
    {
        [TestCase("vajra", typeof(VajraRelic))]
        [TestCase("snecko_skull", typeof(SneckoSkullRelic))]
        [TestCase("paper_krane", typeof(PaperKraneRelic))]
        public void Create_KnownId_ReturnsMatchingRelicTypeWithSameId(string id, Type expectedType)
        {
            var relic = RelicFactory.Create(id);

            Assert.IsInstanceOf(expectedType, relic);
            Assert.AreEqual(id, relic.Id);
        }

        [Test]
        public void Create_UnknownId_Throws()
        {
            Assert.Throws<ArgumentException>(() => RelicFactory.Create("tungsten_rod"));
        }

        [TestCase("vajra")]
        [TestCase("snecko_skull")]
        [TestCase("paper_krane")]
        public void IsKnown_KnownId_ReturnsTrue(string id)
        {
            Assert.IsTrue(RelicFactory.IsKnown(id));
        }

        [TestCase("tungsten_rod")]
        [TestCase("")]
        [TestCase(null)]
        public void IsKnown_UnknownId_ReturnsFalse(string id)
        {
            Assert.IsFalse(RelicFactory.IsKnown(id));
        }
    }
}
