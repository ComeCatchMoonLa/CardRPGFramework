namespace CardRPGFramework.Core.Buffs
{
    /// <summary>
    /// 四个 Buff Id 字面量的唯一出处。这批字符串是跨层契约：Data 资产里的 buffId、Views 的显示名表、
    /// Rule 的 HasBuff、0.5 起遗物钩子的按 Id 判定都对着同一个值；放常量是为了让"改一边忘了另一边"变成编译错误。
    /// 测试文件保留字面量不改，正好锁住常量值本身没被改动。
    /// </summary>
    public static class BuffIds
    {
        public const string Strength = "strength";
        public const string Poison = "poison";
        public const string Weak = "weak";
        public const string Vulnerable = "vulnerable";
    }
}
