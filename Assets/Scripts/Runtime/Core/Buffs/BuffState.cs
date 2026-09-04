using System;

namespace CardRPGFramework.Core.Buffs
{
    /// <summary>
    /// 保存核心状态：谁的、多少层。生命周期跟随所在的 CombatantState 存在，
    /// 战斗结束即销毁，本阶段不做跨战斗持久化。
    /// </summary>
    public abstract class BuffState
    {
        public abstract string Id { get; }
        public int Stacks { get; private set; }

        protected BuffState(int initialStacks) => Stacks = initialStacks;

        public void AddStacks(int amount) => Stacks += amount;
        public void RemoveStacks(int amount) => Stacks = Math.Max(0, Stacks - amount);
    }
}
