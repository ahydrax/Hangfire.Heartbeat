using System;
using Hangfire.Dashboard;
using Hangfire.Dashboard.Pages;
using static Hangfire.Heartbeat.Constants;

namespace Hangfire.Heartbeat.Dashboard
{
    internal class OverviewPage : RazorPage
    {
        public const string Title = "Heartbeat";
        public const string PageRoute = "/heartbeat";
        public const string StatsRoute = "/heartbeat/stats";

        private static readonly string PageHtml;

        private readonly string _config;

        static OverviewPage()
        {
            PageHtml = Utils.ReadStringResource("Hangfire.Heartbeat.Dashboard.html.OverviewPage.html");
        }

        public OverviewPage(TimeSpan checkInterval)
        {
            _config = $"<div id=\"heartbeatConfig\" data-pollinterval=\"{checkInterval.TotalMilliseconds + WaitMilliseconds}\" data-pollurl=\"{StatsRoute}\"></div>";
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
            WriteLiteral(_config);
            WriteLiteral("\r\n");
        }

        private void WriteEmptyLine()
        {
            WriteLiteral("\r\n");
        }
    }
}
