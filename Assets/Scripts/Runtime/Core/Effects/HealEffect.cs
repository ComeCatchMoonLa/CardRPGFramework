using CardRPGFramework.Core.Combatants;

namespace CardRPGFramework.Core.Effects
{
    public static class HealEffect
    {
        public static void Apply(CombatantState target, int amount) => target.Heal(amount);
    }
}
