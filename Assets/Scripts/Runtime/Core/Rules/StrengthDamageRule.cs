using CardRPGFramework.Core.Buffs;

namespace CardRPGFramework.Core.Rules
{
    /// <summary>加法规则：伤害 + 攻击发起方身上的力量层数。</summary>
    public sealed class StrengthDamageRule : IDamageRule
    {
        public void Apply(DamageContext context) => context.Damage += context.Source.GetBuffStacks(BuffIds.Strength);
    }
}
