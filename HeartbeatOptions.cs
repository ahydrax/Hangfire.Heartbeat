using System;

namespace Hangfire.Heartbeat
{
    public sealed class HeartbeatOptions
    {
        public bool ShowServerFullNameInDetails { get; set; }
        public TimeSpan CheckInterval { get; }

        public HeartbeatOptions(TimeSpan checkInterval)
        {
            if (checkInterval == TimeSpan.Zero) throw new ArgumentException("Check interval must be nonzero value.", nameof(checkInterval));
            if (checkInterval != checkInterval.Duration()) throw new ArgumentException("Check interval must be positive value.", nameof(checkInterval));

            CheckInterval = checkInterval;
        }
    }
}
