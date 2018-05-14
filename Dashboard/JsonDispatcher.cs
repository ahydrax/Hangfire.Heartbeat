using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.Dashboard;
using Hangfire.Heartbeat.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Hangfire.Heartbeat.Dashboard
{
    public class JsonDispatcher : IDashboardDispatcher
    {
        public async Task Dispatch(DashboardContext context)
        {
            var servers = context.Storage.GetMonitoringApi().Servers().Select(x =>
            {
                var stats = context.Storage.GetConnection().GetAllEntriesFromHash(x.Name);

                var cpuPercentageParsed = double.TryParse(stats?[SystemMonitor.CpuUsage], NumberStyles.Any, CultureInfo.InvariantCulture, out var cpuPercentage);
                var allocatedParsed = double.TryParse(stats?[SystemMonitor.Allocated], NumberStyles.Any, CultureInfo.InvariantCulture, out var allocated);

                return new ServerView
                {
                    Name = x.Name.Replace(':','-'),
                    CpuUsagePercentage = cpuPercentageParsed ? cpuPercentage : double.NaN,
                    WorkingMemorySet = allocatedParsed ? allocated : double.NaN
                };

            }).ToArray();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new JsonConverter[] { new StringEnumConverter { CamelCaseText = true } }
            };
            var serialized = JsonConvert.SerializeObject(servers, settings);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(serialized);
        }
    }
}
