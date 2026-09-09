using CardRPGFramework.Core.Actions;
using CardRPGFramework.Core.Combatants;

namespace CardRPGFramework.Core.Relics
{
    /// <summary>
    /// 与 IBuffTrigger.OnTurnStart 同形：开战时只入队 Action，不直接改状态，
    /// 这样开战注入的 Buff 和打牌施加的 Buff 走同一条路径（也会经过同样的施加钩子）。
    /// 签名里没有对手：本版本的消费者都只碰持有者自己，"开战给敌人易伤"（弹珠袋）出现时再加参数。
    /// </summary>
    public interface IBattleStartRelic
    {
        void OnBattleStart(CombatantState owner, ActionQueue queue);
    }
}
