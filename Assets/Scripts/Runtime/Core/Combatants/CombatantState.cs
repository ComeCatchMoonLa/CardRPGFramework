using System;

namespace CardRPGFramework.Core.Combatants
{
    /// <summary>
    /// 参战单位（玩家或敌人）的生命值与格挡状态，负责伤害、治疗与格挡的边界规则。
    /// </summary>
    public sealed class CombatantState
    {
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public int Block { get; private set; }
        public bool IsDead => CurrentHp <= 0;

        public CombatantState(int maxHp)
        {
            if (maxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp), "MaxHp 必须大于 0");
            }

            MaxHp = maxHp;
            CurrentHp = maxHp;
        }

        /// <summary>受到伤害时先扣格挡，剩余部分再扣生命值；生命值不低于 0。</summary>
        public void TakeDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var absorbed = Math.Min(Block, amount);
            Block -= absorbed;

            var remaining = amount - absorbed;
            CurrentHp = Math.Max(0, CurrentHp - remaining);
        }

        /// <summary>治疗不超过最大生命值。</summary>
        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
        }

        public void GainBlock(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Block += amount;
        }

        public void ClearBlock()
        {
            Block = 0;
        }
    }
}
