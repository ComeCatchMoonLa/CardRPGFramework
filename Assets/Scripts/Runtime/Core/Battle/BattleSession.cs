using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Actions;
using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Combatants;
using CardRPGFramework.Core.Relics;
using CardRPGFramework.Core.Rules;

namespace CardRPGFramework.Core.Battle
{
    /// <summary>
    /// 一场战斗的唯一规则入口：阶段、回合、能量、双方状态和牌堆流转都只能通过这里修改。
    /// 对遗物只认 RelicState（构造参数）与 IBattleStartRelic（开战遍历），不认任何具体遗物或遗物 Id：
    /// 新增一件用现有钩子的遗物不改这里，改公式常数的遗物改对应 Rule。
    /// </summary>
    public sealed class BattleSession
    {
        private readonly CardPile _cardPile;
        // 贯穿整场战斗的同一个队列，不是每次 TryPlayCard 新建：Phase 2b 回合开始/结束的 Buff
        // 触发也需要复用它入队 Action。
        private readonly ActionQueue _actionQueue = new();

        public BattlePhase Phase { get; private set; } = BattlePhase.NotStarted;
        public int TurnNumber { get; private set; }
        public int Energy { get; private set; }
        public int EnergyPerTurn { get; }
        public int EnemyDamage { get; }
        public int HandSize { get; }

        public CombatantState Player { get; }
        public CombatantState Enemy { get; }

        public IReadOnlyList<CardDefinition> Hand => _cardPile.Hand;
        public int DrawPileCount => _cardPile.DrawPileCount;
        public int DiscardPileCount => _cardPile.DiscardPileCount;
        public int ExhaustPileCount => _cardPile.ExhaustPileCount;

        // relics 可选：遗物只挂玩家，0.1～0.4 的调用点不用改。
        public BattleSession(BattleSetup setup, IEnumerable<CardDefinition> deck, Random random,
                             IEnumerable<RelicState> relics = null)
        {
            Player = new CombatantState(setup.PlayerMaxHp);
            Enemy = new CombatantState(setup.EnemyMaxHp);
            EnemyDamage = setup.EnemyDamage;
            EnergyPerTurn = setup.EnergyPerTurn;
            HandSize = setup.HandSize;
            _cardPile = new CardPile(deck, random);

            foreach (var relic in relics ?? Array.Empty<RelicState>())
            {
                Player.AddRelic(relic);
            }
        }

        /// <summary>开始战斗并进入第一个玩家回合。战斗已开始时不重复初始化。</summary>
        public void StartBattle()
        {
            if (Phase != BattlePhase.NotStarted)
            {
                return;
            }

            // 开战钩子在第一个玩家回合之前：金刚杵这类注入的 Buff 从第一回合起就可见，回合开始的 Buff 触发也能看到它。
            TriggerBattleStartRelics(Player);
            StartPlayerTurn();
        }

        /// <summary>使用手牌中指定位置的卡牌。校验失败或战斗未处于玩家回合时返回 false。</summary>
        public bool TryPlayCard(int handIndex)
        {
            if (Phase != BattlePhase.PlayerTurn)
            {
                return false;
            }

            if (handIndex < 0 || handIndex >= _cardPile.Hand.Count)
            {
                return false;
            }

            var card = _cardPile.Hand[handIndex];
            if (card.Cost > Energy)
            {
                return false;
            }

            Energy -= card.Cost;
            // 先离手、再结算、最后落堆（原版顺序）：结算中若抽牌触发重洗，这张牌不在弃牌堆里，不会被洗回去；
            // 抽到的牌落在手牌末尾，也不会让 handIndex 指错。
            var played = _cardPile.TakeFromHand(handIndex);
            foreach (var effect in played.Effects)
            {
                _actionQueue.Enqueue(ToAction(effect, Player));
            }

            _actionQueue.RunAll(new ActionContext(_actionQueue));
            // 去哪是牌自己的规则（类型默认 + 消耗关键词），这里只问结果，不按类型或卡 Id 分支。
            if (played.ExhaustsWhenPlayed)
            {
                _cardPile.AddToExhaust(played);
            }
            else
            {
                _cardPile.AddToDiscard(played);
            }

            if (Enemy.IsDead)
            {
                Phase = BattlePhase.Victory;
            }

            return true;
        }

