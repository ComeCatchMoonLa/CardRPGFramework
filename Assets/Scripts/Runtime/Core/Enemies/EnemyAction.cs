using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Cards;

namespace CardRPGFramework.Core.Enemies
{
    /// <summary>
    /// 敌人的一条行动 = 效果列表，与卡牌共用 EffectSpec 原语：结算交给 BattleSession.ToAction，意图文案由 View 从效果推导，
    /// 所以这里没有名字、没有意图种类字段。
    /// 允许空列表——"待机"：这条行动正常执行、只是没有效果（测试里的无害敌人、将来的沉睡意图）。
    /// 眩晕不是空行动：那是"行动存在、执行前被规则作废"，属于 Session 流程层。
    /// </summary>
    public sealed class EnemyAction
    {
        /// <summary>构造时拷贝，调用方之后改自己的列表不影响这里。</summary>
        public IReadOnlyList<EffectSpec> Effects { get; }

        public EnemyAction(IReadOnlyList<EffectSpec> effects)
        {
            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            var copy = new EffectSpec[effects.Count];
            for (var i = 0; i < copy.Length; i++)
            {
                // Value == 0 只可能是绕过工厂的 default(EffectSpec)，与 CardDefinition 同一道检查。
                if (effects[i].Value <= 0)
                {
                    throw new ArgumentException($"Effects[{i}] 未经 EffectSpec 静态工厂构造", nameof(effects));
                }

                // 敌人没有牌堆，Draw 会抽玩家的牌：这在任何版本都无意义，所以在 Core 拦；
                // "敌人给玩家上 Buff"只是 0.6 的范围限制，放在 Data 层校验，Core 不留痕。
                if (effects[i].Kind == EffectKind.Draw)
                {
                    throw new ArgumentException($"Effects[{i}] 是 Draw：敌人行动不能抽牌", nameof(effects));
                }

                copy[i] = effects[i];
            }

            Effects = copy;
        }
    }
}
