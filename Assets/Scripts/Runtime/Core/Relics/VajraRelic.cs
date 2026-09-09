using CardRPGFramework.Core.Actions;
using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Combatants;

namespace CardRPGFramework.Core.Relics
{
    /// <summary>金刚杵：战斗开始时获得 1 层力量。开战注入型——遗物本身不进 Buff 栏，留在战场上的是一条普通的力量。</summary>
    public sealed class VajraRelic : RelicState, IBattleStartRelic
    {
        public override string Id => RelicIds.Vajra;

        // 走工厂、走 Action：和打出力量强化是同一条路径（也会经过同样的施加钩子），不 new StrengthBuff、不 owner.ApplyBuff。
        public void OnBattleStart(CombatantState owner, ActionQueue queue) =>
            queue.Enqueue(new ApplyBuffAction(owner, owner, BuffFactory.Create(BuffIds.Strength, 1)));
    }
}
