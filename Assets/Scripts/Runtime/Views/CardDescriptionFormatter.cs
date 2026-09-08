using System;
using System.Text;
using CardRPGFramework.Core.Cards;

namespace CardRPGFramework.Views
{
    /// <summary>
    /// 效果列表 → 每条一行的卡面描述。无委托版本只用 EffectSpec 的基础数值（1.0 奖励页没有战斗，用它）；
    /// 带委托版本把 Damage 行的数字交给 previewAttackDamage 换成公式后数值，其余行不变。
    /// 生成器本身不算力量 / 易伤、不依赖 BattleController，只认 CardDefinition 与一个 int → int：
    /// 公式只在 Core 的 DamageCalculator 一处，View 不复算。
    /// 描述由代码生成而不做成配置字段，避免"效果列表"和"描述文案"两套真相。
    /// </summary>
    public static class CardDescriptionFormatter
    {
        private static readonly Func<int, int> Identity = value => value;

        public static string Format(CardDefinition card) => Format(card, Identity);

        public static string Format(CardDefinition card, Func<int, int> previewAttackDamage)
        {
            // 委托约定非空：没有战斗就用无委托重载，不要传 null 来表示"不预览"。
            if (previewAttackDamage == null)
            {
                throw new ArgumentNullException(nameof(previewAttackDamage));
            }

            var builder = new StringBuilder();
            foreach (var effect in card.Effects)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(Describe(effect, previewAttackDamage));
            }

            return builder.ToString();
        }

        private static string Describe(EffectSpec effect, Func<int, int> previewAttackDamage) => effect.Kind switch
        {
            EffectKind.Damage => $"造成 {previewAttackDamage(effect.Value)} 点伤害",
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
