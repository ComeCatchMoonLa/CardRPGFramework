using System;
using System.Collections.Generic;
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
            ResolveCard(card);
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

            Player.TakeDamage(EnemyDamage);

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
        private void ResolveCard(CardDefinition card)
        {
            switch (card.Type)
            {
                case CardType.Attack:
                    Enemy.TakeDamage(card.Value);
                    break;
                case CardType.Defend:
                    Player.GainBlock(card.Value);
                    break;
                case CardType.Heal:
                    Player.Heal(card.Value);
                    break;
            }
        }

        private void StartPlayerTurn()
        {
            TurnNumber++;
            Phase = BattlePhase.PlayerTurn;
            Player.ClearBlock();
            Energy = EnergyPerTurn;

            var cardsToDraw = HandSize - _cardPile.Hand.Count;
            if (cardsToDraw > 0)
            {
                _cardPile.Draw(cardsToDraw);
            }
        }
    }
}
