using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Heartbeat.Dashboard;

namespace Hangfire.Heartbeat
{
    public static class ConfigurationExtensions
    {
        [PublicAPI]
        public static IGlobalConfiguration UseHeartbeatPage(this IGlobalConfiguration config)
        {
            DashboardRoutes.Routes.AddRazorPage(OverviewPage.UrlRoute, x => new OverviewPage());
            NavigationMenu.Items.Add(page => new MenuItem(OverviewPage.Title, page.Url.To(OverviewPage.UrlRoute))
            {
                Active = page.RequestPath.StartsWith(OverviewPage.UrlRoute)
            });
            return config;
        }
    }
}
