namespace CardRPGFramework.Core.Buffs
{
    /// <summary>
    /// 轮结束触发：只有回合型 Buff（层数 = 剩余轮数）实现。与 IBuffTrigger 同形（固定时机遍历 + 类型判断），
    /// 但没有 owner / queue 参数——减层只是 Buff 自己的层数变化，没有对外效果，不需要入队 Action。
    /// </summary>
    public interface IRoundEndTrigger
    {
        void OnRoundEnd();
    }
}
