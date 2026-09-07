using System;

namespace CardRPGFramework.Core.Cards
{
    /// <summary>
    /// "做什么、对谁、多少"的不可变四元组，只描述效果、不带执行逻辑；把它变成 IAction 的地方只有 BattleSession。
    /// 选 readonly struct 而不是 class：它没有身份、没有可变状态，两张牌上的"对敌人造成 6 点"是同一个值；
    /// 需要沿管线传递并被 Rule 改写的 DamageContext 才用引用类型。
    /// 代价是 default(EffectSpec) 会绕过校验（全 0、BuffId 为 null），所以构造函数私有、只经下面的静态工厂构造，
    /// 也不要 new EffectSpec[n] 这类未初始化数组。
    /// </summary>
    public readonly struct EffectSpec
    {
        public EffectKind Kind { get; }
        public EffectTarget Target { get; }

        /// <summary>按 Kind 解释：伤害 / 格挡 / 治疗量 / Buff 层数 / 抽牌张数。</summary>
        public int Value { get; }

        /// <summary>
        /// 仅 ApplyBuff 使用，其余为 null。这里只是字符串 Id，解析成 BuffState 发生在 Core.Battle，
        /// 让 Core.Cards 不必引用 Core.Buffs。
        /// </summary>
        public string BuffId { get; }

        private EffectSpec(EffectKind kind, EffectTarget target, int value, string buffId)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value 必须大于 0");
            }

            if (kind == EffectKind.ApplyBuff && string.IsNullOrEmpty(buffId))
            {
                throw new ArgumentException("ApplyBuff 必须指定 BuffId", nameof(buffId));
            }

            Kind = kind;
            Target = target;
            Value = value;
            BuffId = buffId;
        }

        // Damage / Block / Heal 的目标写死，是因为 0.2 没有"对自己造成伤害""给敌人格挡"的卡；
        // 出现时再给对应工厂放开 Target 参数，不提前开。
        public static EffectSpec Damage(int amount) => new(EffectKind.Damage, EffectTarget.Opponent, amount, null);

        public static EffectSpec Block(int amount) => new(EffectKind.Block, EffectTarget.Self, amount, null);

        public static EffectSpec Heal(int amount) => new(EffectKind.Heal, EffectTarget.Self, amount, null);

        public static EffectSpec ApplyBuff(EffectTarget target, string buffId, int stacks) =>
            new(EffectKind.ApplyBuff, target, stacks, buffId);

        // 抽牌只能抽打出者自己的牌堆（战斗里只有一个 CardPile），所以不开放 Target，Draw 非 Self 在类型上就构造不出来。
        public static EffectSpec Draw(int count) => new(EffectKind.Draw, EffectTarget.Self, count, null);
    }
}
