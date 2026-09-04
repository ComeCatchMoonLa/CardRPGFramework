using CardRPGFramework.Core.Combatants;
using CardRPGFramework.Core.Effects;

namespace CardRPGFramework.Core.Actions
{
    public sealed class HealAction : IAction
    {
        private readonly CombatantState _target;
        private readonly int _amount;

        public HealAction(CombatantState target, int amount)
        {
            _target = target;
            _amount = amount;
        }

        public void Execute(ActionContext context) => HealEffect.Apply(_target, _amount);
    }
}
