using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Hangfire.Server;
using Hangfire.States;

namespace Hangfire.Heartbeat.TestApplication
{
    public class WaitingState : IState
    {
        public static readonly string StateName = "Waiting";

        public Dictionary<string, string> SerializeData() => new Dictionary<string, string>();

        public string Name => StateName;
        public string Reason => "Waiting for grains";
        public bool IsFinal => false;
        public bool IgnoreJobLoadException => false;
    }

    public static class JobAggregate
    {
        public static string CreateJobAggregate(this IBackgroundJobClient client, Action<IJobAggregateBuilder> action)
        {
            var jodId = client.Create(() => Aggregation(), new WaitingState());
            var builder = new JobAggregateBuilder(jodId, client, JobStorage.Current);

            action(builder);

            return jodId;
        }

        public static void Aggregation()
        {
        }
    }

    public interface IJobAggregateBuilder
    {
        string Enqueue(Expression<Action> action);
        string Schedule(Expression<Action> action, DateTimeOffset executionTime);
        string ContinueWith(Expression<Action> action, string parentId);
    }

    internal class JobAggregateBuilder : IJobAggregateBuilder
    {
        private readonly string _aggregateJobId;
        private readonly IBackgroundJobClient _client;
        private readonly JobStorage _jobStorage;
        private int _innerGrainCounter;
        private readonly string _lockId;

        public JobAggregateBuilder(string aggregateJobId, IBackgroundJobClient client, JobStorage jobStorage)
        {
            _aggregateJobId = aggregateJobId;
            _client = client;
            _jobStorage = jobStorage;
            _innerGrainCounter = 0;
            _lockId = "aggregate-job:" + _aggregateJobId;
        }

        public string Enqueue(Expression<Action> action)
        {
            var grainId = GetGrainId();
            AddGrainState(grainId);

            var jobId = _client.Enqueue(action);
            var endId = _client.ContinueJobWith(jobId, () => AggregationGrainEnd(_aggregateJobId, _lockId, grainId, null));

            return endId;
        }

        public string Schedule(Expression<Action> action, DateTimeOffset executionTime)
        {
            var grainId = GetGrainId();
            AddGrainState(grainId);

            var jobId = _client.Schedule(action, executionTime);
            var endId = _client.ContinueJobWith(jobId, () => AggregationGrainEnd(_aggregateJobId, _lockId, grainId, null));

            return endId;
        }

        public string ContinueWith(Expression<Action> action, string parentId)
        {
            var grainId = GetGrainId();
            AddGrainState(grainId);

            var jobId = _client.ContinueJobWith(parentId, action);
            var endId = _client.ContinueJobWith(jobId, () => AggregationGrainEnd(_aggregateJobId, _lockId, grainId, null));

            return endId;
        }

        private void AddGrainState(string grainId)
        {
            using (var connection = _jobStorage.GetConnection())
            {
                connection.SetRangeInHash(_lockId, new[] { new KeyValuePair<string, string>(grainId, "false") });
            }
        }

        public static void AggregationGrainEnd(string aggregateJobId, string lockId, string grainId, PerformContext context)
        {
            context.Connection.SetRangeInHash(lockId, new[] { new KeyValuePair<string, string>(grainId, "true") });

            var grainsData = context.Connection.GetAllEntriesFromHash(lockId);
            var shouldStart = grainsData.All(x => x.Value == "true");

            if (shouldStart)
            {
                var client = new BackgroundJobClient();
                client.ChangeState(aggregateJobId, new EnqueuedState());
            }
        }

        private string GetGrainId() => _innerGrainCounter++.ToString(CultureInfo.InvariantCulture);
    }
}
