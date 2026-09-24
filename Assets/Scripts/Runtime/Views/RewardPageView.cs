using CardRPGFramework.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace CardRPGFramework.Views
{
    /// <summary>奖励页。只调 SelectReward / SkipReward。卡槽未接线时 Refresh 跳过。</summary>
    public sealed class RewardPageView : MonoBehaviour
    {
        [SerializeField] private RunController runController;
        [SerializeField] private CardButtonView[] cardSlots;
        [SerializeField] private Button skipButton;

        private void Start()
        {
            if (skipButton == null)
            {
                return;
            }

            skipButton.onClick.AddListener(runController.SkipReward);
        }

        public void Refresh()
        {
            if (cardSlots == null || cardSlots.Length != 3 || runController.RewardChoices.Count != 3)
            {
                return;
            }
        }
    }
}
