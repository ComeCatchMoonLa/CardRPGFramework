using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Combatants;
using CardRPGFramework.Core.Effects;

namespace CardRPGFramework.Core.Actions
{
    public sealed class ApplyBuffAction : IAction
    {
        private readonly CombatantState _source;
        private readonly CombatantState _target;
        private readonly BuffState _buff;

        // source 本阶段没有任何消费者，但仍要求传入：施加者在调用点免费可得，
        // 而原版所有"响应施加"的机制（人工制品取消、Snecko Skull 加量、冠军腰带追加）
        // 都需要"谁在施加"，事后补字段要迁移全部调用点。这里只补信息通道，
        // 不实现任何拦截/改写机制，见 Docs/Phase 2 扩展边界批注.md 第 1 节。
        public ApplyBuffAction(CombatantState source, CombatantState target, BuffState buff)
        {
            _source = source;
            _target = target;
            _buff = buff;
        }

        public void Execute(ActionContext context) => BuffEffect.Apply(_target, _buff);
    }
}
