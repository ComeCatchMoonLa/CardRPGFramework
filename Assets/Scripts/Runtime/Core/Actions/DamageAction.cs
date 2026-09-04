using CardRPGFramework.Core.Combatants;
using CardRPGFramework.Core.Effects;

namespace CardRPGFramework.Core.Actions
{
    public sealed class DamageAction : IAction
    {
        private readonly CombatantState _target;
        private readonly int _amount;

        public DamageAction(CombatantState target, int amount)
        {
            _target = target;
            _amount = amount;
        }

        public void Execute(ActionContext context) => DamageEffect.Apply(_target, _amount);
    }
}
