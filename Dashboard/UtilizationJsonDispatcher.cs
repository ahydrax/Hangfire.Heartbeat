using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.Dashboard;
using Hangfire.Storage.Monitoring;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using static Hangfire.Heartbeat.Strings;

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
            var servers = context.Storage.GetMonitoringApi().Servers().Select(x => MapServerStatsToView(context, x)).ToArray();

            context.Response.ContentType = "application/json";
            var serialized = JsonConvert.SerializeObject(servers, JsonSerializerSettings);
            await context.Response.WriteAsync(serialized);
        }

        private static ServerView MapServerStatsToView(DashboardContext context, ServerDto x)
        {
            var stats = context.Storage.GetConnection().GetAllEntriesFromHash(Utils.FormatKey(x.Name));

            var view = new ServerView
            {
                ServerName = FormatServerName(x.Name),
                ServerFullName = x.Name,
                ProcessId = stats?[ProcessId],
                ProcessName = stats?[ProcessName],
                CpuUsagePercentage = ParseInt(stats?[CpuUsage]),
                WorkingMemorySet = ParseLong(stats?[WorkingSet])
            };

            return view;
        }

        private static string FormatServerName(string name)
        {
            var lastIndex = name.LastIndexOf(':');
            return name.Substring(0, lastIndex);
        }

        private static int ParseInt(string s)
        {
            int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i);
            return i;
        }

        private static long ParseLong(string s)
        {
            long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i);
            return i;
        }
    }
}
