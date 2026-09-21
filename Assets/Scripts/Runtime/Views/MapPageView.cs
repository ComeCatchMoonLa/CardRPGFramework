using CardRPGFramework.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace CardRPGFramework.Views
{
    /// <summary>地图页。只调 EnterCurrentNode。点未接线时 Refresh 跳过。</summary>
    public sealed class MapPageView : MonoBehaviour
    {
        private static readonly Color Completed = new(0.5f, 0.5f, 0.5f);
        private static readonly Color Current = Color.white;
        private static readonly Color Upcoming = new(0.2f, 0.2f, 0.2f);

        [SerializeField] private RunController runController;
        [SerializeField] private Button enterButton;
        [SerializeField] private Graphic[] nodeDots;

        private void Start()
        {
            enterButton.onClick.AddListener(runController.EnterCurrentNode);
        }

        public void Refresh()
        {
            if (nodeDots == null || nodeDots.Length == 0)
            {
                return;
            }

            var run = runController.Run;
            if (run == null)
            {
                return;
            }

            var nodeIndex = run.NodeIndex;
            for (var i = 0; i < nodeDots.Length; i++)
            {
                var dot = nodeDots[i];
                if (dot == null)
                {
                    continue;
                }

                if (i < nodeIndex)
                {
                    dot.color = Completed;
                }
                else if (i == nodeIndex)
                {
                    dot.color = Current;
                }
                else
                {
                    dot.color = Upcoming;
                }
            }
        }
    }
}
