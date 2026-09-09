using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Combatants;

namespace CardRPGFramework.Core.Relics
{
    /// <summary>
    /// 施加 Buff 执行之前、施加方遗物的一次机会：只允许改即将施加的这个 buff 实例（层数）。
    /// 传入的 buff 由 BuffFactory 刚建出来、还不在任何人的字典里，直接 AddStacks 改的就是"这一次施加多少"，是安全的。
    /// 不提供取消 / 追加：人工制品、冠军腰带都还没有消费者，届时再升级成带 Context 的前后两个钩子，
    /// 那时也顺带需要 Buff / Debuff 极性标记。
    /// </summary>
    public interface IApplyBuffModifier
    {
        void ModifyOutgoingBuff(CombatantState source, CombatantState target, BuffState buff);
    }
}
