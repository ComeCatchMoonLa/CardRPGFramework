using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Combatants;

namespace CardRPGFramework.Core.Relics
{
    /// <summary>蛇颅骨：持有者施加中毒时，这一次的层数 +1。改的是"这一次施加多少"，不是第二次施加，也不是规则常数。</summary>
    public sealed class SneckoSkullRelic : RelicState, IApplyBuffModifier
    {
        public override string Id => RelicIds.SneckoSkull;

        // 按 Id 判定而不是"所有施加 +1"：金刚杵开战给的力量也会经过这里，但什么都不发生。
        public void ModifyOutgoingBuff(CombatantState source, CombatantState target, BuffState buff)
        {
            if (buff.Id == BuffIds.Poison)
            {
                buff.AddStacks(1);
            }
        }
    }
}
