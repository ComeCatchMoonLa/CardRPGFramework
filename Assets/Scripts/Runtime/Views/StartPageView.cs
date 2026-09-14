using CardRPGFramework.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace CardRPGFramework.Views
{
    /// <summary>开始页：只调 StartRun。</summary>
    public sealed class StartPageView : MonoBehaviour
    {
        [SerializeField] private RunController runController;
        [SerializeField] private Button startButton;

        private void Start()
        {
            startButton.onClick.AddListener(runController.StartRun);
        }
    }
}
