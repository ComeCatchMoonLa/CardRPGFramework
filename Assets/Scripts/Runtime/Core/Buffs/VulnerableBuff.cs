namespace CardRPGFramework.Core.Buffs
{
    /// <summary>回合型 Buff：由 VulnerableDamageRule 通过 BuffIds.Vulnerable 读"有无"，层数只决定还能维持几轮（见 DurationBuff）。</summary>
    public sealed class VulnerableBuff : DurationBuff
    {
        public override string Id => BuffIds.Vulnerable;

        public VulnerableBuff(int stacks, bool justApplied = false) : base(stacks, justApplied)
        {
        }
    }
}
