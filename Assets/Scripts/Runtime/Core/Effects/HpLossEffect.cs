using CardRPGFramework.Core.Combatants;

namespace CardRPGFramework.Core.Effects
{
    public static class HpLossEffect
    {
        public static void Apply(CombatantState target, int amount) => target.LoseHp(amount);
    }
}
