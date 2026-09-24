using System;
using System.Collections.Generic;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Run;
using CardRPGFramework.Data;
using CardRPGFramework.Views;
using UnityEngine;

namespace CardRPGFramework.Controllers
{
    /// <summary>
    /// 短 Run 流程：持有 RunState 与唯一一份 RunConfig。开始去地图；进入节点才开战；写回只在 ApplyOutcomeIfEnded。
    /// </summary>
    public sealed class RunController : MonoBehaviour
    {
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private BattleController battleController;
        [SerializeField] private BattleView battleView;
        [SerializeField] private MapPageView mapPageView;
        [SerializeField] private RewardPageView rewardPageView;
        [SerializeField] private RunShellView runShellView;
        [SerializeField] private ResultPageView resultPageView;
        [SerializeField] private GameObject startPage;
        [SerializeField] private GameObject mapPage;
        [SerializeField] private GameObject combatPage;
        [SerializeField] private GameObject rewardPage;
        [SerializeField] private GameObject resultPage;

        public RunState Run { get; private set; }

        /// <summary>空列表表示未在等选。不要用 null，也不要另做一套等待标志。</summary>
        public IReadOnlyList<CardDefinition> RewardChoices { get; private set; } = Array.Empty<CardDefinition>();

        private void Awake()
        {
            ShowStart();
            RefreshShell();
        }

        /// <summary>校验失败则停留在 Start。通过则填遗物字典、显示地图，不 Begin。</summary>
        public void StartRun()
        {
            if (runConfig == null)
            {
                Debug.LogError("RunController: 未指定 RunConfig，无法开始一局。");
                return;
            }

            if (!runConfig.TryValidate(out var error))
            {
                Debug.LogError($"RunController: {error}");
                return;
            }

            Run = runConfig.CreateRunState();
            battleController.SetRelicDisplayNames(runConfig.Relics);
            ShowMap();
        }

        /// <summary>
        /// Run == null / IsOver / IsReady / 正在等选奖励则直接 return。
        /// 未开战就是 Run == null。等选时 Session 已丢，IsReady 挡不住，必须看 RewardChoices。
        /// </summary>
        public void EnterCurrentNode()
        {
            if (Run == null || Run.IsOver || battleController.IsReady || RewardChoices.Count != 0)
            {
                return;
            }

            switch (Run.CurrentNodeType)
            {
                case NodeType.Combat:
                    battleController.Begin(Run.CreateBattleInput());
                    ShowCombat();
                    battleView.Refresh();
                    RefreshShell();
                    break;
                default:
                    Debug.LogError($"RunController: 未实现的节点类型 {Run.CurrentNodeType}，留在地图。");
                    break;
            }
        }

        public bool PlayCard(int handIndex)
        {
            if (Run == null || !battleController.IsReady)
            {
                return false;
            }

            var played = battleController.PlayCard(handIndex);
            ApplyOutcomeIfEnded();
            RefreshShell();
            return played;
        }

        public bool EndTurn()
        {
            if (Run == null || !battleController.IsReady)
            {
                return false;
            }

            var ended = battleController.EndTurn();
            ApplyOutcomeIfEnded();
            RefreshShell();
            return ended;
        }

        /// <summary>未在等选或下标不在 0..2 则 return。加卡后未通关回地图，通关才进结束页。</summary>
        public void SelectReward(int index)
        {
            if (RewardChoices.Count == 0 || index < 0 || index > 2)
            {
                return;
            }

            Run.AddCard(RewardChoices[index]);
            RewardChoices = Array.Empty<CardDefinition>();
            RefreshShell();
            LeaveReward();
        }

        /// <summary>未在等选则 return。不加卡，去向与选卡相同。</summary>
        public void SkipReward()
        {
            if (RewardChoices.Count == 0)
            {
                return;
            }

            RewardChoices = Array.Empty<CardDefinition>();
            RefreshShell();
            LeaveReward();
        }

        public void Restart()
        {
            Run = null;
            RewardChoices = Array.Empty<CardDefinition>();
            battleController.DiscardSession();
            battleController.SetRelicDisplayNames(Array.Empty<RelicData>());
            ShowStart();
            RefreshShell();
        }

        private void ApplyOutcomeIfEnded()
        {
            switch (battleController.Phase)
            {
                case BattlePhase.Victory:
                    Run.ApplyResult(true, battleController.Player.CurrentHp);
                    battleController.DiscardSession();
                    ShowReward();
                    break;
                case BattlePhase.Defeat:
                    Run.ApplyResult(false, battleController.Player.CurrentHp);
                    battleController.DiscardSession();
                    ShowResult(cleared: false);
                    break;
            }
        }

        private void ShowStart()
        {
            SetPages(startVisible: true, mapVisible: false, combatVisible: false, rewardVisible: false, resultVisible: false);
        }

        private void ShowMap()
        {
            SetPages(startVisible: false, mapVisible: true, combatVisible: false, rewardVisible: false, resultVisible: false);
            mapPageView.Refresh();
            RefreshShell();
        }

        private void ShowCombat()
        {
            SetPages(startVisible: false, mapVisible: false, combatVisible: true, rewardVisible: false, resultVisible: false);
        }

        private void ShowReward()
        {
            var pool = new List<CardDefinition>(runConfig.RewardPool.Count);
            foreach (var card in runConfig.RewardPool)
            {
                pool.Add(card.ToDefinition());
            }

            RewardChoices = Run.CreateRewardChoices(pool);
            SetPages(startVisible: false, mapVisible: false, combatVisible: false, rewardVisible: true, resultVisible: false);
            rewardPageView.Refresh();
            RefreshShell();
        }

        private void ShowResult(bool cleared)
        {
            SetPages(startVisible: false, mapVisible: false, combatVisible: false, rewardVisible: false, resultVisible: true);
            resultPageView.SetOutcome(cleared);
        }

        private void LeaveReward()
        {
            if (Run.IsCleared)
            {
                ShowResult(cleared: true);
            }
            else
            {
                ShowMap();
            }
        }

        private void SetPages(bool startVisible, bool mapVisible, bool combatVisible, bool rewardVisible, bool resultVisible)
        {
            startPage.SetActive(startVisible);
            mapPage.SetActive(mapVisible);
            combatPage.SetActive(combatVisible);
            rewardPage.SetActive(rewardVisible);
            resultPage.SetActive(resultVisible);
        }

        private void RefreshShell()
        {
            runShellView.Refresh();
        }
    }
}
