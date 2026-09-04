using CardRPGFramework.Core.Actions;
using CardRPGFramework.Core.Combatants;

namespace CardRPGFramework.Core.Buffs
{
    public sealed class PoisonBuff : BuffState, IBuffTrigger
    {
        public override string Id => "poison";

        public PoisonBuff(int stacks) : base(stacks)
        {
        }

        /// <summary>造成等于当前层数的 HP Loss 伤害（不受格挡影响），然后层数减 1；层数为 0 时不再触发。</summary>
        public void OnTurnStart(CombatantState owner, ActionQueue queue)
        {
            if (Stacks <= 0)
            {
                return;
            }

            queue.Enqueue(new HpLossAction(owner, Stacks));
            RemoveStacks(1);
        }
    }
}
