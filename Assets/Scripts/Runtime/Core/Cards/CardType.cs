namespace CardRPGFramework.Core.Cards
{
    /// <summary>
    /// 只回答"这张牌是什么"（原版含义），"打出后发生什么"由 CardDefinition.Effects 回答，
    /// "打出后去哪"由 CardDefinition.ExhaustsWhenPlayed 回答（能力牌默认进消耗堆）。
    /// 0.1 曾用它同时表示效果种类，已收掉。Power 追加在末尾：0.2 资产序列化的是整数，插在中间会让旧资产全部错位。
    /// </summary>
    public enum CardType
    {
        Attack,
        Skill,
        Power
    }
}
