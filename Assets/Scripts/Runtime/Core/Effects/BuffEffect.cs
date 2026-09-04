using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Combatants;

namespace CardRPGFramework.Core.Effects
{
    public static class BuffEffect
    {
        public static void Apply(CombatantState target, BuffState buff) => target.ApplyBuff(buff);
    }
}
