using CardRPGFramework.Core.Actions;
using CardRPGFramework.Core.Combatants;

namespace CardRPGFramework.Core.Buffs
{
    /// <summary>
    /// 只有需要"回合开始时做点什么"的 Buff 才实现这个接口；纯数值型 Buff（力量）不实现它，
    /// 触发逻辑完全下沉到具体子类，不存在外部代码"拿着 Id 去找对应 Buff 实例"的情况。
    /// </summary>
    public interface IBuffTrigger
    {
        void OnTurnStart(CombatantState owner, ActionQueue queue);
    }
}
