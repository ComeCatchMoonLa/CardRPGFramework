namespace CardRPGFramework.Core.Buffs
{
    /// <summary>纯数值型 Buff，无触发。Id 必须与 VulnerableDamageRule 读取的字符串一致，改一边就要同步改另一边。</summary>
    public sealed class VulnerableBuff : BuffState
    {
        public override string Id => "vulnerable";

        public VulnerableBuff(int stacks) : base(stacks)
        {
        }
    }
}
