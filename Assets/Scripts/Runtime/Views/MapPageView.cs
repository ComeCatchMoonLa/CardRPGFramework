using CardRPGFramework.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace CardRPGFramework.Views
{
    /// <summary>地图页。只调 EnterCurrentNode。点未接线时 Refresh 跳过。</summary>
    public sealed class MapPageView : MonoBehaviour
    {
        [SerializeField] private RunController runController;
        [SerializeField] private Button enterButton;
        [SerializeField] private Graphic[] nodeDots;

        private void Start()
        {
            if (enterButton != null)
            {
                enterButton.onClick.AddListener(runController.EnterCurrentNode);
            }
        }

        public void Refresh()
        {
            if (nodeDots == null || nodeDots.Length == 0)
            {
                return;
            }
        }
    }
}
