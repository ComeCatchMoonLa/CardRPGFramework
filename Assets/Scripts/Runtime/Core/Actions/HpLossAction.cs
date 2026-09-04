using CardRPGFramework.Core.Combatants;
using CardRPGFramework.Core.Effects;

namespace CardRPGFramework.Core.Actions
{
    /// <summary>
    /// 独立于 DamageAction 存在，不是同一个类加一个"是否走格挡"的布尔参数：
    /// 目前只有两种伤害路径（Attack 走格挡、HP Loss 不走），加类比加参数更符合
    /// "调用方一看类型就知道语义"的可读性优先原则。
    /// </summary>
    public sealed class HpLossAction : IAction
    {
        private readonly CombatantState _target;
        private readonly int _amount;

        public HpLossAction(CombatantState target, int amount)
        {
            _target = target;
            _amount = amount;
        }

        public void Execute(ActionContext context) => HpLossEffect.Apply(_target, _amount);
    }
}
