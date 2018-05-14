using System.Collections.Generic;

namespace Hangfire.Heartbeat.Dashboard
{
    public class OverviewPageModel
    {
        public ServerView[] Servers { get; set; }
    }

    public class ServerView
    {
        public string Name { get; set; }
        public double CpuUsagePercentage { get; set; }
        public double WorkingMemorySet { get; set; }
    }
}
