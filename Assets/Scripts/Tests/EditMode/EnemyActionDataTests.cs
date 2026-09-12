using System.Collections.Generic;
using CardRPGFramework.Core.Cards;
using CardRPGFramework.Data;
using NUnit.Framework;

namespace CardRPGFramework.Tests
{
    /// <summary>
    /// 只测敌人行动特有的两条拒绝（空效果、Draw）与 0.7 放开的"给玩家上 Buff"；
    /// 单条效果的 kind / target / value 校验已由 EffectSpecDataTests 覆盖，这里只确认内层错误会带下标转发出来。
    /// </summary>
    public class EnemyActionDataTests
    {
        private static EffectSpecData Damage(int value) => new(EffectKind.Damage, EffectTarget.Opponent, value, null);
        private static EffectSpecData Block(int value) => new(EffectKind.Block, EffectTarget.Self, value, null);

        private static EnemyActionData Action(params EffectSpecData[] effects) =>
            new("测试行动", new List<EffectSpecData>(effects));

        [Test]
        public void TryValidate_DamageAndBlock_Passes()
        {
            Assert.IsTrue(Action(Damage(7), Block(5)).TryValidate(out var error));
            Assert.IsNull(error);
        }

        [Test]
        public void TryValidate_ApplyBuffOnSelf_Passes()
        {
            var action = Action(new EffectSpecData(EffectKind.ApplyBuff, EffectTarget.Self, 3, "strength"), Block(6));

            Assert.IsTrue(action.TryValidate(out _));
        }

        [Test]
        public void TryValidate_EmptyEffects_ReturnsError()
        {
            // Core 允许空行动（待机），Data 在 0.6 拒绝：漏填效果的行要在启动时报出来。配沉睡类敌人时这条改为通过。
            Assert.IsFalse(Action().TryValidate(out var error));
            StringAssert.Contains("effects", error);
            // 代码里 new 出来的条目列表可能是 null；Inspector 序列化出来的永远是空列表。两者同样拒绝。
            Assert.IsFalse(new EnemyActionData("咬", null).TryValidate(out _));
        }

        [Test]
        public void TryValidate_Draw_ReturnsError()
        {
            var action = Action(Damage(7), new EffectSpecData(EffectKind.Draw, EffectTarget.Self, 1, null));

            Assert.IsFalse(action.TryValidate(out var error));
            StringAssert.Contains("effects[1]", error);
            StringAssert.Contains("Draw", error);
        }

        [Test]
        public void TryValidate_ApplyBuffOnOpponent_Passes()
        {
            // 0.6 拒绝这一条（虚弱没有减层会永远挂着）；0.7 有了轮末减层与刚施加保护，耙（7 伤 + 给玩家 1 层虚弱）只是配数据。
            var action = Action(Damage(7), new EffectSpecData(EffectKind.ApplyBuff, EffectTarget.Opponent, 1, "weak"));

            Assert.IsTrue(action.TryValidate(out var error));
            Assert.IsNull(error);
        }

        [Test]
        public void TryValidate_InvalidEffect_ForwardsInnerErrorWithIndex()
        {
            var action = Action(Damage(7), Damage(0));

            Assert.IsFalse(action.TryValidate(out var error));
            StringAssert.Contains("effects[1]", error);
            StringAssert.Contains("value", error);
        }

        [Test]
        public void ToAction_KeepsEffectOrderAndValues()
        {
            var action = Action(Damage(7), Block(5)).ToAction();

            Assert.AreEqual(2, action.Effects.Count);
            Assert.AreEqual(EffectKind.Damage, action.Effects[0].Kind);
            Assert.AreEqual(7, action.Effects[0].Value);
            Assert.AreEqual(EffectKind.Block, action.Effects[1].Kind);
            Assert.AreEqual(5, action.Effects[1].Value);
        }
    }
}
