using CardRPGFramework.Core.Combatants;
using CardRPGFramework.Core.Effects;
using CardRPGFramework.Core.Rules;

namespace CardRPGFramework.Core.Actions
{
    /// <summary>
    /// 攻击类伤害的唯一入口：先经 DamageCalculator 按双方 Buff 修正，再交给 DamageEffect 走格挡。
    /// 玩家出牌和敌人固定攻击都必须走这里，否则规则只对一方生效。
    /// </summary>
    public sealed class DamageAction : IAction
    {
        private readonly CombatantState _source;
        private readonly CombatantState _target;
        private readonly int _baseAmount;

        public DamageAction(CombatantState source, CombatantState target, int baseAmount)
        {
            _source = source;
            _target = target;
            _baseAmount = baseAmount;
        }

        public void Execute(ActionContext context)
        {
            var damageContext = new DamageContext(_baseAmount, _source, _target);
            var finalDamage = DamageCalculator.CalculateFinalDamage(damageContext);
            DamageEffect.Apply(_target, finalDamage);
        }
    }
}
