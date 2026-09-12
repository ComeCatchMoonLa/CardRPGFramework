using System.Collections.Generic;
using CardRPGFramework.Core.Enemies;
using UnityEngine;

namespace CardRPGFramework.Data
{
    /// <summary>
    /// 敌人的 Inspector 配置资产，转换为 Core 可用的 EnemyDefinition。Core 不保留对本类的引用。
    /// 新增一只敌人 = 新建一份资产并在战斗配置里引用，不改代码。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "CardRPG/Enemy Data")]
    public sealed class EnemyData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private int maxHp;
        [SerializeField] private List<EnemyActionData> actions = new();

        public string Id => id;
        public string DisplayName => displayName;

        public EnemyDefinition ToDefinition()
        {
            var result = new EnemyAction[actions.Count];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = actions[i].ToAction();
            }

            return new EnemyDefinition(id, displayName, maxHp, result);
        }

        /// <summary>启动期配置校验：字段本身是否合法；行动逐条交给 EnemyActionData，错误信息带下标与行动名以便在 Inspector 里定位。</summary>
        public bool TryValidate(out string error)
        {
            if (string.IsNullOrEmpty(id))
            {
                error = $"EnemyData '{name}': id 不能为空";
                return false;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                error = $"EnemyData '{name}': displayName 不能为空";
                return false;
            }

            if (maxHp <= 0)
            {
                error = $"EnemyData '{name}': maxHp 必须大于 0";
                return false;
            }

            if (actions == null || actions.Count == 0)
            {
                error = $"EnemyData '{name}': actions 至少要有一条";
                return false;
            }

            for (var i = 0; i < actions.Count; i++)
            {
                if (!actions[i].TryValidate(out var actionError))
                {
                    error = $"EnemyData '{name}': actions[{i}] '{actions[i].Name}' {actionError}";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
