using System;

namespace CardRPGFramework.Core.Buffs
{
    /// <summary>
    /// Buff Id → BuffState 的唯一转换点，供 EffectSpec.ApplyBuff（只带字符串 Id）落地成实例。
    /// 用 switch 而不是字典注册：Buff 种类是代码里的封闭集合，新 Buff 必然要写子类，顺手加一行分支成本为零；
    /// 注册表只会让"漏注册"从编译期错误变成运行期错误。IsKnown 与 Create 两处 Id 列表必须同步改。
    /// </summary>
    public static class BuffFactory
    {
        public static bool IsKnown(string id) => id is BuffIds.Strength or BuffIds.Poison or BuffIds.Weak or BuffIds.Vulnerable;

        /// <summary>justApplied 是刚施加保护，只对回合型 Buff（虚弱 / 易伤）有意义；力量 / 中毒没有轮末减层，忽略它。</summary>
        public static BuffState Create(string id, int stacks, bool justApplied = false) => id switch
        {
            BuffIds.Strength => new StrengthBuff(stacks),
            BuffIds.Poison => new PoisonBuff(stacks),
            BuffIds.Weak => new WeakBuff(stacks, justApplied),
            BuffIds.Vulnerable => new VulnerableBuff(stacks, justApplied),
            _ => throw new ArgumentException($"未知的 Buff Id: {id}", nameof(id)),
        };
    }
}
