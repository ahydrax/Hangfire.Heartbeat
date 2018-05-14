using Hangfire.Dashboard;
using Hangfire.Dashboard.Pages;
using Hangfire.Heartbeat.Server;

namespace Hangfire.Heartbeat.Dashboard
{
    internal class OverviewPage : RazorPage
    {
        public const string Title = "Heartbeat";
        public const string UrlRoute = "/heartbeat";

        public override void Execute()
        {
            WriteEmptyLine();
            Layout = new LayoutPage(Title);

            var servers = Storage.GetMonitoringApi().Servers();

            WriteLiteralLine("<table class='table'>");
            WriteLiteralLine("<thead>");
            WriteLiteralLine("<tr>");
            WriteLiteralLine("<th>Name<th/>");
            WriteLiteralLine("<th>CPU<th/>");
            WriteLiteralLine("<th>Working memory set<th/>");
            WriteLiteralLine("<tr/>");
            WriteLiteralLine("<thead/>");

            WriteLiteralLine("<tbody>");
            foreach (var server in servers)
            {
                try
                {
                    var keys = Storage.GetConnection().GetAllEntriesFromHash(server.Name);

                    WriteLiteral("<tr>");
                    WriteLiteral($"<td>{server.Name}<td/>");
                    WriteLiteral($"<td>{keys?[SystemMonitor.CpuUsage] ?? "N/A"}<td/>");
                    WriteLiteral($"<td>{keys?[SystemMonitor.Allocated] ?? "N/A"}<td/>");
                    WriteLiteralLine("<tr/>");
                }
                catch
                {
                }
                
            }
            WriteLiteralLine("<tbody/>");
            WriteLiteralLine("<table />");

        }

        private void WriteLiteralLine(string textToAppend)
        {
            WriteLiteral(textToAppend);
            WriteLiteral("\r\n");
        }

        private void WriteEmptyLine()
        {
            WriteLiteral("\r\n");
        }
    }
}
