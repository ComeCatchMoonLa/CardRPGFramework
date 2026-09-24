using System;
using CardRPGFramework.Core.Cards;

namespace CardRPGFramework.Views
{
    /// <summary>
    /// 卡面类型中文。未知值抛出：CardType 是封闭枚举，漏写一种会让手牌和奖励页同时显示错字。
    /// </summary>
    public static class CardTypeDisplayNames
    {
        public static string Of(CardType type) => type switch
        {
            CardType.Attack => "攻击",
            CardType.Skill => "技能",
            CardType.Power => "能力",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "未知的 CardType"),
        };
    }
}
