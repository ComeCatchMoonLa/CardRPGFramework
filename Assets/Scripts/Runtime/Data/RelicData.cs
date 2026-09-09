using CardRPGFramework.Core.Relics;
using UnityEngine;

namespace CardRPGFramework.Data
{
    /// <summary>
    /// 遗物的 Inspector 配置资产：只有 Id 和显示名，行为全在 Core.Relics 的类里，由 RelicFactory 按 Id 落地。
    /// 显示名放这里而不放代码：与 CardData 一致，内容在 Inspector 里命名，Core 不含显示文本。
    /// </summary>
    [CreateAssetMenu(fileName = "RelicData", menuName = "CardRPG/Relic Data")]
    public sealed class RelicData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        public string Id => id;
        public string DisplayName => displayName;

        public RelicState ToState() => RelicFactory.Create(id);

        /// <summary>启动期配置校验：字段本身是否合法。跨资产的重复 Id 由 BattleConfig 统一检查。</summary>
        public bool TryValidate(out string error)
        {
            if (string.IsNullOrEmpty(id))
            {
                error = $"RelicData '{name}': id 不能为空";
                return false;
            }

            if (!RelicFactory.IsKnown(id))
            {
                error = $"RelicData '{name}': 未知的遗物 id '{id}'";
                return false;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                error = $"RelicData '{name}': displayName 不能为空";
                return false;
            }

            error = null;
            return true;
        }
    }
}
