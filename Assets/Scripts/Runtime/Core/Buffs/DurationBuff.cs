namespace CardRPGFramework.Core.Buffs
{
    /// <summary>
    /// 虚弱 / 易伤的共同中间基类：层数 = 剩余轮数，每轮结束减 1；有无决定倍率，层数不影响强度。
    /// 刚施加保护（一代 justApplied）是构造时的事实，只跳过第一次轮末减层。叠加走 CombatantState.ApplyBuff 的 AddStacks，
    /// 新实例被丢弃，所以保护不会随叠加刷新——每 1 层严格对应施加方的 1 段行动。
    /// BuffState 基类不加字段：力量 / 中毒没有"剩余轮数"这个概念。
    /// </summary>
    public abstract class DurationBuff : BuffState, IRoundEndTrigger
    {
        private bool _justApplied;

        protected DurationBuff(int stacks, bool justApplied) : base(stacks) => _justApplied = justApplied;

        public void OnRoundEnd()
        {
            if (_justApplied)
            {
                _justApplied = false;
                return;
            }

            RemoveStacks(1);
        }
    }
}
