using CardRPGFramework.Core.Combatants;

namespace CardRPGFramework.Core.Effects
{
    public static class BlockEffect
    {
        public static void Apply(CombatantState target, int amount) => target.GainBlock(amount);
    }
}
