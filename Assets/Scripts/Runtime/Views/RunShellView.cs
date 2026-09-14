using System.Collections.Generic;
using System.Text;
using CardRPGFramework.Controllers;
using TMPro;
using UnityEngine;

namespace CardRPGFramework.Views
{
    /// <summary>
    /// 常驻顶栏：Start 不读 RunConfig；战斗中生命跟局内，其它页读 Run。
    /// 切页时始终显示，不进三页 SetActive。
    /// </summary>
    public sealed class RunShellView : MonoBehaviour
    {
        [SerializeField] private RunController runController;
        [SerializeField] private BattleController battleController;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text deckCountText;
        [SerializeField] private TMP_Text relicsText;

        public void Refresh()
        {
            var run = runController.Run;
            if (run == null)
            {
                hpText.text = "生命 --/--";
                goldText.text = "金币 --";
                deckCountText.text = "牌组 --";
                relicsText.text = "遗物：无";
                return;
            }

            if (battleController.IsReady)
            {
                var player = battleController.Player;
                hpText.text = $"生命 {player.CurrentHp}/{player.MaxHp}";
            }
            else
            {
                hpText.text = $"生命 {run.CurrentHp}/{run.MaxHp}";
            }

            goldText.text = $"金币 {run.Gold}";
            deckCountText.text = $"牌组 {run.Deck.Count}";
            relicsText.text = FormatRelics(run.RelicIds);
        }

        private string FormatRelics(IReadOnlyList<string> relicIds)
        {
            if (relicIds.Count == 0)
            {
                return "遗物：无";
            }

            var builder = new StringBuilder("遗物：");
            for (var i = 0; i < relicIds.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("  ");
                }

                builder.Append(battleController.RelicDisplayName(relicIds[i]));
            }

            return builder.ToString();
        }
    }
}
