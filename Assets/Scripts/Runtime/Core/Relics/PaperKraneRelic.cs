namespace CardRPGFramework.Core.Relics
{
    /// <summary>
    /// 纸鹤：带虚弱的敌人打持有者时，虚弱倍率 0.6 代替 0.75。没有钩子——它是被 WeakDamageRule 读取的数据，
    /// 和虚弱 Buff 被规则读取是同一种关系；改的是公式常数，不给谁加层数、也不再乘一层。
    /// </summary>
    public sealed class PaperKraneRelic : RelicState
    {
        public override string Id => RelicIds.PaperKrane;
    }
}