        /// <summary>结束玩家回合：弃掉剩余手牌，结算敌人攻击，未失败则进入下一玩家回合。</summary>
        public bool TryEndPlayerTurn()
        {
            if (Phase != BattlePhase.PlayerTurn)
            {
                return false;
            }

            _cardPile.DiscardHand();
            // EnemyTurn 目前只是瞬间经过的阶段：敌人行动同步执行，没有独立 AI/动画等待。
            // 保留这个阶段值是为了让状态机语义完整，后续接入异步敌人行动时再拆分。
            Phase = BattlePhase.EnemyTurn;

            TriggerTurnStartBuffs(Enemy);
            if (Enemy.IsDead)
            {
                // 中毒把敌人打死时立即以胜利结束，不再执行敌人固定攻击。
                Phase = BattlePhase.Victory;
                return true;
            }

            // 敌人攻击和中毒结算是两次独立的 RunAll：先确认敌人没被毒死，再入队攻击，
            // 否则毒杀之后这一下仍会打出去。
            _actionQueue.Enqueue(new DamageAction(Enemy, Player, EnemyDamage));
            _actionQueue.RunAll(new ActionContext(_actionQueue));

            if (Player.IsDead)
            {
                Phase = BattlePhase.Defeat;
            }
            else
            {
                StartPlayerTurn();
            }

            return true;
        }

        /// <summary>
        /// 只读预览玩家攻击：返回格挡前的最终伤害，与 DamageAction.Execute 走同一个 DamageContext / DamageCalculator，不改任何状态。
        /// (baseDamage, source, target) 的配对必须与 ToAction 里 EffectKind.Damage 的 (effect.Value, Player, Enemy) 相同，否则预览会漂；
        /// 读的是此刻的 Buff，所以痛击的 8 点不会算进它自己即将施加的易伤，与"先伤后易伤"的结算一致。
        /// </summary>
        public int PreviewPlayerAttack(int baseDamage) =>
            DamageCalculator.CalculateFinalDamage(new DamageContext(baseDamage, Player, Enemy));

        /// <summary>只读预览敌人固定攻击（格挡前），配对与 TryEndPlayerTurn 里的 DamageAction(Enemy, Player, EnemyDamage) 相同。</summary>
        public int PreviewEnemyAttack() =>
            DamageCalculator.CalculateFinalDamage(new DamageContext(EnemyDamage, Enemy, Player));

        // 唯一的 EffectSpec → IAction 转换点。这个 switch 穷举的是五种效果原语，加一张卡不会再碰它；
        // 新增一种原语才需要加一行，这是有意的封闭点。留在 Session 而不抽成工厂，是因为它要的 Player / Enemy / _cardPile
        // 全是 Session 字段，0.6 敌人行动的第二个调用方仍是 Session（source 传 Enemy）。
        private IAction ToAction(EffectSpec effect, CombatantState source)
        {
            var target = effect.Target == EffectTarget.Self ? source : Opponent(source);
            return effect.Kind switch
            {
                EffectKind.Damage => new DamageAction(source, target, effect.Value),
                EffectKind.Block => new BlockAction(target, effect.Value),
                EffectKind.Heal => new HealAction(target, effect.Value),
                EffectKind.ApplyBuff => new ApplyBuffAction(source, target, BuffFactory.Create(effect.BuffId, effect.Value)),
                EffectKind.Draw => new DrawCardsAction(_cardPile, effect.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(effect), effect.Kind, "未知的 EffectKind"),
            };
        }

        private CombatantState Opponent(CombatantState who) => who == Player ? Enemy : Player;

        /// <summary>遍历 owner 的遗物里实现 IBattleStartRelic 的，触发其开战行为并立即结算产生的 Action。与 TriggerTurnStartBuffs 同形。</summary>
        private void TriggerBattleStartRelics(CombatantState owner)
        {
            foreach (var relic in owner.Relics)
            {
                if (relic is IBattleStartRelic starter)
                {
                    starter.OnBattleStart(owner, _actionQueue);
                }
            }

            _actionQueue.RunAll(new ActionContext(_actionQueue));
        }

        /// <summary>遍历 owner 身上实现 IBuffTrigger 的 Buff，触发其回合开始行为并立即结算产生的 Action。</summary>
        private void TriggerTurnStartBuffs(CombatantState owner)
        {
            foreach (var buff in owner.Buffs)
            {
                if (buff is IBuffTrigger trigger)
                {
                    trigger.OnTurnStart(owner, _actionQueue);
                }
            }

            _actionQueue.RunAll(new ActionContext(_actionQueue));
        }

        private void StartPlayerTurn()
        {
            TurnNumber++;
            Phase = BattlePhase.PlayerTurn;
            Player.ClearBlock();

            TriggerTurnStartBuffs(Player);
            if (Player.IsDead)
            {
                Phase = BattlePhase.Defeat;
                return;
            }

            Energy = EnergyPerTurn;

            var cardsToDraw = HandSize - _cardPile.Hand.Count;
            if (cardsToDraw > 0)
            {
                _cardPile.Draw(cardsToDraw);
            }
        }
    }
}
