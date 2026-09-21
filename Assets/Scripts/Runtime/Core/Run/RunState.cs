using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Enemies;
using CardRPGFramework.Core.Relics;

namespace CardRPGFramework.Core.Run
{
    /// <summary>
    /// 一局局外状态：输出 BattleInput、接收胜负与剩余生命、往牌组加卡。不创建 BattleSession。
    /// </summary>
    public sealed class RunState
    {
        private readonly List<CardDefinition> _deck;
        private readonly List<string> _relicIds;
        private readonly List<EnemyDefinition> _encounters;
        private readonly NodeType[] _nodeTypes;
        // 同一节点第二次 ApplyResult 若当 no-op，胜利会落在已经 +1 的下标上，nodeIndex 偷偷 +2。
        private bool _inputIssued;

        public int RunSeed { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public int EnergyPerTurn { get; }
        public int HandSize { get; }
        public int Gold => 0;
        public int NodeIndex { get; private set; }
        public IReadOnlyList<CardDefinition> Deck => _deck;
        public IReadOnlyList<string> RelicIds => _relicIds;
        public IReadOnlyList<NodeType> NodeTypes => _nodeTypes;
        public bool IsFailed { get; private set; }
        public bool IsCleared => !IsFailed && NodeIndex >= _encounters.Count;
        public bool IsOver => IsFailed || IsCleared;

        /// <summary>
        /// 未结束返回当前节点类型。失败后下标仍合法，必须看 IsOver，不能用下标当哨兵。
        /// </summary>
        public NodeType CurrentNodeType
        {
            get
            {
                if (IsOver)
                {
                    throw new InvalidOperationException("本局已结束，不能读取当前节点类型");
                }

                return _nodeTypes[NodeIndex];
            }
        }

        public RunState(int runSeed, BattleSetup playerSetup,
            IReadOnlyList<CardDefinition> deck, IReadOnlyList<string> relicIds,
            IReadOnlyList<EnemyDefinition> encounters)
        {
            if (deck == null)
            {
                throw new ArgumentNullException(nameof(deck));
            }

            if (relicIds == null)
            {
                throw new ArgumentNullException(nameof(relicIds));
            }

            if (encounters == null)
            {
                throw new ArgumentNullException(nameof(encounters));
            }

            _deck = new List<CardDefinition>(deck);
            _relicIds = new List<string>(relicIds);
            _encounters = new List<EnemyDefinition>(encounters);

            if (_encounters.Count != 3)
            {
                throw new ArgumentException("encounters 必须恰好 3 条", nameof(encounters));
            }

            for (var i = 0; i < _encounters.Count; i++)
            {
                if (_encounters[i] == null)
                {
                    throw new ArgumentException($"encounters[{i}] 为 null", nameof(encounters));
                }
            }

            var seen = new HashSet<string>();
            foreach (var id in _relicIds)
            {
                if (!RelicFactory.IsKnown(id))
                {
                    throw new ArgumentException($"未知的遗物 Id: {id}", nameof(relicIds));
                }

                if (!seen.Add(id))
                {
                    throw new ArgumentException($"遗物 Id 重复: {id}", nameof(relicIds));
                }
            }

            _nodeTypes = new NodeType[_encounters.Count];
            for (var i = 0; i < _nodeTypes.Length; i++)
            {
                _nodeTypes[i] = NodeType.Combat;
            }

            RunSeed = runSeed;
            MaxHp = playerSetup.PlayerMaxHp;
            CurrentHp = playerSetup.PlayerCurrentHp;
            EnergyPerTurn = playerSetup.EnergyPerTurn;
            HandSize = playerSetup.HandSize;
        }

        public BattleInput CreateBattleInput()
        {
            if (IsOver)
            {
                throw new InvalidOperationException("本局已结束，不能再创建战斗输入");
            }

            var relics = new List<RelicState>(_relicIds.Count);
            foreach (var id in _relicIds)
            {
                relics.Add(RelicFactory.Create(id));
            }

            var input = new BattleInput(
                new BattleSetup(MaxHp, EnergyPerTurn, HandSize, CurrentHp),
                new List<CardDefinition>(_deck),
                relics,
                _encounters[NodeIndex],
                new Random(unchecked(RunSeed * 397 ^ NodeIndex)));
            _inputIssued = true;
            return input;
        }

        public void ApplyResult(bool won, int remainingHp)
        {
            if (IsOver || !_inputIssued)
            {
                throw new InvalidOperationException("本节点尚未发出战斗输入，或已经写过结果");
            }

            if (won)
            {
                if (remainingHp <= 0 || remainingHp > MaxHp)
                {
                    throw new ArgumentOutOfRangeException(nameof(remainingHp), "胜利剩余生命必须大于 0 且不超过最大生命");
                }

                CurrentHp = remainingHp;
                NodeIndex++;
                _inputIssued = false;
                return;
            }

            IsFailed = true;
        }

        public void AddCard(CardDefinition card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (IsFailed)
            {
                throw new InvalidOperationException("本局已失败，不能再加卡");
            }

            _deck.Add(card);
        }
    }
}
