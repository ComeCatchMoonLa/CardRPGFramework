using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Combatants;
using CardRPGFramework.Core.Enemies;
using CardRPGFramework.Core.Run;
using CardRPGFramework.Data;
using UnityEngine;

namespace CardRPGFramework.Controllers
{
    /// <summary>
    /// 一场战斗的组装入口：由 Begin 创建 Session，只负责转发命令和暴露只读状态。
    /// 不持有 RunConfig，不在 Begin 里填遗物显示名。
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
        private BattleSession _session;
        // 遗物 Id → 显示名。显示名在 RelicData 资产里，View 不该认识 Data 类型，所以由这里翻一次；
        // 遗物 Id 本身从 Player.Relics 读，和 Buff 行读 Player.Buffs 同一条路径。
        private readonly Dictionary<string, string> _relicDisplayNames = new();

        /// <summary>尚未 Begin 或已 DiscardSession 时为 false。</summary>
        public bool IsReady => _session != null;

        public BattlePhase Phase => _session?.Phase ?? BattlePhase.NotStarted;
        public int TurnNumber => _session?.TurnNumber ?? 0;
        public int Energy => _session?.Energy ?? 0;
        public int EnergyPerTurn => _session?.EnergyPerTurn ?? 0;
        public CombatantState Player => _session?.Player;
        public CombatantState Enemy => _session?.Enemy;
        // 敌人显示名在 Core 的 EnemyDefinition 里（和 CardDefinition.DisplayName 一样是内容数据），不需要遗物那种 Id → 显示名字典。
        public string EnemyDisplayName => _session?.EnemyDefinition.DisplayName ?? string.Empty;
        /// <summary>未初始化时为 null；View 在 IsReady 为 false 时不会读。</summary>
        public EnemyAction CurrentEnemyAction => _session?.CurrentEnemyAction;
        public IReadOnlyList<CardDefinition> Hand => _session?.Hand ?? Array.Empty<CardDefinition>();
        public int DrawPileCount => _session?.DrawPileCount ?? 0;
        public int DiscardPileCount => _session?.DiscardPileCount ?? 0;
        public int ExhaustPileCount => _session?.ExhaustPileCount ?? 0;

        /// <summary>
        /// 用 input 五个字段 new BattleSession 再 StartBattle。Random 只用 input.Random。
        /// 已有一场时先丢掉旧 Session。不读 RunConfig，不填遗物显示名。
        /// </summary>
        public void Begin(BattleInput input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            _session = new BattleSession(input.Setup, input.Enemy, input.Deck, input.Random, input.Relics);
            _session.StartBattle();
        }

        /// <summary>丢掉当前 Session。不清遗物显示名表。</summary>
        public void DiscardSession()
        {
            _session = null;
        }

        /// <summary>未知 Id 原样返回而不抛异常，与 BuffDisplayNames 同一条兜底规则：启动校验已经拦过未知遗物 Id。</summary>
        public string RelicDisplayName(string relicId) =>
            _relicDisplayNames.TryGetValue(relicId, out var displayName) ? displayName : relicId;

        /// <summary>整表替换。null 抛。空列表 = 清空。不读 RunConfig，不碰 Session。</summary>
        public void SetRelicDisplayNames(IReadOnlyList<RelicData> relics)
        {
            if (relics == null)
            {
                throw new ArgumentNullException(nameof(relics));
            }

            _relicDisplayNames.Clear();
            foreach (var relic in relics)
            {
                _relicDisplayNames[relic.Id] = relic.DisplayName;
            }
        }

        public bool PlayCard(int handIndex)
        {
            return _session != null && _session.TryPlayCard(handIndex);
        }

        public bool EndTurn()
        {
            return _session != null && _session.TryEndPlayerTurn();
        }

        // 只读预览原样转发，不加逻辑；未初始化时的兜底值只是让签名完整，View 在 IsReady 为 false 时不会调到这里。
        public int PreviewPlayerAttack(int baseDamage) => _session?.PreviewPlayerAttack(baseDamage) ?? baseDamage;

        public int PreviewEnemyAttack() => _session?.PreviewEnemyAttack() ?? 0;
    }
}
