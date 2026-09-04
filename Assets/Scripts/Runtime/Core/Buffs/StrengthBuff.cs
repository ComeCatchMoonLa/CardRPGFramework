namespace CardRPGFramework.Core.Buffs
{
    /// <summary>纯数值型 Buff，本阶段没有任何数值消费它（消费逻辑在 Phase 2c 的 RuleSystem）。</summary>
    public sealed class StrengthBuff : BuffState
    {
        public override string Id => "strength";

        public StrengthBuff(int stacks) : base(stacks)
        {
        }
    }
}
