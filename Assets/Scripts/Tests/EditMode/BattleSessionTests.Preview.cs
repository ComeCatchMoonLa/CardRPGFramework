using CardRPGFramework.Core.Buffs;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 0.3 预览 = 结算。每个用例都先调 Preview* 记下数字，再真的打出 / 结束回合，断言 HP 差值等于预览值（双方 0 格挡）；
    /// 不和 DamageCalculator 对拍——Preview* 本身就是它的一行包装，对拍等于没测。
    /// </summary>
    public partial class BattleSessionTests
    {
        [Test]
        public void PreviewPlayerAttack_WithoutBuffs_EqualsBaseDamage_AndActualHpLoss()
        {
            var session = CreateSession(deck: BuildDeck(5));
            session.StartBattle();

            var preview = session.PreviewPlayerAttack(6);
            session.TryPlayCard(0);

            Assert.AreEqual(6, preview);
            Assert.AreEqual(36 - preview, session.Enemy.CurrentHp);
        }

        [Test]
        public void PreviewPlayerAttack_AgainstVulnerableEnemy_IsNine_AndEqualsActualHpLoss()
        {
            var session = CreateSession(deck: BuildDeck(5));
            session.StartBattle();
            session.Enemy.ApplyBuff(new VulnerableBuff(1));

            var preview = session.PreviewPlayerAttack(6);
            session.TryPlayCard(0);

            Assert.AreEqual(9, preview);
            Assert.AreEqual(36 - preview, session.Enemy.CurrentHp);
        }

        [Test]
        public void PreviewPlayerAttack_WithTwoStrengthAgainstVulnerableEnemy_IsTwelve_AndEqualsActualHpLoss()
        {
            var session = CreateSession(deck: BuildDeck(5));
            session.StartBattle();
            session.Player.ApplyBuff(new StrengthBuff(2));
            session.Enemy.ApplyBuff(new VulnerableBuff(1));

            var preview = session.PreviewPlayerAttack(6);
            session.TryPlayCard(0);

            // floor((6 + 2) × 1.5) = 12：力量先加、易伤后乘，不是 9 也不是 11。
            Assert.AreEqual(12, preview);
            Assert.AreEqual(36 - preview, session.Enemy.CurrentHp);
        }

        [Test]
        public void PreviewEnemyAttack_WithWeakEnemy_IsFour_AndEqualsActualPlayerHpLoss()
        {
            var session = CreateSession(deck: BuildDeck(10));
            session.StartBattle();
            session.Enemy.ApplyBuff(new WeakBuff(2));

            var preview = session.PreviewEnemyAttack();
            session.TryEndPlayerTurn();

            Assert.AreEqual(4, preview);
            Assert.AreEqual(40 - preview, session.Player.CurrentHp);
        }

        [Test]
        public void PreviewPlayerAttack_ReadsCurrentBuffs_BashOwnHitNotAmplified_LaterAttackIs()
        {
            var session = CreateSession(deck: DeckOf(Bash, 5));
            session.StartBattle();

            var bashPreview = session.PreviewPlayerAttack(8);
            session.TryPlayCard(0);
            var attackPreviewAfterBash = session.PreviewPlayerAttack(6);

            // 打出前敌人没有易伤，预览 8 与实际 8 一致；痛击上了易伤之后，其余攻击卡才变成 ×1.5。
            Assert.AreEqual(8, bashPreview);
            Assert.AreEqual(36 - bashPreview, session.Enemy.CurrentHp);
            Assert.AreEqual(9, attackPreviewAfterBash);
        }

        [Test]
        public void Preview_DoesNotChangeState_SameResultTwice_HpBuffsEnergyHandUntouched()
        {
            var session = CreateSession(deck: BuildDeck(5));
            session.StartBattle();
            session.Player.ApplyBuff(new StrengthBuff(2));
            session.Enemy.ApplyBuff(new VulnerableBuff(1));
            session.Enemy.ApplyBuff(new WeakBuff(2));

            var playerFirst = session.PreviewPlayerAttack(6);
            var playerSecond = session.PreviewPlayerAttack(6);
            var enemyFirst = session.PreviewEnemyAttack();
            var enemySecond = session.PreviewEnemyAttack();

            Assert.AreEqual(playerFirst, playerSecond);
            Assert.AreEqual(enemyFirst, enemySecond);
            Assert.AreEqual(40, session.Player.CurrentHp);
            Assert.AreEqual(36, session.Enemy.CurrentHp);
            Assert.AreEqual(2, session.Player.GetBuffStacks("strength"));
            Assert.AreEqual(1, session.Enemy.GetBuffStacks("vulnerable"));
            Assert.AreEqual(2, session.Enemy.GetBuffStacks("weak"));
            Assert.AreEqual(3, session.Energy);
            Assert.AreEqual(5, session.Hand.Count);
        }
    }
}
