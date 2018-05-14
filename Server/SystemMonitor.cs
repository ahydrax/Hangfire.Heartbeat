using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Server;
using Hangfire.Storage;

namespace Hangfire.Heartbeat.Server
{
    [UsedImplicitly]
    public sealed class SystemMonitor : IBackgroundProcess
    {
        public const string Allocated = "Allocated";
        public const string CpuUsage = "CpuUsage";

        private readonly Process _currentProcess;
        private readonly TimeSpan _checkInterval;
        private readonly int _processorCount;

        public SystemMonitor()
        {
            _currentProcess = Process.GetCurrentProcess();
            _checkInterval = TimeSpan.FromSeconds(3);
            _processorCount = Environment.ProcessorCount;
        }

        public void Execute(BackgroundProcessContext context)
        {
            var connection = context.Storage.GetConnection();
            if (context.CancellationToken.IsCancellationRequested)
            {
                CleanupState(context, connection);
            }

            var previousProcessorTime = _currentProcess.TotalProcessorTime;
            Thread.Sleep(1000);
            _currentProcess.Refresh();
            var currentProcessorTime = _currentProcess.TotalProcessorTime;

            var allocatedBytes = _currentProcess.WorkingSet64;

            var cpuPercentUsage = CalculateCpuPercentUsage(currentProcessorTime, previousProcessorTime);

            var cpuUsageString = cpuPercentUsage.ToString(CultureInfo.InvariantCulture);
            var allocatedString = allocatedBytes.ToString(CultureInfo.InvariantCulture);

            connection.SetRangeInHash(context.ServerId, new[]
            {
                new KeyValuePair<string, string>(Allocated, allocatedString),
                new KeyValuePair<string, string>(CpuUsage, cpuUsageString)
            });

            context.Wait(_checkInterval);
        }

        private double CalculateCpuPercentUsage(TimeSpan currentProcessorTime, TimeSpan previousProcessorTime)
            => (currentProcessorTime - previousProcessorTime).TotalMilliseconds / (_processorCount * 10.0);

        private static void CleanupState(BackgroundProcessContext context, IStorageConnection connection)
        {
            using (var transaction = connection.CreateWriteTransaction())
            {
                transaction.RemoveHash(context.ServerId);
                transaction.Commit();
            }
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
