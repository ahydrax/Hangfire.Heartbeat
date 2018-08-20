using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Hangfire.Dashboard;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using static Hangfire.Heartbeat.Constants;

namespace Hangfire.Heartbeat.Dashboard
{
    public class UtilizationJsonDispatcher : IDashboardDispatcher
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
                    var view = new ServerView
                    {
                        ServerName = FormatServerName(serverDto.Name),
                        ServerFullName = serverDto.Name,
                        ProcessId = hash?[ProcessId],
                        ProcessName = hash?[ProcessName],
                        CpuUsagePercentage = ParseInt(hash?[CpuUsage]),
                        WorkingMemorySet = ParseLong(hash?[WorkingSet]),
                        Timestamp = ParseLong(hash?[Timestamp])
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
            var lastIndex = name.LastIndexOf(':');
            return lastIndex > 0 ? name.Substring(0, lastIndex) : name;
        }

        private static int ParseInt(string s)
        {
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i);
            return i;
        }

        private static long ParseLong(string s)
        {
            long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i);
            return i;
        }
    }
}
