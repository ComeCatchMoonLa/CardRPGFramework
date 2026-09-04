namespace CardRPGFramework.Core.Rules
{
    /// <summary>
    /// 乘法规则：攻击发起方身上有虚弱时伤害 × 0.75。按"有无"而不按层数——
    /// 虚弱层数在原版是持续回合数不是强度，以后引入轮末减层时也不要改成按层数放大倍率。
    /// </summary>
    public sealed class WeakDamageRule : IDamageRule
    {
        public void Apply(DamageContext context)
        {
            if (context.Source.HasBuff("weak"))
            {
                context.Damage *= 0.75;
            }
        }
    }
}
