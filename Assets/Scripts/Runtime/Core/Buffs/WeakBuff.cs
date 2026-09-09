namespace CardRPGFramework.Core.Buffs
{
    /// <summary>纯数值型 Buff，无触发；由 WeakDamageRule 通过 BuffIds.Weak 读取。</summary>
    public sealed class WeakBuff : BuffState
    {
        public override string Id => BuffIds.Weak;

        public WeakBuff(int stacks) : base(stacks)
        {
        }
    }
}
