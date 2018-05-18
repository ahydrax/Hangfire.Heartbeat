using System;
using System.Reflection;
using System.Threading.Tasks;
using Hangfire.Dashboard;

namespace Hangfire.Heartbeat.Dashboard
{
    internal class ContentDispatcher : IDashboardDispatcher
    {
        private static readonly Assembly ThisAssembly = typeof(ContentDispatcher).Assembly;
        private readonly string _resourceName;
        private readonly string _contentType;

        public ContentDispatcher(string contentType, string resourceName)
        {
            _resourceName = resourceName;
            _contentType = contentType;
        }

        public async Task Dispatch(DashboardContext context)
        {
            context.Response.ContentType = _contentType;
            context.Response.SetExpire(DateTimeOffset.Now.AddYears(1));

            await WriteResourceAsync(context);
        }

        private async Task WriteResourceAsync(DashboardContext context)
        {
            using (var stream = ThisAssembly.GetManifestResourceStream(_resourceName))
            {
                if (stream == null)
                {
                    context.Response.StatusCode = 404;
                }
                else
                {
                    await stream.CopyToAsync(context.Response.Body);
                }
            }
        }
    }
}
