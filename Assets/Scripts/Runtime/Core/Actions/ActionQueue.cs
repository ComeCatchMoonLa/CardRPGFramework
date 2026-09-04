using System.Collections.Generic;

namespace CardRPGFramework.Core.Actions
{
    /// <summary>
    /// FIFO 队列。RunAll 执行期间，Action.Execute 内部可以继续调用 context.Queue.Enqueue，
    /// 新入队的 Action 会在同一次 RunAll 内被继续处理，这是 Phase 2b 中毒触发追加伤害 Action 时需要的能力。
    /// 不支持插队/优先级，出现真正需要插队的机制（如荆棘反伤）时再加。
    /// </summary>
    public sealed class ActionQueue
    {
        private readonly Queue<IAction> _pending = new();

        public void Enqueue(IAction action) => _pending.Enqueue(action);

        public void RunAll(ActionContext context)
        {
            while (_pending.Count > 0)
            {
                _pending.Dequeue().Execute(context);
            }
        }
    }
}
