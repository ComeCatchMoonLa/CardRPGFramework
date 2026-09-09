using CardRPGFramework.Core.Buffs;

namespace CardRPGFramework.Core.Rules
{
    /// <summary>乘法规则：受击方身上有易伤时伤害 × 1.5，同样按"有无"不按层数。</summary>
    public sealed class VulnerableDamageRule : IDamageRule
    {
        public void Apply(DamageContext context)
        {
            if (context.Target.HasBuff(BuffIds.Vulnerable))
            {
                context.Damage *= 1.5;
            }
        }
    }
}
