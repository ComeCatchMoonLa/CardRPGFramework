using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Combatants;
using CardRPGFramework.Core.Effects;
using CardRPGFramework.Core.Relics;

namespace CardRPGFramework.Core.Actions
{
    public sealed class ApplyBuffAction : IAction
    {
        private readonly CombatantState _source;
        private readonly CombatantState _target;
        private readonly BuffState _buff;

        // source 的第一个消费者是 0.5 的蛇颅骨（施加方遗物的 IApplyBuffModifier）。原版"响应施加"的其它机制
        // （人工制品取消、冠军腰带追加）同样需要"谁在施加"，但要的是能取消 / 追加的钩子，等消费者出现再升级，
        // 见 Docs/Phase 2 扩展边界批注.md 第 1、3 节。
        public ApplyBuffAction(CombatantState source, CombatantState target, BuffState buff)
        {
            _source = source;
            _target = target;
            _buff = buff;
        }

        public void Execute(ActionContext context)
        {
            // 遍历的是施加方的遗物：蛇颅骨是"你施加中毒时"，不是"有人被施加中毒时"。敌人没有遗物，自然不受影响。
            foreach (var relic in _source.Relics)
            {
                if (relic is IApplyBuffModifier modifier)
                {
                    modifier.ModifyOutgoingBuff(_source, _target, _buff);
                }
            }

            BuffEffect.Apply(_target, _buff);
        }
    }
}
