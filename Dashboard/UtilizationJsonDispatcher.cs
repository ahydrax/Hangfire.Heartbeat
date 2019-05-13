using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Hangfire.Dashboard;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using static Hangfire.Heartbeat.Model.ProcessInformationConstants;

namespace Hangfire.Heartbeat.Dashboard
{
    internal sealed class UtilizationJsonDispatcher : IDashboardDispatcher
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new JsonConverter[] { new StringEnumConverter { CamelCaseText = true } }
        };

        public async Task Dispatch(DashboardContext context)
        {
            var servers = context.Storage.GetMonitoringApi().Servers();
            var serverUtilizationViews = new List<ServerView>(servers.Count);
            using (var connection = context.Storage.GetConnection())
            {
                foreach (var serverDto in servers)
                {
                    var key = Utils.FormatKey(serverDto.Name);
                    var hash = connection.GetAllEntriesFromHash(key);

                    if (hash == null) continue;
                    if (hash.Count < 5) continue;

                    var view = new ServerView
                    {
                        DisplayName = FormatServerName(serverDto.Name),
                        Name = serverDto.Name,
                        ProcessId = hash[ProcessId],
                        ProcessName = hash[ProcessName],
                        CpuUsagePercentage = ParseDouble(hash[CpuUsage]),
                        WorkingMemorySet = ParseLong(hash[WorkingSet]),
                        Timestamp = ParseLong(hash[Timestamp])
                    };
                    serverUtilizationViews.Add(view);
                }
            }

            context.Response.ContentType = "application/json";
            var serialized = JsonConvert.SerializeObject(serverUtilizationViews, JsonSerializerSettings);
            await context.Response.WriteAsync(serialized);
        }

        private static string FormatServerName(string name)
        {
            var lastIndex = name.Length - 1;
            var occurrences = 0;

            for (var i = name.Length - 1; i > 0; i--)
            {
                if (name[i] == ':') occurrences++;
                if (occurrences == 2)
                {
                    lastIndex = i;
                    break;
                }
            }

            return lastIndex > 0 ? name.Substring(0, lastIndex) : name;
        }

        private static double ParseDouble(string s)
        {
            double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d);
            return d;
        }

        private static long ParseLong(string s)
        {
            long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i);
            return i;
        }
    }
}
