namespace CardRPGFramework.Core.Rules
{
    /// <summary>
    /// 规则是纯函数：只读 Source/Target 上的 Buff 数据，只写 Damage，不调用任何 Effect/Action。
    /// 加法/乘法只是 DamageCalculator 内部的执行阶段分类，不在接口层面区分。
    /// </summary>
    public interface IDamageRule
    {
        void Apply(DamageContext context);
    }
}
