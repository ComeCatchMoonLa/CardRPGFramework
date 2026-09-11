using System;

namespace CardRPGFramework.Core.Battle
{
    /// <summary>
    /// BattleSession 初始化所需的玩家侧标量参数。敌人由 EnemyDefinition 整体描述，和牌组、遗物一样是构造函数的单独参数，不放在这里。
    /// </summary>
    public readonly struct BattleSetup
    {
        public int PlayerMaxHp { get; }
        public int EnergyPerTurn { get; }
        public int HandSize { get; }

        public BattleSetup(int playerMaxHp, int energyPerTurn, int handSize)
        {
            if (playerMaxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerMaxHp), "PlayerMaxHp 必须大于 0");
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
            EnergyPerTurn = energyPerTurn;
            HandSize = handSize;
        }
    }
}
