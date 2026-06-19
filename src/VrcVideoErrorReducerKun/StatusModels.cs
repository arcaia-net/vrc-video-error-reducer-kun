namespace VrcVideoErrorReducerKun
{
    public enum FirewallStatusKind
    {
        Unknown,
        Configured,
        NotConfigured,
        ComponentMissing,
        Incomplete,
        Error
    }

    public sealed class FirewallStatus
    {
        public FirewallStatusKind Kind { get; set; }
        public string DisplayText { get; set; }
        public string Message { get; set; }
        public string TargetPath { get; set; }
        public bool ComponentExists { get; set; }
        public bool ManagedRuleExists { get; set; }
        public bool CanEnable { get; set; }
        public bool CanDisable { get; set; }
        public string Details { get; set; }
    }
}
