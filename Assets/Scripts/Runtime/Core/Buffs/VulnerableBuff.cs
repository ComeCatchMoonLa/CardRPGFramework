namespace CardRPGFramework.Core.Buffs
{
    /// <summary>纯数值型 Buff，无触发；由 VulnerableDamageRule 通过 BuffIds.Vulnerable 读取。</summary>
    public sealed class VulnerableBuff : BuffState
    {
        public override string Id => BuffIds.Vulnerable;

        public VulnerableBuff(int stacks) : base(stacks)
        {
        }
    }
}
