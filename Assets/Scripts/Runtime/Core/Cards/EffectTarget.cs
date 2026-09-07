namespace CardRPGFramework.Core.Cards
{
    /// <summary>
    /// 取名 Self / Opponent 而不是 Player / Enemy：0.6 敌人行动表会复用同一套 EffectSpec，
    /// 那时 Self 是敌人自己、Opponent 是玩家。现在就按"相对于施放者"取名，以后不用改枚举。
    /// </summary>
    public enum EffectTarget
    {
        Self,
        Opponent
    }
}
