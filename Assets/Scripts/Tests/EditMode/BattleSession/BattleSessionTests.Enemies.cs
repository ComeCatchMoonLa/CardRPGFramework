using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Combatants;
using CardRPGFramework.Core.Enemies;
using CardRPGFramework.Core.Relics;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 0.6 敌人行动表：Session 拿到的是 EnemyDefinition，行动按固定顺序循环、每条行动经 ToAction 结算。
    /// 数值用例按 0.3 的做法先锁 PreviewEnemyAttack() 再比玩家 HP 差值（玩家 0 格挡）；牌堆默认全防御牌、只结束回合不打牌，
    /// 需要打牌的用例换成对应牌堆。颚虫的数值与 Assets/Data 里的资产一致，但测试不读资产。
    /// </summary>
    public partial class BattleSessionTests
    {
        /// <summary>颚虫：咬 11 → 猛击 7 + 格挡 5 → 咆哮 力量 3 + 格挡 6，固定循环。</summary>
        private static EnemyDefinition JawWorm() => new("jaw_worm", "颚虫", 42, new[]
        {
            new EnemyAction(new[] { EffectSpec.Damage(11) }),
            new EnemyAction(new[] { EffectSpec.Damage(7), EffectSpec.Block(5) }),
            new EnemyAction(new[] { EffectSpec.ApplyBuff(EffectTarget.Self, "strength", 3), EffectSpec.Block(6) }),
        });

        /// <summary>单一行动、两段攻击的敌人：验证多段各自过公式再求和。</summary>
        private static EnemyDefinition TwoHits(int first, int second) => new("two_hits", "双击敌人", 36, new[]
        {
            new EnemyAction(new[] { EffectSpec.Damage(first), EffectSpec.Damage(second) }),
        });

        /// <summary>对任何 Buff 都 +1 的施加修改器替身：用来证明 ApplyBuffAction 只看施加方的遗物。</summary>
        private sealed class PlusOneOnAnyBuff : RelicState, IApplyBuffModifier
        {
            public override string Id => "plus_one_on_any_buff";

            public void ModifyOutgoingBuff(CombatantState source, CombatantState target, BuffState buff) => buff.AddStacks(1);
        }

        private static BattleSession CreateJawWormSession(int playerMaxHp = 40) =>
            CreateSession(playerMaxHp: playerMaxHp, enemy: JawWorm(), deck: BuildDeck(0, defendCount: 10));

        [Test]
        public void Constructor_WithEnemyDefinition_EnemyHpFromDefinition_AndFirstActionIsCurrent()
        {
            var jawWorm = JawWorm();

            var session = CreateSession(enemy: jawWorm, deck: BuildDeck(0, defendCount: 10));

            Assert.AreEqual(42, session.Enemy.MaxHp);
            Assert.AreEqual(42, session.Enemy.CurrentHp);
            Assert.AreSame(jawWorm, session.EnemyDefinition);
            Assert.AreSame(jawWorm.Actions[0], session.CurrentEnemyAction);
        }

        [Test]
        public void EnemyActions_CycleInOrder_AndWrapAround()
        {
            var jawWorm = JawWorm();
            var session = CreateSession(enemy: jawWorm, deck: BuildDeck(0, defendCount: 10));
            session.StartBattle();
            Assert.AreSame(jawWorm.Actions[0], session.CurrentEnemyAction);

            session.TryEndPlayerTurn();
            Assert.AreSame(jawWorm.Actions[1], session.CurrentEnemyAction);
            session.TryEndPlayerTurn();
            Assert.AreSame(jawWorm.Actions[2], session.CurrentEnemyAction);
            session.TryEndPlayerTurn();
            Assert.AreSame(jawWorm.Actions[0], session.CurrentEnemyAction);
            session.TryEndPlayerTurn();
            Assert.AreSame(jawWorm.Actions[1], session.CurrentEnemyAction);

            Assert.AreEqual(BattlePhase.PlayerTurn, session.Phase);
        }

        [Test]
        public void JawWorm_Chomp_PreviewIsEleven_AndEqualsActualPlayerHpLoss()
        {
            var session = CreateJawWormSession();
            session.StartBattle();

            var preview = session.PreviewEnemyAttack();
            session.TryEndPlayerTurn();

            Assert.AreEqual(11, preview);
            Assert.AreEqual(40 - preview, session.Player.CurrentHp);
        }

        [Test]
        public void JawWorm_Thrash_PreviewIsSeven_EqualsActualHpLoss_ThenEnemyHasFiveBlock()
        {
            var session = CreateJawWormSession();
            session.StartBattle();
            session.TryEndPlayerTurn();
            var hpBeforeThrash = session.Player.CurrentHp;

            var preview = session.PreviewEnemyAttack();
            session.TryEndPlayerTurn();

            Assert.AreEqual(7, preview);
            Assert.AreEqual(hpBeforeThrash - preview, session.Player.CurrentHp);
            Assert.AreEqual(5, session.Enemy.Block);
        }

        [Test]
        public void JawWorm_ThrashBlock_AbsorbsPlayerAttackOnNextPlayerTurn()
        {
            var session = CreateSession(enemy: JawWorm(), deck: BuildDeck(10));
            session.StartBattle();
            session.TryEndPlayerTurn();
            session.TryEndPlayerTurn();
            Assert.AreEqual(5, session.Enemy.Block);

            session.TryPlayCard(0);

            // 6 点攻击打在 5 格挡上只掉 1 血：猛击的格挡顶着玩家整个下一回合。
            Assert.AreEqual(41, session.Enemy.CurrentHp);
            Assert.AreEqual(0, session.Enemy.Block);
        }

        [Test]
        public void JawWorm_EnemyBlockClearsAtItsOwnTurnStart_BellowLeavesSixNotEleven()
        {
            var session = CreateJawWormSession();
            session.StartBattle();
            session.TryEndPlayerTurn();
            session.TryEndPlayerTurn();
            Assert.AreEqual(5, session.Enemy.Block);

            session.TryEndPlayerTurn();

            // 咆哮前先清掉猛击剩下的 5，再获得 6；不清会是 11。
            Assert.AreEqual(6, session.Enemy.Block);
            Assert.AreEqual(3, session.Enemy.GetBuffStacks("strength"));
        }

        [Test]
        public void JawWorm_AfterBellow_ChompIsFourteen_ThenSecondThrashIsTen_PreviewEqualsActualHpLoss()
        {
            // 40 血撑不到第 5 回合（11 + 7 + 14 + 10 = 42），这条用 80。
            var session = CreateJawWormSession(playerMaxHp: 80);
            session.StartBattle();
            session.TryEndPlayerTurn();
            session.TryEndPlayerTurn();
            session.TryEndPlayerTurn();
            Assert.AreEqual(3, session.Enemy.GetBuffStacks("strength"));

            var chompPreview = session.PreviewEnemyAttack();
            var hpBeforeChomp = session.Player.CurrentHp;
            session.TryEndPlayerTurn();

            Assert.AreEqual(14, chompPreview);
            Assert.AreEqual(hpBeforeChomp - chompPreview, session.Player.CurrentHp);

            // 力量是永久的：第二圈的猛击跟着涨到 7 + 3。
            var thrashPreview = session.PreviewEnemyAttack();
            var hpBeforeThrash = session.Player.CurrentHp;
            session.TryEndPlayerTurn();

            Assert.AreEqual(10, thrashPreview);
            Assert.AreEqual(hpBeforeThrash - thrashPreview, session.Player.CurrentHp);
        }

        [Test]
        public void JawWorm_WithTwoWeak_ChompPreviewIsEight_AndEqualsActualPlayerHpLoss()
        {
            var session = CreateJawWormSession();
            session.StartBattle();
            session.Enemy.ApplyBuff(new WeakBuff(2));

            var preview = session.PreviewEnemyAttack();
            session.TryEndPlayerTurn();

            // floor(11 × 0.75) = 8。
            Assert.AreEqual(8, preview);
            Assert.AreEqual(40 - preview, session.Player.CurrentHp);
        }

        [Test]
        public void TwoHits_AgainstVulnerablePlayer_PreviewIsEightNotNine_AndEqualsActualHpLoss()
        {
            var session = CreateSession(enemy: TwoHits(3, 3), deck: BuildDeck(0, defendCount: 10));
            session.StartBattle();
            session.Player.ApplyBuff(new VulnerableBuff(1));

            var preview = session.PreviewEnemyAttack();
            session.TryEndPlayerTurn();

            // 每段各自取整：floor(3 × 1.5) + floor(3 × 1.5) = 4 + 4 = 8，不是 floor(6 × 1.5) = 9。
            Assert.AreEqual(8, preview);
            Assert.AreEqual(40 - preview, session.Player.CurrentHp);
        }

        [Test]
        public void IdleEnemy_EmptyAction_PreviewIsZero_AndPlayerHpUnchanged()
        {
            var session = CreateSession(enemyDamage: 0, deck: BuildDeck(0, defendCount: 10));
            session.StartBattle();

            Assert.AreEqual(0, session.CurrentEnemyAction.Effects.Count);
            Assert.AreEqual(0, session.PreviewEnemyAttack());

            session.TryEndPlayerTurn();

            Assert.AreEqual(40, session.Player.CurrentHp);
            Assert.AreEqual(BattlePhase.PlayerTurn, session.Phase);
        }

        [Test]
        public void PoisonKillsJawWorm_EndsInVictory_WithoutAdvancingActionPointer()
        {
            // 0.1 的 PoisonKillsEnemy 用单行动敌人，锁不住"毒死不推进指针"：误推进也会 % 1 回到同一条，所以这里用三条行动的颚虫。
            var jawWorm = JawWorm();
            var session = CreateSession(enemy: jawWorm, deck: DeckOf(() => Poison(99), 5));
            session.StartBattle();
            session.TryPlayCard(0);

            session.TryEndPlayerTurn();

            Assert.AreEqual(BattlePhase.Victory, session.Phase);
            Assert.AreSame(jawWorm.Actions[0], session.CurrentEnemyAction);
            Assert.AreEqual(40, session.Player.CurrentHp);
        }

        [Test]
        public void JawWorm_Bellow_PlayerApplyBuffModifierDoesNotTouchEnemySelfBuff()
        {
            var session = CreateSession(enemy: JawWorm(), deck: DeckOf(() => Strength(), 10),
                relics: new[] { new PlusOneOnAnyBuff() });
            session.StartBattle();

            // 玩家自己施加时替身生效：力量强化 2 → 3，证明它确实在钩子里。
            session.TryPlayCard(0);
            Assert.AreEqual(3, session.Player.GetBuffStacks("strength"));

            session.TryEndPlayerTurn();
            session.TryEndPlayerTurn();
            session.TryEndPlayerTurn();

            // 咆哮的施加方是敌人，敌人没有遗物：仍是 3，不是 4。
            Assert.AreEqual(3, session.Enemy.GetBuffStacks("strength"));
        }
    }
}
