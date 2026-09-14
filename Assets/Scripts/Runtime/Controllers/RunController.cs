using System;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Run;
using CardRPGFramework.Data;
using CardRPGFramework.Views;
using UnityEngine;

namespace CardRPGFramework.Controllers
{
    /// <summary>
    /// 短 Run 流程：持有 RunState 与唯一一份 RunConfig。局内命令入口，写回只在 ApplyOutcomeIfEnded。
    /// </summary>
    public sealed class RunController : MonoBehaviour
    {
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private BattleController battleController;
        [SerializeField] private BattleView battleView;
        [SerializeField] private RunShellView runShellView;
        [SerializeField] private ResultPageView resultPageView;
        [SerializeField] private GameObject startPage;
        [SerializeField] private GameObject combatPage;
        [SerializeField] private GameObject resultPage;

        public RunState Run { get; private set; }

        private void Awake()
        {
            ShowStart();
            RefreshShell();
        }

        /// <summary>校验失败则停留在 Start。通过则填遗物字典、第一次 Begin、显示 Combat。</summary>
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
            battleController.Begin(Run.CreateBattleInput());
            ShowCombat();
            battleView.Refresh();
            RefreshShell();
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

        public void Restart()
        {
            Run = null;
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
                    if (Run.IsCleared)
                    {
                        battleController.DiscardSession();
                        ShowResult(cleared: true);
                    }
                    else
                    {
                        battleController.Begin(Run.CreateBattleInput());
                        battleView.Refresh();
                    }

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
            SetPages(startVisible: true, combatVisible: false, resultVisible: false);
        }

        private void ShowCombat()
        {
            SetPages(startVisible: false, combatVisible: true, resultVisible: false);
        }

        private void ShowResult(bool cleared)
        {
            SetPages(startVisible: false, combatVisible: false, resultVisible: true);
            resultPageView.SetOutcome(cleared);
        }

        private void SetPages(bool startVisible, bool combatVisible, bool resultVisible)
        {
            startPage.SetActive(startVisible);
            combatPage.SetActive(combatVisible);
            resultPage.SetActive(resultVisible);
        }

        private void RefreshShell()
        {
            runShellView.Refresh();
        }
    }
}
