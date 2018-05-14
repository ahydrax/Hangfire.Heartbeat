using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Server;

namespace Hangfire.Heartbeat.Server
{
    [UsedImplicitly]
    public sealed class SystemMonitor : IBackgroundProcess
    {
        public const string Allocated = "Allocated";
        public const string CpuUsage = "CpuUsage";

        private readonly Process _currentProcess;

        public SystemMonitor()
        {
            _currentProcess = Process.GetCurrentProcess();
        }

        public void Execute(BackgroundProcessContext context)
        {
            var connection = context.Storage.GetConnection();

            var processorCount = Environment.ProcessorCount;

            var previous = _currentProcess.TotalProcessorTime;
            Thread.Sleep(1000);

            _currentProcess.Refresh();
            var allocated = _currentProcess.WorkingSet64;

            var current = _currentProcess.TotalProcessorTime;
            var ratio = (current - previous).TotalMilliseconds / (processorCount * 10.0);

            var cpuUsageString = FormatCpuUsage(ratio);
            var allocatedString = FormatAllocated(allocated);

            connection.SetRangeInHash(context.ServerId, new[]
            {
                new KeyValuePair<string, string>(Allocated, allocatedString),
                new KeyValuePair<string, string>(CpuUsage, cpuUsageString)
            });

            context.CancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
        }

        private static string FormatCpuUsage(double ratio) => $"{ratio:0.00}%";

        private static readonly string[] Sizes = { "B", "KB", "MB", "GB", "TB" };
        private string FormatAllocated(long allocated)
        {
            double size = allocated;
            int order = 0;
            while (size >= 1024 && order < Sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            return $"{size:0.00} {Sizes[order]}";
        }
    }
}
