using CardRPGFramework.Core.Combatants;
using CardRPGFramework.Core.Effects;

namespace CardRPGFramework.Core.Actions
{
    public sealed class BlockAction : IAction
    {
        private readonly CombatantState _target;
        private readonly int _amount;

        public BlockAction(CombatantState target, int amount)
        {
            _target = target;
            _amount = amount;
        }

        public void Execute(ActionContext context) => BlockEffect.Apply(_target, _amount);
    }
}
