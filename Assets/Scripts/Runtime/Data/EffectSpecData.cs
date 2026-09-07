using System;
using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Cards;
using UnityEngine;

namespace CardRPGFramework.Data
{
    /// <summary>
    /// 卡牌效果的 Inspector 条目，是 EffectSpec 的可序列化影子：Unity 序列化走这里，不碰 readonly struct。
    /// ToSpec 复用 EffectSpec 的静态工厂，让 Core 的校验兜底。
    /// </summary>
    [Serializable]
    public struct EffectSpecData
    {
        [SerializeField] private EffectKind kind;
        [SerializeField] private EffectTarget target;
        [SerializeField] private int value;
        [SerializeField] private string buffId;

        /// <summary>供 EditMode 测试直接构造；Inspector 走序列化，不经这里。</summary>
        public EffectSpecData(EffectKind kind, EffectTarget target, int value, string buffId)
        {
            this.kind = kind;
            this.target = target;
            this.value = value;
            this.buffId = buffId;
        }

        public EffectSpec ToSpec() => kind switch
        {
            EffectKind.Damage => EffectSpec.Damage(value),
            EffectKind.Block => EffectSpec.Block(value),
            EffectKind.Heal => EffectSpec.Heal(value),
            EffectKind.ApplyBuff => EffectSpec.ApplyBuff(target, buffId, value),
            EffectKind.Draw => EffectSpec.Draw(value),
            _ => throw new InvalidOperationException($"未知的 EffectKind: {(int)kind}"),
        };

        /// <summary>
        /// 启动期校验。kind 与 target 的一致性必须在这里查：Damage / Block / Heal / Draw 的工厂不收 target，
        /// Inspector 里把伤害条的 target 填成 Self 会被 ToSpec 静默改回 Opponent，不在这里拦就到打出时才暴露。
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (value <= 0)
            {
                error = "value 必须大于 0";
                return false;
            }

            switch (kind)
            {
                case EffectKind.Damage:
                    if (target != EffectTarget.Opponent)
                    {
                        error = "Damage 的 target 必须是 Opponent";
                        return false;
                    }

                    break;
                case EffectKind.Block:
                case EffectKind.Heal:
                case EffectKind.Draw:
                    if (target != EffectTarget.Self)
                    {
                        error = $"{kind} 的 target 必须是 Self";
                        return false;
                    }

                    break;
                case EffectKind.ApplyBuff:
                    if (!BuffFactory.IsKnown(buffId))
                    {
                        error = $"ApplyBuff 的 buffId '{buffId}' 未知";
                        return false;
                    }

                    break;
                default:
                    error = $"kind 值 {(int)kind} 不是合法的 EffectKind";
                    return false;
            }

            error = null;
            return true;
        }
    }
}
