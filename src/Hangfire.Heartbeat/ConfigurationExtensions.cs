using System;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Heartbeat.Dashboard;

namespace Hangfire.Heartbeat
{
    public static class ConfigurationExtensions
    {
        [PublicAPI]
        public static IGlobalConfiguration UseHeartbeatPage(this IGlobalConfiguration config, TimeSpan checkInterval)
            => UseHeartbeatPage(config, new HeartbeatDashboardOptions(checkInterval));

        [PublicAPI]
        public static IGlobalConfiguration UseHeartbeatPage(this IGlobalConfiguration config, HeartbeatDashboardOptions heartbeatDashboardOptions)
        {
            DashboardRoutes.Routes.AddRazorPage(OverviewPage.PageRoute, x => new OverviewPage(heartbeatDashboardOptions));
            NavigationMenu.Items.Add(page => new MenuItem(OverviewPage.Title, page.Url.To(OverviewPage.PageRoute))
            {
                Active = page.RequestPath.StartsWith(OverviewPage.PageRoute)
            });
            DashboardRoutes.Routes.Add(OverviewPage.StatsRoute, new UtilizationJsonDispatcher());

            DashboardRoutes.Routes.Add(
                "/heartbeat/knockout.js",
                new ContentDispatcher("application/javascript", "Hangfire.Heartbeat.Dashboard.js.knockout-3.4.2.js",
                    TimeSpan.FromDays(30)));

            DashboardRoutes.Routes.Add(
                "/heartbeat/knockoutorderable.js",
                new ContentDispatcher("application/javascript", "Hangfire.Heartbeat.Dashboard.js.knockout.bindings.orderable.js",
                    TimeSpan.FromDays(30)));

            DashboardRoutes.Routes.Add(
                "/heartbeat/numeral.js",
                new ContentDispatcher("application/javascript", "Hangfire.Heartbeat.Dashboard.js.numeral.min.js", TimeSpan.FromDays(30)));

            DashboardRoutes.Routes.Add(
                "/heartbeat/page.js",
                new ContentDispatcher("application/javascript", "Hangfire.Heartbeat.Dashboard.js.OverviewPage.js", TimeSpan.FromSeconds(1)));

            DashboardRoutes.Routes.Add(
                "/heartbeat/styles.css",
                new ContentDispatcher("text/css", "Hangfire.Heartbeat.Dashboard.css.styles.css", TimeSpan.FromSeconds(1)));

            return config;
        }
    }
}
