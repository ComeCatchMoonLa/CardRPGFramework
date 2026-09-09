using System;
using CardRPGFramework.Core.Actions;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Combatants;
using CardRPGFramework.Core.Relics;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 0.5 遗物。前半用替身锁 Session 与遗物容器 / 开战钩子的接线——遗物落在玩家身上、StartBattle 只触发一次并结算队列；
    /// 后半是三件具体遗物的数值用例，按 0.3 的做法先锁预览再比 HP 差值（双方 0 格挡），蛇颅骨走真的打出剧毒而不是 ApplyBuff 直调。
    /// </summary>
    public partial class BattleSessionTests
    {
        /// <summary>只有 Id 的遗物替身。</summary>
        private sealed class InertRelic : RelicState
        {
            public InertRelic(string id) => Id = id;

            public override string Id { get; }
        }

        /// <summary>
        /// 开战钩子替身：记录被调用次数，并按约定只入队 Action——这里入队 1 点 HP 损失，
        /// 因为它在第一回合的清格挡之后仍可观测，且不依赖任何 Buff 逻辑。
        /// </summary>
        private sealed class RecordingStartRelic : RelicState, IBattleStartRelic
        {
            public override string Id => "recording_start";
            public int Calls { get; private set; }

            public void OnBattleStart(CombatantState owner, ActionQueue queue)
            {
                Calls++;
                queue.Enqueue(new HpLossAction(owner, 1));
            }
        }

        [Test]
        public void Constructor_WithRelics_PutsThemOnPlayerOnly()
        {
            var session = CreateSession(relics: new RelicState[] { new InertRelic("a"), new InertRelic("b") });

            Assert.AreEqual(2, session.Player.Relics.Count);
            Assert.IsTrue(session.Player.HasRelic("a"));
            Assert.IsTrue(session.Player.HasRelic("b"));
            Assert.AreEqual(0, session.Enemy.Relics.Count);
            Assert.AreEqual(0, session.Player.Buffs.Count);
        }

        [Test]
        public void Constructor_WithoutRelics_PlayerHasNone()
        {
            var session = CreateSession();

            Assert.AreEqual(0, session.Player.Relics.Count);
        }

        [Test]
        public void Constructor_DuplicateRelicIds_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateSession(relics: new RelicState[] { new InertRelic("a"), new InertRelic("a") }));
        }

        [Test]
        public void StartBattle_RunsBattleStartRelicOnce_ThenEntersFirstPlayerTurn()
        {
            var relic = new RecordingStartRelic();
            var session = CreateSession(relics: new[] { relic });

            session.StartBattle();

            Assert.AreEqual(1, relic.Calls);
            Assert.AreEqual(39, session.Player.CurrentHp);
            Assert.AreEqual(BattlePhase.PlayerTurn, session.Phase);
            Assert.AreEqual(1, session.TurnNumber);
            Assert.AreEqual(5, session.Hand.Count);
        }

        [Test]
        public void StartBattle_CalledTwice_DoesNotRunBattleStartRelicAgain()
        {
            var relic = new RecordingStartRelic();
            var session = CreateSession(relics: new[] { relic });
            session.StartBattle();

            session.StartBattle();

            Assert.AreEqual(1, relic.Calls);
            Assert.AreEqual(39, session.Player.CurrentHp);
        }

        [Test]
        public void StartBattle_RelicWithoutStartHook_IsIgnored()
        {
            var session = CreateSession(relics: new RelicState[] { new InertRelic("a") });

            session.StartBattle();

            Assert.AreEqual(40, session.Player.CurrentHp);
            Assert.AreEqual(BattlePhase.PlayerTurn, session.Phase);
        }

        // ---------- 金刚杵：开战注入 ----------

        [Test]
        public void Vajra_StartBattle_PlayerHasOneStrength_RelicStaysOutOfBuffs()
        {
            var session = CreateSession(deck: BuildDeck(5), relics: new[] { new VajraRelic() });

            session.StartBattle();

            Assert.AreEqual(1, session.Player.GetBuffStacks("strength"));
            Assert.AreEqual(1, session.Player.Buffs.Count);
            Assert.AreEqual(1, session.Player.Relics.Count);
            Assert.AreEqual(BattlePhase.PlayerTurn, session.Phase);
        }

        [Test]
        public void Vajra_PreviewPlayerAttack_IsSeven_AndEqualsActualHpLoss()
        {
            var session = CreateSession(deck: BuildDeck(5), relics: new[] { new VajraRelic() });
            session.StartBattle();

            var preview = session.PreviewPlayerAttack(6);
            session.TryPlayCard(0);

            Assert.AreEqual(7, preview);
            Assert.AreEqual(36 - preview, session.Enemy.CurrentHp);
        }

        [Test]
        public void NoRelics_StartBattle_NoStrength_AttackPreviewIsSix()
        {
            var session = CreateSession(deck: BuildDeck(5));
            session.StartBattle();

            Assert.AreEqual(0, session.Player.GetBuffStacks("strength"));
            Assert.AreEqual(6, session.PreviewPlayerAttack(6));
        }

        // ---------- 蛇颅骨：改这一次施加 ----------

        [Test]
        public void SneckoSkull_PlayPoison_EnemyGetsFourStacks()
        {
            var session = CreateSession(deck: DeckOf(() => Poison(), 5), relics: new[] { new SneckoSkullRelic() });
            session.StartBattle();

            session.TryPlayCard(0);

            Assert.AreEqual(4, session.Enemy.GetBuffStacks("poison"));
        }

        [Test]
        public void NoRelics_PlayPoison_EnemyGetsThreeStacks()
        {
            var session = CreateSession(deck: DeckOf(() => Poison(), 5));
            session.StartBattle();

            session.TryPlayCard(0);

            Assert.AreEqual(3, session.Enemy.GetBuffStacks("poison"));
        }

        [Test]
        public void SneckoSkullOnly_PlayStrengthCard_StillTwoStacks_HookIsById()
        {
            // 只配蛇颅骨、不带金刚杵：开战没有力量，打出后正好 2 层；带金刚杵会是 3，那 1 层是金刚杵的。
            var session = CreateSession(deck: DeckOf(() => Strength(), 5), relics: new[] { new SneckoSkullRelic() });
            session.StartBattle();

            session.TryPlayCard(0);

            Assert.AreEqual(2, session.Player.GetBuffStacks("strength"));
        }

        // ---------- 纸鹤：改公式常数（读目标的遗物） ----------

        [Test]
        public void PaperKrane_WeakEnemyAttack_PreviewIsThree_AndEqualsActualPlayerHpLoss()
        {
            // 敌人不会自己带虚弱：先走正常出牌给敌人上虚弱，再结束回合挨打。
            var session = CreateSession(deck: DeckOf(() => Weak(), 10), relics: new[] { new PaperKraneRelic() });
            session.StartBattle();
            session.TryPlayCard(0);

            var preview = session.PreviewEnemyAttack();
            session.TryEndPlayerTurn();

            // floor(6 × 0.6) = 3；无纸鹤是 floor(6 × 0.75) = 4。
            Assert.AreEqual(3, preview);
            Assert.AreEqual(40 - preview, session.Player.CurrentHp);
        }

        [Test]
        public void NoRelics_WeakEnemyAttack_PreviewIsFour_AndEqualsActualPlayerHpLoss()
        {
            var session = CreateSession(deck: DeckOf(() => Weak(), 10));
            session.StartBattle();
            session.TryPlayCard(0);

            var preview = session.PreviewEnemyAttack();
            session.TryEndPlayerTurn();

            Assert.AreEqual(4, preview);
            Assert.AreEqual(40 - preview, session.Player.CurrentHp);
        }

        [Test]
        public void PaperKrane_PlayerOwnWeak_OwnAttackStillUsesDefaultMultiplier()
        {
            // 纸鹤看的是目标：持有者自己带虚弱（本版本没有来源，测试直接构造）去打敌人，敌人没有纸鹤，仍是 0.75。
            var session = CreateSession(deck: BuildDeck(5), relics: new[] { new PaperKraneRelic() });
            session.StartBattle();
            session.Player.ApplyBuff(new WeakBuff(1));

            var preview = session.PreviewPlayerAttack(6);
            session.TryPlayCard(0);

            // floor(6 × 0.75) = 4，不是 floor(6 × 0.6) = 3。
            Assert.AreEqual(4, preview);
            Assert.AreEqual(36 - preview, session.Enemy.CurrentHp);
        }

        // ---------- 三件同配 ----------

        [Test]
        public void AllThreeRelics_StartBattle_ThreeRelicsOnPlayer_OnlyStrengthInBuffs()
        {
            var session = CreateSession(deck: BuildDeck(5),
                relics: new RelicState[] { new VajraRelic(), new SneckoSkullRelic(), new PaperKraneRelic() });

            session.StartBattle();

            Assert.AreEqual(3, session.Player.Relics.Count);
            // 金刚杵的力量经过了蛇颅骨的钩子但没被加量：钩子按 Id 判定，不是"所有施加 +1"。
            Assert.AreEqual(1, session.Player.GetBuffStacks("strength"));
            Assert.AreEqual(1, session.Player.Buffs.Count);
            foreach (var buff in session.Player.Buffs)
            {
                Assert.IsFalse(RelicFactory.IsKnown(buff.Id), $"遗物 Id '{buff.Id}' 不应出现在 Buff 栏");
            }

            Assert.AreEqual(0, session.Enemy.Relics.Count);
            Assert.AreEqual(0, session.Enemy.Buffs.Count);
        }
    }
}
