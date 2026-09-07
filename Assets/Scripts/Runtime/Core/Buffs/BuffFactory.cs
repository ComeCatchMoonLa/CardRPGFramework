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
        public static bool IsKnown(string id) => id is "strength" or "poison" or "weak" or "vulnerable";

        public static BuffState Create(string id, int stacks) => id switch
        {
            "strength" => new StrengthBuff(stacks),
            "poison" => new PoisonBuff(stacks),
            "weak" => new WeakBuff(stacks),
            "vulnerable" => new VulnerableBuff(stacks),
            _ => throw new ArgumentException($"未知的 Buff Id: {id}", nameof(id)),
        };
    }
}
