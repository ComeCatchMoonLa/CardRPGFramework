using CardRPGFramework.Core.Cards;

namespace CardRPGFramework.Core.Actions
{
    /// <summary>
    /// 直接调 CardPile.Draw，不经过 Effect 层：Phase 2a 把 Effect 钉死为"只改 CombatantState"，
    /// 抽牌改的是牌堆不是生物，加一个 DrawEffect 只会让 Core.Effects 反向依赖 Core.Cards，方法体还只是一行转发。
    /// Action 自己持有要改的对象，与 DamageAction 持有 source / target 是同一模式；ActionContext 不为此加 CardPile 字段。
    /// </summary>
    public sealed class DrawCardsAction : IAction
    {
        private readonly CardPile _pile;
        private readonly int _count;

        public DrawCardsAction(CardPile pile, int count)
        {
            _pile = pile;
            _count = count;
        }

        public void Execute(ActionContext context) => _pile.Draw(_count);
    }
}
