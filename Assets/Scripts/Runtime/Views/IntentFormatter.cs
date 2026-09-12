using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Enemies;

namespace CardRPGFramework.Views
{
    /// <summary>
    /// 效果列表 → 意图文案。攻击段合并成一项"攻击 N"（N 是 Core 预览好的总和，这里不算公式），
    /// 其余按效果首次出现的顺序去重：Block → 防御，ApplyBuff(Self) / Heal → 增益，ApplyBuff(Opponent) → 减益；
    /// 用 " · " 连接；空行动 → 待机。与 CardDescriptionFormatter 同理：只认 Core 值类型与一个数字，不依赖 Controller。
    /// 意图不在 Core 里做成枚举：Core 没有任何消费者读"意图种类"，多一份"效果 → 意图"的真相只服务显示。
    /// </summary>
    public static class IntentFormatter
    {
        public static string Format(EnemyAction action, int previewedAttackDamage)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (action.Effects.Count == 0)
            {
                return "待机";
            }

            // 两段攻击的文案相同（预览数字是整条行动的总和），靠去重自然合成一项，不需要单独数攻击段。
            var parts = new List<string>(action.Effects.Count);
            foreach (var effect in action.Effects)
            {
                var part = Label(effect, previewedAttackDamage);
                if (!parts.Contains(part))
                {
                    parts.Add(part);
                }
            }

            return string.Join(" · ", parts);
        }

        private static string Label(EffectSpec effect, int previewedAttackDamage) => effect.Kind switch
        {
            EffectKind.Damage => $"攻击 {previewedAttackDamage}",
            EffectKind.Block => "防御",
            EffectKind.Heal => "增益",
            EffectKind.ApplyBuff => effect.Target == EffectTarget.Self ? "增益" : "减益",
            // Draw 在 EnemyAction 构造期已被拒绝，这里只兜未来新增原语忘了配文案的情况。
            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect.Kind, "敌人行动不该出现的 EffectKind"),
        };
    }
}
