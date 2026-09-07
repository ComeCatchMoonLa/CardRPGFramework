namespace CardRPGFramework.Core.Cards
{
    /// <summary>
    /// 只回答"这张牌是什么"（原版含义），"打出后发生什么"由 CardDefinition.Effects 回答。
    /// 0.1 曾用它同时表示效果种类，已收掉。0.4 加 Power。
    /// </summary>
    public enum CardType
    {
        Attack,
        Skill
    }
}
