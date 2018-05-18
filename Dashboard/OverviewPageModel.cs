using Hangfire.Annotations;

namespace Hangfire.Heartbeat.Dashboard
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class ServerView
    {
        public string ServerName { get; set; }
        public string ServerFullName { get; set; }
        public string ProcessId { get; set; }
        public string ProcessName { get; set; }
        public int CpuUsagePercentage { get; set; }
        public long WorkingMemorySet { get; set; }
    }
}
