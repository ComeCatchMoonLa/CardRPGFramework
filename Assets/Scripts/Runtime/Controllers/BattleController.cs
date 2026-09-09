using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Combatants;
using CardRPGFramework.Data;
using UnityEngine;

namespace CardRPGFramework.Controllers
{
    /// <summary>
    /// 场景中的战斗组装入口：根据 BattleConfig 创建并持有一场 BattleSession，
    /// 不实现具体战斗规则，只负责转发命令和暴露只读状态给 Views。
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
        [SerializeField] private BattleConfig battleConfig;
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int seed;

        private BattleSession _session;
        // 遗物 Id → 显示名。显示名在 RelicData 资产里，View 不该认识 Data 类型，所以由这里翻一次；
        // 遗物 Id 本身经 Player.Relics 读，与 Buff 行读 Player.Buffs 同一条路径。
        private readonly Dictionary<string, string> _relicDisplayNames = new();

        /// <summary>配置缺失或校验失败时为 false，此时其余状态均为未初始化的默认值。</summary>
        public bool IsReady => _session != null;

        public BattlePhase Phase => _session?.Phase ?? BattlePhase.NotStarted;
        public int TurnNumber => _session?.TurnNumber ?? 0;
        public int Energy => _session?.Energy ?? 0;
        public int EnergyPerTurn => _session?.EnergyPerTurn ?? 0;
        public int EnemyDamage => _session?.EnemyDamage ?? 0;
        public CombatantState Player => _session?.Player;
        public CombatantState Enemy => _session?.Enemy;
        public IReadOnlyList<CardDefinition> Hand => _session?.Hand ?? Array.Empty<CardDefinition>();
        public int DrawPileCount => _session?.DrawPileCount ?? 0;
        public int DiscardPileCount => _session?.DiscardPileCount ?? 0;
        public int ExhaustPileCount => _session?.ExhaustPileCount ?? 0;

        private void Awake()
        {
            if (battleConfig == null)
            {
                Debug.LogError("BattleController: 未指定 BattleConfig，战斗无法初始化。");
                return;
            }

            if (!battleConfig.TryValidate(out var error))
            {
                Debug.LogError($"BattleController: {error}");
                return;
            }

            foreach (var relic in battleConfig.Relics)
            {
                _relicDisplayNames[relic.Id] = relic.DisplayName;
            }

            var random = useFixedSeed ? new System.Random(seed) : new System.Random();
            _session = new BattleSession(battleConfig.ToSetup(), battleConfig.ToDeckDefinitions(), random,
                battleConfig.ToRelicStates());
            _session.StartBattle();
        }

        /// <summary>未知 Id 原样返回而不抛异常，与 BuffDisplayNames 同一条兜底规则：启动校验已经拦过未知遗物 Id。</summary>
        public string RelicDisplayName(string relicId) =>
            _relicDisplayNames.TryGetValue(relicId, out var displayName) ? displayName : relicId;

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
