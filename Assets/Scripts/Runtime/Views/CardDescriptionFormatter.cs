using System;
using System.Text;
using CardRPGFramework.Core.Cards;

namespace CardRPGFramework.Views
{
    /// <summary>
    /// 效果列表 → 每条一行的卡面描述，只用 EffectSpec 的基础数值，不算力量 / 易伤修正（公式后数值是 0.3 的内容）。
    /// 只收 CardDefinition、不收 BattleController：1.0 奖励页在没有战斗时也要生成卡面。
    /// 描述由代码生成而不做成配置字段，避免"效果列表"和"描述文案"两套真相。
    /// </summary>
    public static class CardDescriptionFormatter
    {
        public static string Format(CardDefinition card)
        {
            var builder = new StringBuilder();
            foreach (var effect in card.Effects)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(Describe(effect));
            }

            return builder.ToString();
        }

        private static string Describe(EffectSpec effect) => effect.Kind switch
        {
            EffectKind.Damage => $"造成 {effect.Value} 点伤害",
            EffectKind.Block => $"获得 {effect.Value} 点格挡",
            EffectKind.Heal => $"恢复 {effect.Value} 点生命",
            EffectKind.ApplyBuff => effect.Target == EffectTarget.Self
                ? $"获得 {effect.Value} 层{BuffDisplayNames.Of(effect.BuffId)}"
                : $"施加 {effect.Value} 层{BuffDisplayNames.Of(effect.BuffId)}",
            EffectKind.Draw => $"抽 {effect.Value} 张牌",
            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect.Kind, "未知的 EffectKind"),
        };
    }
}
