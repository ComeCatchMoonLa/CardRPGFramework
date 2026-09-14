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

        private void Start()
        {
            restartButton.onClick.AddListener(runController.Restart);
        }

        public void SetOutcome(bool cleared)
        {
            resultText.text = cleared ? "通关" : "失败";
        }
    }
}
