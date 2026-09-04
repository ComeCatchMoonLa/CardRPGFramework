using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Actions;
using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Combatants;

namespace CardRPGFramework.Core.Battle
{
    /// <summary>
    /// 一场战斗的唯一规则入口：阶段、回合、能量、双方状态和牌堆流转都只能通过这里修改。
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

        public BattleSession(BattleSetup setup, IEnumerable<CardDefinition> deck, Random random)
        {
            Player = new CombatantState(setup.PlayerMaxHp);
            Enemy = new CombatantState(setup.EnemyMaxHp);
            EnemyDamage = setup.EnemyDamage;
            EnergyPerTurn = setup.EnergyPerTurn;
            HandSize = setup.HandSize;
            _cardPile = new CardPile(deck, random);
        }

        /// <summary>开始战斗并进入第一个玩家回合。战斗已开始时不重复初始化。</summary>
        public void StartBattle()
        {
            if (Phase != BattlePhase.NotStarted)
            {
                return;
            }

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
            EnqueueCardAction(card);
            _actionQueue.RunAll(new ActionContext(_actionQueue));
            _cardPile.PlayCard(handIndex);

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

        // 结算顺序在 PlayCard（移入弃牌堆）之前：Phase 1 三种效果都不改牌堆/手牌，暂时安全。
        // 以后出现"打出时触发抽牌"等会改变手牌的效果时，需要重新核对 handIndex 的时机语义。
        // switch(CardType) 依然存在，但现在决定的是"该入队哪个 Action"，不是"直接怎么改状态"。
        private void EnqueueCardAction(CardDefinition card)
        {
            switch (card.Type)
            {
                case CardType.Attack:
                    _actionQueue.Enqueue(new DamageAction(Player, Enemy, card.Value));
                    break;
                case CardType.Defend:
                    _actionQueue.Enqueue(new BlockAction(Player, card.Value));
                    break;
                case CardType.Heal:
                    _actionQueue.Enqueue(new HealAction(Player, card.Value));
                    break;
                case CardType.Strength:
                    _actionQueue.Enqueue(new ApplyBuffAction(Player, Player, new StrengthBuff(card.Value)));
                    break;
                case CardType.Poison:
                    _actionQueue.Enqueue(new ApplyBuffAction(Player, Enemy, new PoisonBuff(card.Value)));
                    break;
                case CardType.Weak:
                    _actionQueue.Enqueue(new ApplyBuffAction(Player, Enemy, new WeakBuff(card.Value)));
                    break;
                case CardType.Vulnerable:
                    _actionQueue.Enqueue(new ApplyBuffAction(Player, Enemy, new VulnerableBuff(card.Value)));
                    break;
            }
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
