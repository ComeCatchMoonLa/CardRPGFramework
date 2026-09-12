using System;
using System.Collections.Generic;
using System.Linq;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Enemies;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 0.7 回合型 Buff：虚弱 / 易伤的层数是剩余轮数，轮结束（敌人行动之后、下一玩家回合之前）双方各减 1，
    /// 敌人施加的跳过第一次减层，0 层从字典移除。敌人行动只在测试内定义，Data 层 0.7 仍拒绝空 effects。
    /// </summary>
    public partial class BattleSessionTests
    {
        // 蓝奴隶贩子的耙：原版 7 伤 + 给玩家 1 层虚弱。刺击 / 待机只用来控制"下一回合敌人做什么"，让每个用例只观察一件事。
        private static EnemyAction Rake() => new(new[]
        {
            EffectSpec.Damage(7),
            EffectSpec.ApplyBuff(EffectTarget.Opponent, "weak", 1),
        });

        private static EnemyAction Stab() => new(new[] { EffectSpec.Damage(12) });

        // 待机是 Core 允许的空行动，与 FixedAttacker(maxHp, 0) 同一写法；不用 Damage(0)（工厂拒绝），也不为测试放开 Data 校验。
        private static EnemyAction Idle() => new(Array.Empty<EffectSpec>());

        private static EnemyAction SelfVulnerable() => new(new[] { EffectSpec.ApplyBuff(EffectTarget.Self, "vulnerable", 1) });

        private static EnemyDefinition EnemyWith(params EnemyAction[] actions) => new("round_end_enemy", "轮末测试敌人", 48, actions);

        private static int HandIndexOf(BattleSession session, string cardId)
        {
            for (var i = 0; i < session.Hand.Count; i++)
            {
                if (session.Hand[i].Id == cardId)
                {
                    return i;
                }
            }

            throw new InvalidOperationException($"手牌里没有 {cardId}");
        }

        private static bool HasBuffEntry(BattleSession session, bool player, string id) =>
            (player ? session.Player : session.Enemy).Buffs.Any(b => b.Id == id);

        [Test]
        public void Bash_TwoVulnerable_CoversThisTurnAndNext_ThenRemoved()
        {
            // 5 张牌整手在握、每回合弃掉再全部洗回来，回合 2 一定还拿得到攻击。
            var deck = new List<CardDefinition> { Bash(), Attack(), Attack(), Attack(), Attack() };
            var session = CreateSession(deck: deck);
            session.StartBattle();

            session.TryPlayCard(HandIndexOf(session, "bash"));
            Assert.AreEqual(2, session.Enemy.GetBuffStacks("vulnerable"));
            Assert.AreEqual(9, session.PreviewPlayerAttack(6));

            session.TryEndPlayerTurn();
            Assert.AreEqual(1, session.Enemy.GetBuffStacks("vulnerable"));

            var hpBefore = session.Enemy.CurrentHp;
            var preview = session.PreviewPlayerAttack(6);
            session.TryPlayCard(HandIndexOf(session, "attack"));
            Assert.AreEqual(9, preview);
            Assert.AreEqual(hpBefore - 9, session.Enemy.CurrentHp);

            session.TryEndPlayerTurn();
            Assert.AreEqual(0, session.Enemy.GetBuffStacks("vulnerable"));
            Assert.IsFalse(HasBuffEntry(session, player: false, "vulnerable"));
            Assert.AreEqual(6, session.PreviewPlayerAttack(6));
        }

        [Test]
        public void PlayerAppliedWeakOnEnemy_ReducesThisAttack_ThenExpiresAtRoundEnd()
        {
            var session = CreateSession(deck: DeckOf(() => Weak(1), 5));
            session.StartBattle();
            session.TryPlayCard(0);

            var preview = session.PreviewEnemyAttack();
            session.TryEndPlayerTurn();

            Assert.AreEqual(4, preview);
            Assert.AreEqual(40 - 4, session.Player.CurrentHp);
            Assert.IsFalse(session.Enemy.HasBuff("weak"));
            Assert.IsFalse(HasBuffEntry(session, player: false, "weak"));
            Assert.AreEqual(6, session.PreviewEnemyAttack());
        }

        [Test]
        public void Rake_WeakSurvivesTheRoundItWasApplied_ReducesNextTurnAttack_ThenExpires()
        {
            var session = CreateSession(deck: BuildDeck(10), enemy: EnemyWith(Rake(), Stab()));
            session.StartBattle();

            session.TryEndPlayerTurn();
            // 刚施加保护：本轮末不减，玩家下一回合还带着它；没有保护这 1 层在玩家出牌前就没了。
            Assert.AreEqual(1, session.Player.GetBuffStacks("weak"));
            Assert.AreEqual(40 - 7, session.Player.CurrentHp);

            var hpBefore = session.Enemy.CurrentHp;
            var preview = session.PreviewPlayerAttack(6);
            session.TryPlayCard(0);
            Assert.AreEqual(4, preview);
            Assert.AreEqual(hpBefore - 4, session.Enemy.CurrentHp);

            session.TryEndPlayerTurn();
            Assert.IsFalse(HasBuffEntry(session, player: true, "weak"));
            Assert.AreEqual(6, session.PreviewPlayerAttack(6));
        }

        [Test]
        public void RakeTwice_StacksWithoutRefreshingProtection_WeakLastsExactlyTwoPlayerTurns()
        {
            var session = CreateSession(deck: BuildDeck(0, defendCount: 10), enemy: EnemyWith(Rake(), Rake(), Idle(), Idle()));
            session.StartBattle();

            session.TryEndPlayerTurn();
            // 第一次耙：新建 1 层、带保护，轮末不减。
            Assert.AreEqual(1, session.Player.GetBuffStacks("weak"));
            Assert.AreEqual(4, session.PreviewPlayerAttack(6));

            session.TryEndPlayerTurn();
            // 第二次耙：叠到 2 层、保护不刷新，轮末减为 1。若叠加也刷新保护，这里会是 2。
            Assert.AreEqual(1, session.Player.GetBuffStacks("weak"));
            Assert.AreEqual(4, session.PreviewPlayerAttack(6));

            session.TryEndPlayerTurn();
            // 待机：轮末减为 0 并移除。两次施加各覆盖一个玩家回合，总共两个。
            Assert.AreEqual(0, session.Player.GetBuffStacks("weak"));
            Assert.IsFalse(HasBuffEntry(session, player: true, "weak"));
            Assert.AreEqual(6, session.PreviewPlayerAttack(6));
        }

        [Test]
        public void EnemySelfAppliedVulnerable_IsProtected_JudgedBySource_NotByTargetBeingPlayer()
        {
            var session = CreateSession(deck: BuildDeck(0, defendCount: 10), enemy: EnemyWith(SelfVulnerable(), Idle()));
            session.StartBattle();

            session.TryEndPlayerTurn();
            // 1v1 里痛击 / 耙分不出"施加方是敌人"与"目标是玩家"；这一例只有前者成立。
            // 若判定写成 target == Player，这 1 层会在本轮末被减到 0 并移除，下面两条都红。
            Assert.AreEqual(1, session.Enemy.GetBuffStacks("vulnerable"));
            Assert.AreEqual(9, session.PreviewPlayerAttack(6));

            session.TryEndPlayerTurn();
            Assert.IsFalse(HasBuffEntry(session, player: false, "vulnerable"));
            Assert.AreEqual(6, session.PreviewPlayerAttack(6));
        }

        [Test]
        public void Poison_IsNotDecrementedAtRoundEnd_OnlyAtEnemyTurnStart()
        {
            var session = CreateSession(deck: DeckOf(() => Poison(3), 5));
            session.StartBattle();
            session.TryPlayCard(0);

            session.TryEndPlayerTurn();

            // 敌人回合开始掉 3 血、自减 1；轮末不再减，否则会是 1。
            Assert.AreEqual(36 - 3, session.Enemy.CurrentHp);
            Assert.AreEqual(2, session.Enemy.GetBuffStacks("poison"));
        }

        [Test]
        public void Poison_DecrementedToZeroAtTurnStart_IsRemovedAtRoundEnd()
        {
            var session = CreateSession(deck: DeckOf(() => Poison(1), 5));
            session.StartBattle();
            session.TryPlayCard(0);

            session.TryEndPlayerTurn();

            Assert.AreEqual(36 - 1, session.Enemy.CurrentHp);
            Assert.IsFalse(session.Enemy.HasBuff("poison"));
            Assert.IsFalse(HasBuffEntry(session, player: false, "poison"));
        }

        [Test]
        public void PlayerOwnWeak_WithoutProtection_ReducesOwnAttack_AndExpiresAtRoundEnd()
        {
            var session = CreateSession(deck: BuildDeck(5));
            session.StartBattle();
            session.Player.ApplyBuff(new WeakBuff(1));

            Assert.AreEqual(4, session.PreviewPlayerAttack(6));

            session.TryEndPlayerTurn();

            Assert.IsFalse(HasBuffEntry(session, player: true, "weak"));
            Assert.AreEqual(6, session.PreviewPlayerAttack(6));
        }

        [Test]
        public void Defeat_SkipsRoundEnd_EnemyBuffsUntouched()
        {
            var session = CreateSession(playerMaxHp: 5, deck: BuildDeck(5));
            session.StartBattle();
            session.Enemy.ApplyBuff(new VulnerableBuff(1));

            session.TryEndPlayerTurn();

            Assert.AreEqual(BattlePhase.Defeat, session.Phase);
            // 轮结束没有执行：执行了的话这 1 层会被减到 0 并移除。
            Assert.AreEqual(1, session.Enemy.GetBuffStacks("vulnerable"));
        }
    }
}
