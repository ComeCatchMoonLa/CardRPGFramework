using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Relics;

namespace CardRPGFramework.Core.Rules
{
    /// <summary>
    /// 乘法规则：攻击发起方身上有虚弱时伤害 × 0.75；受击方持有纸鹤时改为 × 0.6。按"有无"而不按层数——
    /// 虚弱层数在原版是持续回合数不是强度，以后引入轮末减层时也不要改成按层数放大倍率。
    /// 纸鹤改的是这条公式的常数，不是再乘一层，也不给谁加虚弱层数；读的是目标（被打的一方）的遗物。
    /// </summary>
    public sealed class WeakDamageRule : IDamageRule
    {
        private const double DefaultMultiplier = 0.75;
        private const double PaperKraneMultiplier = 0.6;

        public void Apply(DamageContext context)
        {
            if (!context.Source.HasBuff(BuffIds.Weak))
            {
                return;
            }

            var multiplier = context.Target.HasRelic(RelicIds.PaperKrane) ? PaperKraneMultiplier : DefaultMultiplier;
            context.Damage *= multiplier;
        }
    }
}
