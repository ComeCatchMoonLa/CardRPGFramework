using CardRPGFramework.Controllers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardRPGFramework.Views
{
    /// <summary>通关 / 失败结束页：只调 Restart。</summary>
    public sealed class ResultPageView : MonoBehaviour
    {
        [SerializeField] private RunController runController;
        [SerializeField] private Button restartButton;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text summaryText;

        private void Start()
        {
            restartButton.onClick.AddListener(runController.Restart);
        }

        public void SetOutcome(bool cleared)
        {
            resultText.text = cleared ? "通关" : "失败";
            var run = runController.Run;
            // 失败不推进 NodeIndex，已打场数是当前节点 + 1；通关后 NodeIndex 就是场数。
            var fights = run.IsFailed ? run.NodeIndex + 1 : run.NodeIndex;
            summaryText.text = $"打了 {fights} 场 · 牌组 {run.Deck.Count} 张";
        }
    }
}
