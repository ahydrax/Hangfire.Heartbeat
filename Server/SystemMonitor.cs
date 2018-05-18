using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Server;
using Hangfire.Storage;
using static Hangfire.Heartbeat.Strings;

namespace Hangfire.Heartbeat.Server
{
    [UsedImplicitly]
    public sealed class SystemMonitor : IBackgroundProcess
    {
        private readonly Process _currentProcess;
        private readonly TimeSpan _checkInterval;
        private readonly int _processorCount;

        public SystemMonitor()
        {
            _currentProcess = Process.GetCurrentProcess();
            _checkInterval = TimeSpan.FromSeconds(2);
            _processorCount = Environment.ProcessorCount;
        }

        public void Execute(BackgroundProcessContext context)
        {
            var connection = context.Storage.GetConnection();
            if (context.IsShutdownRequested)
            {
                CleanupState(context, connection);
            }

            var cpuPercentUsage = ComputeCpuUsage();

            using (var writeTransaction = connection.CreateWriteTransaction())
            {
                var key = Utils.FormatKey(context.ServerId);

                var values = new Dictionary<string, string>
                {
                    [ProcessId] = _currentProcess.Id.ToString(CultureInfo.InvariantCulture),
                    [ProcessName] = _currentProcess.ProcessName,
                    [CpuUsage] = cpuPercentUsage.ToString(CultureInfo.InvariantCulture),
                    [WorkingSet] = _currentProcess.WorkingSet64.ToString(CultureInfo.InvariantCulture)
                };

                writeTransaction.SetRangeInHash(key, values);

                // if storage supports manual expiration handling
                if (writeTransaction is JobStorageTransaction jsTransaction)
                {
                    jsTransaction.ExpireHash(key, TimeSpan.FromMinutes(5));
                }

                writeTransaction.Commit();
            }

            context.Wait(_checkInterval);
        }

        private int ComputeCpuUsage()
        {
            var current = _currentProcess.TotalProcessorTime;
            Thread.Sleep(1000);
            _currentProcess.Refresh();
            var next = _currentProcess.TotalProcessorTime;

            var totalMilliseconds = (int)(next - current).TotalMilliseconds;
            var cpuPercentUsage = totalMilliseconds / (_processorCount * 10);
            return cpuPercentUsage;
        }

        private static void CleanupState(BackgroundProcessContext context, IStorageConnection connection)
        {
            using (var transaction = connection.CreateWriteTransaction())
            {
                transaction.RemoveHash(context.ServerId);
                transaction.Commit();
            }
        }
    }
}
