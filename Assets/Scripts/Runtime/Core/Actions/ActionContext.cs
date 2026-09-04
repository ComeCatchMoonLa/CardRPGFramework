namespace CardRPGFramework.Core.Actions
{
    /// <summary>
    /// 携带 Action 执行时可能需要的上下文；现在只放 Queue，
    /// 用来让 Action.Execute 内部可以继续入队新的 Action。
    /// </summary>
    public sealed class ActionContext
    {
        public ActionQueue Queue { get; }

        public ActionContext(ActionQueue queue)
        {
            Queue = queue;
        }
    }
}
