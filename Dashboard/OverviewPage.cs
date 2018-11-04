using System;
using Hangfire.Dashboard;
using Hangfire.Dashboard.Pages;

namespace Hangfire.Heartbeat.Dashboard
{
    internal class OverviewPage : RazorPage
    {
        private readonly HeartbeatOptions _options;
        public const string Title = "Heartbeat";
        public const string PageRoute = "/heartbeat";
        public const string StatsRoute = "/heartbeat/stats";

        private static readonly string PageHtml;
        
        static OverviewPage()
        {
            PageHtml = Utils.ReadStringResource("Hangfire.Heartbeat.Dashboard.html.OverviewPage.html");
        }

        public OverviewPage(HeartbeatOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
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
            WriteConfig();
            WriteLiteral("\r\n");
        }

        private void WriteConfig()
        {
            WriteLiteral($@"<div id='heartbeatConfig' 
data-pollinterval='{(int)_options.CheckInterval.TotalMilliseconds}'
data-pollurl='{Url.To(StatsRoute)}' 
data-showfullname='{_options.ShowServerFullNameInDetails.ToString().ToLowerInvariant()}'></div>");
        }

        private void WriteEmptyLine()
        {
            WriteLiteral("\r\n");
        }
    }
}
