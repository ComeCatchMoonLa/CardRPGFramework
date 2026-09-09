using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Buffs;
using CardRPGFramework.Core.Relics;

namespace CardRPGFramework.Core.Combatants
{
    /// <summary>
    /// 参战单位（玩家或敌人）的生命值、格挡、Buff 与遗物状态，负责伤害、治疗、格挡与两个容器的边界规则。
    /// 遗物列表与 Buff 字典并列而不是合并：遗物没有层数、不被清 Buff 的效果影响，规则读"是否持有"而不是读层数。
    /// </summary>
    public sealed class CombatantState
    {
        private readonly Dictionary<string, BuffState> _buffs = new();
        private readonly List<RelicState> _relics = new();

        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public int Block { get; private set; }
        public bool IsDead => CurrentHp <= 0;
        public IReadOnlyCollection<BuffState> Buffs => _buffs.Values;
        public IReadOnlyList<RelicState> Relics => _relics;

        public CombatantState(int maxHp)
        {
            if (maxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp), "MaxHp 必须大于 0");
            }

            MaxHp = maxHp;
            CurrentHp = maxHp;
        }

        /// <summary>受到伤害时先扣格挡，剩余部分再扣生命值；生命值不低于 0。</summary>
        public void TakeDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var absorbed = Math.Min(Block, amount);
            Block -= absorbed;

            var remaining = amount - absorbed;
            LoseHp(remaining);
        }

        /// <summary>
        /// 忽略格挡，直接扣血；不低于 0。这是实际减少 CurrentHp 前的唯一入口，
        /// 将来给钨条（失去生命时少扣 1）这类"不看来源、只要生命即将减少就生效"的机制用。
        /// 注意：无实体（受到的伤害改为 1）是格挡前的最终伤害修正，不挂这里，
        /// 挂钩点见 Docs/Phase 2 扩展边界批注.md 第 2 节时机地图。
        /// </summary>
        public void LoseHp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentHp = Math.Max(0, CurrentHp - amount);
        }

        /// <summary>同 Id 的 Buff 叠加层数，否则新建一条。</summary>
        public void ApplyBuff(BuffState buff)
        {
            if (_buffs.TryGetValue(buff.Id, out var existing))
            {
                existing.AddStacks(buff.Stacks);
            }
            else
            {
                _buffs[buff.Id] = buff;
            }
        }

        public int GetBuffStacks(string id) => _buffs.TryGetValue(id, out var buff) ? buff.Stacks : 0;

        public bool HasBuff(string id) => GetBuffStacks(id) > 0;

        /// <summary>同 Id 重复添加抛异常而不是静默合并：遗物唯一（原版拾取时直接跳过重复），重复只可能是配置错误。</summary>
        public void AddRelic(RelicState relic)
        {
            if (relic == null)
            {
                throw new ArgumentNullException(nameof(relic));
            }

            if (HasRelic(relic.Id))
            {
                throw new ArgumentException($"遗物 '{relic.Id}' 已持有，不能重复添加", nameof(relic));
            }

            _relics.Add(relic);
        }

        public bool HasRelic(string id)
        {
            foreach (var relic in _relics)
            {
                if (relic.Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>治疗不超过最大生命值。</summary>
        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
        }

        public void GainBlock(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Block += amount;
        }

        public void ClearBlock()
        {
            Block = 0;
        }
    }
}
