using System;

namespace CardRPGFramework.Core.Battle
{
    /// <summary>
    /// BattleSession 初始化所需的标量战斗参数（不含牌组，牌组由构造函数单独传入）。
    /// </summary>
    public readonly struct BattleSetup
    {
        public int PlayerMaxHp { get; }
        public int EnemyMaxHp { get; }
        public int EnemyDamage { get; }
        public int EnergyPerTurn { get; }
        public int HandSize { get; }

        public BattleSetup(int playerMaxHp, int enemyMaxHp, int enemyDamage, int energyPerTurn, int handSize)
        {
            if (playerMaxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerMaxHp), "PlayerMaxHp 必须大于 0");
            }

            if (enemyMaxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyMaxHp), "EnemyMaxHp 必须大于 0");
            }

            if (enemyDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyDamage), "EnemyDamage 不能小于 0");
            }

            if (energyPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(energyPerTurn), "EnergyPerTurn 不能小于 0");
            }

            if (handSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(handSize), "HandSize 不能小于 0");
            }

            PlayerMaxHp = playerMaxHp;
            EnemyMaxHp = enemyMaxHp;
            EnemyDamage = enemyDamage;
            EnergyPerTurn = energyPerTurn;
            HandSize = handSize;
        }
    }
}
