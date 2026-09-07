namespace CardRPGFramework.Views
{
    /// <summary>
    /// Buff Id → 中文名，只在这里维护一份。放 Views 而不是 Core：显示名是表现层的事，Core 里的 Buff 只认 Id。
    /// </summary>
    public static class BuffDisplayNames
    {
        // 未知 Id 原样返回而不抛异常：Data 层启动校验已经拦过未知 buffId，这里再炸只会让一张卡面拖垮整个界面。
        public static string Of(string buffId) => buffId switch
        {
            "strength" => "力量",
            "poison" => "中毒",
            "weak" => "虚弱",
            "vulnerable" => "易伤",
            _ => buffId,
        };
    }
}
