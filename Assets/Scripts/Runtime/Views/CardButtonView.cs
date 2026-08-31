using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardRPGFramework.Views
{
    /// <summary>
    /// 单张手牌按钮：只负责显示文本和把点击的手牌索引回传给 BattleView，不知道 Core/Data。
    /// </summary>
    public sealed class CardButtonView : MonoBehaviour
    {
        private Button _button;
        private TMP_Text _label;

        private int _handIndex;
        private Action<int> _onClicked;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _label = GetComponentInChildren<TMP_Text>();
            _button.onClick.AddListener(HandleClick);
        }

        /// <summary>显示这张卡并记住点击后要回传的手牌索引。</summary>
        public void Bind(int handIndex, string displayText, Action<int> onClicked)
        {
            _handIndex = handIndex;
            _onClicked = onClicked;
            _label.text = displayText;
            gameObject.SetActive(true);
        }

        /// <summary>手牌数量不足 5 张时，多余槽位隐藏。</summary>
        public void Hide()
        {
            _onClicked = null;
            gameObject.SetActive(false);
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(_handIndex);
        }
    }
}
