using System;
using System.Collections.Generic;

namespace CardRPGFramework.Core.Rules
{
    /// <summary>
    /// 固定两段管线：加法规则先跑，乘法规则后跑，最后向下取整一次。
    /// 规则集合写死在这里而不做注册引擎：当前只有 3 条规则，注册机制没有第二个调用方。
    /// </summary>
    public static class DamageCalculator
    {
        private static readonly IReadOnlyList<IDamageRule> AdditiveRules = new IDamageRule[]
        {
            new StrengthDamageRule(),
        };

        private static readonly IReadOnlyList<IDamageRule> MultiplicativeRules = new IDamageRule[]
        {
            new WeakDamageRule(),
            new VulnerableDamageRule(),
        };

        public static int CalculateFinalDamage(DamageContext context)
        {
            foreach (var rule in AdditiveRules)
            {
                rule.Apply(context);
            }

            foreach (var rule in MultiplicativeRules)
            {
                rule.Apply(context);
            }

            return (int)Math.Floor(context.Damage);
        }
    }
}
