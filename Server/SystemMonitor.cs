using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Hangfire.Annotations;
using Hangfire.Server;
using Hangfire.Storage;
using static Hangfire.Heartbeat.Model.ProcessInformationConstants;

namespace Hangfire.Heartbeat.Server
{
    [PublicAPI]
    public sealed class SystemMonitor : IBackgroundProcess
    {
        private readonly Process _currentProcess;
        private readonly TimeSpan _checkInterval;
        private readonly int _processorCount;
        private readonly TimeSpan _expireIn;
        private (TimeSpan? current, TimeSpan? next) _processorTimeUsage;

        public SystemMonitor(TimeSpan checkInterval)
        {
            if (checkInterval == TimeSpan.Zero) throw new ArgumentException("Check interval must be nonzero value.", nameof(checkInterval));
            if (checkInterval != checkInterval.Duration()) throw new ArgumentException("Check interval must be positive value.", nameof(checkInterval));
            _checkInterval = checkInterval;

            _currentProcess = Process.GetCurrentProcess();
            _expireIn = _checkInterval + TimeSpan.FromMinutes(1);
            _processorCount = Environment.ProcessorCount;
            _processorTimeUsage = default;
        }

        public void Execute(BackgroundProcessContext context)
        {
            if (context.IsStopping)
            {
                CleanupState(context);
                return;
            }

            if (_processorTimeUsage.current.HasValue && _processorTimeUsage.next.HasValue)
            {
                var cpuPercentUsage = ComputeCpuUsage(_processorTimeUsage.current.Value, _processorTimeUsage.next.Value);

                WriteState(context, cpuPercentUsage);
            }

            context.Wait(_checkInterval);
            _currentProcess.Refresh();

            var next = _currentProcess.TotalProcessorTime;
            _processorTimeUsage = (_processorTimeUsage.next, next);
        }

        private void WriteState(BackgroundProcessContext context, double cpuPercentUsage)
        {
            using (var connection = context.Storage.GetConnection())
            using (var writeTransaction = connection.CreateWriteTransaction())
            {
                var key = Utils.FormatKey(context.ServerId);

                var values = new Dictionary<string, string>
                {
                    [ProcessId] = _currentProcess.Id.ToString(CultureInfo.InvariantCulture),
                    [ProcessName] = _currentProcess.ProcessName,
                    [CpuUsage] = cpuPercentUsage.ToString(CultureInfo.InvariantCulture),
                    [WorkingSet] = _currentProcess.WorkingSet64.ToString(CultureInfo.InvariantCulture),
                    [Timestamp] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
                };

                writeTransaction.SetRangeInHash(key, values);

                // if storage supports manual expiration handling
                if (writeTransaction is JobStorageTransaction jsTransaction)
                {
                    jsTransaction.ExpireHash(key, _expireIn);
                }

                writeTransaction.Commit();
            }
        }

        private double ComputeCpuUsage(TimeSpan current, TimeSpan next)
        {
            var totalMilliseconds = (next - current).TotalMilliseconds;
            var totalCpuPercentUsage = (totalMilliseconds / _checkInterval.TotalMilliseconds) * 100;
            var cpuPercentUsage = totalCpuPercentUsage / _processorCount;
            return Math.Round(cpuPercentUsage, 1);
        }

        private static void CleanupState(BackgroundProcessContext context)
        {
            using (var connection = context.Storage.GetConnection())
            using (var transaction = connection.CreateWriteTransaction())
            {
                var key = Utils.FormatKey(context.ServerId);
                transaction.RemoveHash(key);
                transaction.Commit();
            }
        }
    }
}
