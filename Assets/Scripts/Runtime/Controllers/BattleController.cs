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

            var random = useFixedSeed ? new System.Random(seed) : new System.Random();
            _session = new BattleSession(battleConfig.ToSetup(), battleConfig.ToDeckDefinitions(), random);
            _session.StartBattle();
        }

        public bool PlayCard(int handIndex)
        {
            return _session != null && _session.TryPlayCard(handIndex);
        }

        public bool EndTurn()
        {
            return _session != null && _session.TryEndPlayerTurn();
        }
    }
}
