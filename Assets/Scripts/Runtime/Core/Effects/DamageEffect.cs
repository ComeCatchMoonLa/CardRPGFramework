using CardRPGFramework.Core.Combatants;

namespace CardRPGFramework.Core.Effects
{
    public static class DamageEffect
    {
        public static void Apply(CombatantState target, int amount) => target.TakeDamage(amount);
    }
}
