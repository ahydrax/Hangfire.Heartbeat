using Hangfire.Dashboard;
using Hangfire.Dashboard.Pages;

namespace Hangfire.Heartbeat.Dashboard
{
    internal class OverviewPage : RazorPage
    {
        public const string Title = "Heartbeat";
        public const string PageRoute = "/heartbeat";
        public const string StatsRoute = "/heartbeat/stats";

        private static readonly string PageHtml;

        static OverviewPage()
        {
            PageHtml = Utils.ReadStringResource("Hangfire.Heartbeat.Dashboard.html.OverviewPage.html");
        }

        public override void Execute()
        {
            WriteEmptyLine();

            Layout = new LayoutPage(Title);

            WriteLiteralLine(PageHtml);

            WriteEmptyLine();
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
