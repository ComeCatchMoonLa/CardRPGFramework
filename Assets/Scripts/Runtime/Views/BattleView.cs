using CardRPGFramework.Controllers;
using CardRPGFramework.Core.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardRPGFramework.Views
{
    /// <summary>
    /// 战斗整体状态显示与结束回合按钮。不直接调用 Core/Data，命令执行后主动读取
    /// BattleController 的只读状态刷新界面，不引入事件/观察者机制。
    /// </summary>
    public sealed class BattleView : MonoBehaviour
    {
        [SerializeField] private BattleController battleController;

        [SerializeField] private TMP_Text playerStatusText;
        [SerializeField] private TMP_Text enemyStatusText;
        [SerializeField] private TMP_Text enemyIntentText;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private TMP_Text resultText;

        [SerializeField] private CardButtonView[] cardSlots;
        [SerializeField] private Button endTurnButton;

        [SerializeField] private EndScreenView endScreenView;

        private void Start()
        {
            endTurnButton.onClick.AddListener(HandleEndTurnClicked);
            Refresh();
        }

        private void HandleEndTurnClicked()
        {
            resultText.text = battleController.EndTurn() ? "回合结束" : "无法结束回合";
            Refresh();
        }

        private void HandleCardClicked(int handIndex)
        {
            var hand = battleController.Hand;
            if (handIndex < 0 || handIndex >= hand.Count)
            {
                resultText.text = "无法使用该卡牌";
                Refresh();
                return;
            }

            var cardName = hand[handIndex].DisplayName;
            resultText.text = battleController.PlayCard(handIndex) ? $"使用了 {cardName}" : "无法使用该卡牌";
            Refresh();
        }

        private void Refresh()
        {
            if (!battleController.IsReady)
            {
                resultText.text = "战斗未初始化，请检查 BattleConfig";
                return;
            }

            var player = battleController.Player;
            var enemy = battleController.Enemy;

            playerStatusText.text = $"HP {player.CurrentHp}/{player.MaxHp}  格挡 {player.Block}";
            enemyStatusText.text = $"敌人 HP {enemy.CurrentHp}/{enemy.MaxHp}  格挡 {enemy.Block}";
            enemyIntentText.text = $"敌人意图：攻击 {battleController.EnemyDamage}";
            energyText.text = $"能量 {battleController.Energy}/{battleController.EnergyPerTurn}";
            turnText.text = $"回合 {battleController.TurnNumber}";

            RefreshHand();
            RefreshEndScreen();
        }

        private void RefreshHand()
        {
            var hand = battleController.Hand;
            for (var i = 0; i < cardSlots.Length; i++)
            {
                if (i < hand.Count)
                {
                    var card = hand[i];
                    cardSlots[i].Bind(i, $"{card.DisplayName}\n费用 {card.Cost}\n数值 {card.Value}", HandleCardClicked);
                }
                else
                {
                    cardSlots[i].Hide();
                }
            }
        }

        private void RefreshEndScreen()
        {
            switch (battleController.Phase)
            {
                case BattlePhase.Victory:
                    endScreenView.Show("胜利！");
                    break;
                case BattlePhase.Defeat:
                    endScreenView.Show("失败");
                    break;
                default:
                    endScreenView.Hide();
                    break;
            }
        }
    }
}
