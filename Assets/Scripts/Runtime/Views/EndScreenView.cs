using TMPro;
using UnityEngine;

namespace CardRPGFramework.Views
{
    /// <summary>
    /// 战斗结束遮罩：显示胜负文本，同时用不透明背景挡住底下的手牌与结束回合按钮，防止误操作。
    /// 需要在 Hierarchy 里保持激活状态（不要手动关掉）：Awake 里会立刻自我隐藏一次，
    /// 不依赖调用方在初始化时记得调 Hide()。
    /// </summary>
    public sealed class EndScreenView : MonoBehaviour
    {
        private TMP_Text _resultText;

        private void Awake()
        {
            _resultText = GetComponentInChildren<TMP_Text>();
            gameObject.SetActive(false);
        }

        public void Show(string message)
        {
            gameObject.SetActive(true);
            _resultText.text = message;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
