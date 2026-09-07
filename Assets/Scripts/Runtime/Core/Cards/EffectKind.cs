namespace CardRPGFramework.Core.Cards
{
    /// <summary>
    /// 封闭枚举，不是插件点：五种原语全部写在 Core 里，消费方用 switch 穷举，缺分支编译期就能发现。
    /// 不做"注册新效果种类"的接口；加一张卡不该碰这里，加一种原语才需要。
    /// </summary>
    public enum EffectKind
    {
        Damage,
        Block,
        Heal,
        ApplyBuff,
        Draw
    }
}
