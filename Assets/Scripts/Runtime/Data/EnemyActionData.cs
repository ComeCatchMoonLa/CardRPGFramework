using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Enemies;
using UnityEngine;

namespace CardRPGFramework.Data
{
    /// <summary>
    /// 敌人一条行动的 Inspector 条目：名字 + 效果列表，是 EnemyAction 的可序列化影子。
    /// 名字只给 Inspector 看（列表里认得出哪条是咆哮），不进 Core——意图文案由效果推导，不存第二份真相。
    /// </summary>
    [Serializable]
    public struct EnemyActionData
    {
        [SerializeField] private string name;
        [SerializeField] private List<EffectSpecData> effects;

        /// <summary>供 EditMode 测试直接构造；Inspector 走序列化，不经这里。</summary>
        public EnemyActionData(string name, List<EffectSpecData> effects)
        {
            this.name = name;
            this.effects = effects;
        }

        public string Name => name;

        public EnemyAction ToAction()
        {
            var count = effects?.Count ?? 0;
            var specs = new EffectSpec[count];
            for (var i = 0; i < count; i++)
            {
                specs[i] = effects[i].ToSpec();
            }

            return new EnemyAction(specs);
        }

        /// <summary>
        /// 启动期校验。空列表拒绝：Core 允许空行动（测试的无害敌人、将来的沉睡意图，显示为"待机"），但目前没有任何内容需要它，
        /// 行动表里名字叫"咬"却忘了填效果的行应该在这里报出来，而不是显示成"待机"跑起来；配沉睡类敌人时删掉这一条。
        /// 之后逐条先过 EffectSpecData 自己的校验，再按转换后的 Core 值查敌人特有的一条：Draw 是规则真相（敌人没有牌堆，Core 也会拦，这里提前到启动期）。
        /// 0.6 曾拒绝 ApplyBuff 给 Opponent（回合型 Buff 没有减层，敌人上的虚弱会永远挂着），0.7 有了轮末减层与刚施加保护后放开，蓝奴隶贩子的耙靠它。
        /// </summary>
        public bool TryValidate(out string error)
        {
            var count = effects?.Count ?? 0;
            if (count == 0)
            {
                error = "effects 不能为空（没有待机 / 沉睡类敌人之前，漏填效果的行在这里报出）";
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                if (!effects[i].TryValidate(out var effectError))
                {
                    error = $"effects[{i}] {effectError}";
                    return false;
                }

                if (effects[i].ToSpec().Kind == EffectKind.Draw)
                {
                    error = $"effects[{i}] 敌人行动不能抽牌（Draw）";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
