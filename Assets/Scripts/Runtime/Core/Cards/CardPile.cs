using System;
using System.Collections.Generic;

namespace CardRPGFramework.Core.Cards
{
    /// <summary>
    /// 只处理牌的容器规则：抽牌堆、手牌、弃牌堆之间的流转，不关心卡牌效果。
    /// </summary>
    public sealed class CardPile
    {
        private readonly Random _random;
        private readonly List<CardDefinition> _drawPile = new();
        private readonly List<CardDefinition> _hand = new();
        private readonly List<CardDefinition> _discardPile = new();

        public IReadOnlyList<CardDefinition> Hand => _hand;
        public int DrawPileCount => _drawPile.Count;
        public int DiscardPileCount => _discardPile.Count;

        public CardPile(IEnumerable<CardDefinition> initialDeck, Random random)
        {
            if (initialDeck == null)
            {
                throw new ArgumentNullException(nameof(initialDeck));
            }

            _random = random ?? throw new ArgumentNullException(nameof(random));

            _drawPile.AddRange(initialDeck);
            Shuffle(_drawPile);
        }

        /// <summary>
        /// 依次抽 count 张牌。抽牌堆为空时先把弃牌堆洗入抽牌堆；两堆同时为空时安全停止，不抛异常。
        /// </summary>
        public void Draw(int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (_drawPile.Count == 0)
                {
                    ReshuffleDiscardIntoDrawPile();
                }

                if (_drawPile.Count == 0)
                {
                    return;
                }

                var topIndex = _drawPile.Count - 1;
                var card = _drawPile[topIndex];
                _drawPile.RemoveAt(topIndex);
                _hand.Add(card);
            }
        }

        /// <summary>把手牌中指定位置的卡牌移入弃牌堆，返回被打出的卡牌。</summary>
        public CardDefinition PlayCard(int handIndex)
        {
            var card = _hand[handIndex];
            _hand.RemoveAt(handIndex);
            _discardPile.Add(card);
            return card;
        }

        /// <summary>回合结束时把剩余手牌全部移入弃牌堆。</summary>
        public void DiscardHand()
        {
            _discardPile.AddRange(_hand);
            _hand.Clear();
        }

        private void ReshuffleDiscardIntoDrawPile()
        {
            if (_discardPile.Count == 0)
            {
                return;
            }

            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);
        }

        private void Shuffle(List<CardDefinition> cards)
        {
            for (var i = cards.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }
    }
}
