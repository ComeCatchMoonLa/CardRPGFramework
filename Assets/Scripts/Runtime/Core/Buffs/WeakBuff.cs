namespace CardRPGFramework.Core.Buffs
{
    /// <summary>回合型 Buff：由 WeakDamageRule 通过 BuffIds.Weak 读"有无"，层数只决定还能维持几轮（见 DurationBuff）。</summary>
    public sealed class WeakBuff : DurationBuff
    {
        public override string Id => BuffIds.Weak;

        public WeakBuff(int stacks, bool justApplied = false) : base(stacks, justApplied)
        {
        }
    }
}
