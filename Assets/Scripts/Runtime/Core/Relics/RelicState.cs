namespace CardRPGFramework.Core.Relics
{
    /// <summary>
    /// 遗物只有 Id：没有层数、不衰减、不进 Buff 字典，整场常驻在持有者的遗物列表里。
    /// 它和 Buff 复用同一种钩子形状（固定时机被遍历、调虚方法），但容器不同——这是"遗物不是 Buff"的全部含义。
    /// 将来带计数的遗物（钢笔尖一类）在子类里自己加字段，基类不预留。
    /// </summary>
    public abstract class RelicState
    {
        public abstract string Id { get; }
    }
}
