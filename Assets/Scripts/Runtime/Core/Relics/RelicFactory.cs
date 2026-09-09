using System;

namespace CardRPGFramework.Core.Relics
{
    /// <summary>
    /// 遗物 Id → RelicState 的唯一转换点，供 RelicData（只带字符串 Id）落地成实例。
    /// 与 BuffFactory 同理用 switch 穷举：遗物是代码里的封闭集合，新遗物必然要写类，顺手加一行。IsKnown 与 Create 两处 Id 列表必须同步改。
    /// </summary>
    public static class RelicFactory
    {
        public static bool IsKnown(string id) => id is RelicIds.Vajra or RelicIds.SneckoSkull or RelicIds.PaperKrane;

        public static RelicState Create(string id) => id switch
        {
            RelicIds.Vajra => new VajraRelic(),
            RelicIds.SneckoSkull => new SneckoSkullRelic(),
            RelicIds.PaperKrane => new PaperKraneRelic(),
            _ => throw new ArgumentException($"未知的遗物 Id: {id}", nameof(id)),
        };
    }
}
