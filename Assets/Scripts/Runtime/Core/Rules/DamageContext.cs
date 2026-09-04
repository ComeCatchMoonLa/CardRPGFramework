using CardRPGFramework.Core.Combatants;

namespace CardRPGFramework.Core.Rules
{
    /// <summary>
    /// Damage 全程用 double 运算，只在 DamageCalculator 输出时取整一次：
    /// 乘法规则叠加时如果中间取整，结果会和"一次性算完再取整"不一致（如 5 × 0.75 × 1.5）。
    /// </summary>
    public sealed class DamageContext
    {
        public double Damage { get; set; }
        public CombatantState Source { get; }
        public CombatantState Target { get; }

        public DamageContext(int baseDamage, CombatantState source, CombatantState target)
        {
            Damage = baseDamage;
            Source = source;
            Target = target;
        }
    }
}
