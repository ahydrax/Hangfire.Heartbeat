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

            var html = Utils.ReadStringResource("Hangfire.Heartbeat.Dashboard.html.OverviewPage.html");

            WriteLiteralLine(html);

            WriteLiteralLine("<script language='javascript'>");
            var script = Utils.ReadStringResource("Hangfire.Heartbeat.Dashboard.js.numeral.min.js");
            WriteLiteralLine(script);
            WriteLiteralLine("</script>");

            WriteLiteralLine("<script language='javascript'>");
            script = Utils.ReadStringResource("Hangfire.Heartbeat.Dashboard.js.OverviewPage.js");
            WriteLiteralLine(script);
            WriteLiteralLine("</script>");
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
