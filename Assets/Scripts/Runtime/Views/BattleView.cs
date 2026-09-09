using System;
using System.Text;
using CardRPGFramework.Controllers;
using CardRPGFramework.Core.Battle;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Core.Combatants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardRPGFramework.Views
{
    /// <summary>
    /// 战斗整体状态显示与结束回合按钮。不直接调用 Core/Data，命令执行后主动读取
    /// BattleController 的只读状态刷新界面，不引入事件/观察者机制。
    /// 卡面与意图上的伤害数字来自 Controller 转发的只读预览，这里不出现任何公式常数。
    /// </summary>
    public sealed class BattleView : MonoBehaviour
    {
        [SerializeField] private BattleController battleController;

        [SerializeField] private TMP_Text playerStatusText;
        [SerializeField] private TMP_Text playerBuffsText;
        [SerializeField] private TMP_Text enemyStatusText;
        [SerializeField] private TMP_Text enemyBuffsText;
        [SerializeField] private TMP_Text enemyIntentText;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text pileCountsText;
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
            playerBuffsText.text = FormatBuffs(player);
            enemyStatusText.text = $"敌人 HP {enemy.CurrentHp}/{enemy.MaxHp}  格挡 {enemy.Block}";
            enemyBuffsText.text = FormatBuffs(enemy);
            enemyIntentText.text = $"敌人意图：攻击 {battleController.PreviewEnemyAttack()}";
            energyText.text = $"能量 {battleController.Energy}/{battleController.EnergyPerTurn}";
            pileCountsText.text =
                $"抽牌 {battleController.DrawPileCount}  弃牌 {battleController.DiscardPileCount}  消耗 {battleController.ExhaustPileCount}";
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
                    var description = CardDescriptionFormatter.Format(card, battleController.PreviewPlayerAttack);
                    cardSlots[i].Bind(i, $"{card.DisplayName}\n{TypeLabel(card.Type)} · 费用 {card.Cost}\n{description}",
                        HandleCardClicked);
                }
                else
                {
                    cardSlots[i].Hide();
                }
            }
        }

        // 类型中文暂时只有这一处用到，留在 View 里；出现第二个调用方（1.0 奖励页）再抽成与 BuffDisplayNames 同类的静态类。
        private static string TypeLabel(CardType type) => type switch
        {
            CardType.Attack => "攻击",
            CardType.Skill => "技能",
            CardType.Power => "能力",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "未知的 CardType"),
        };

        /// <summary>每个 Buff 一段 `名称 层数`，多段用两个空格连接；没有可显示的 Buff 时为空串。</summary>
        private static string FormatBuffs(CombatantState who)
        {
            var builder = new StringBuilder();
            foreach (var buff in who.Buffs)
            {
                // 0 层 Buff 在 0.7 轮末减层之前仍留在内部字典里（HasBuff 用 > 0 兜住），显示层只过滤、不改数据。
                if (buff.Stacks <= 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("  ");
                }

                builder.Append(BuffDisplayNames.Of(buff.Id)).Append(' ').Append(buff.Stacks);
            }

            return builder.ToString();
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
