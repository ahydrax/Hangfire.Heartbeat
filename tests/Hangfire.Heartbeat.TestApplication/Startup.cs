using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Dashboard;
using Hangfire.Heartbeat.Server;
using Hangfire.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.Heartbeat.TestApplication
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(configuration =>
                configuration
                    .UseRedisStorage("192.168.5.32:6379")
                    .UseHeartbeatPage(TimeSpan.FromMilliseconds(500)));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage();

            var random = new Random();
            var serverName = "Test server #" + random.Next(1, 1000);
            var randomCheckInterval = TimeSpan.FromMilliseconds(random.Next(300, 2000));
            var monitors = Process.GetProcessesByName("chrome")
                .Select(x => new ProcessMonitor(TimeSpan.FromMilliseconds(random.Next(300, 2000)), x))
                .Append(new ProcessMonitor(randomCheckInterval))
                .ToArray();

            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                ServerName = serverName,
                ServerCheckInterval = TimeSpan.FromSeconds(5),
                ServerTimeout = TimeSpan.FromSeconds(15),
                HeartbeatInterval = TimeSpan.FromSeconds(3),
                WorkerCount = 1
            }, monitors);

            app.UseHangfireDashboard("", new DashboardOptions { Authorization = new IDashboardAuthorizationFilter[0] });
            RecurringJob.AddOrUpdate(() => Alloc(), Cron.Daily(), TimeZoneInfo.Utc);
            RecurringJob.AddOrUpdate(() => CpuKill(75), Cron.Daily(), TimeZoneInfo.Utc);
            RecurringJob.AddOrUpdate(() => GC.Collect(2), Cron.Daily(), TimeZoneInfo.Utc);
            RecurringJob.AddOrUpdate(() => AggregateTest(), Cron.Daily(), TimeZoneInfo.Utc);
        }

        public void AggregateTest()
        {
            var client = new BackgroundJobClient(JobStorage.Current);

            var aggregate = client.CreateJobAggregate(builder =>
            {
                var job1 = builder.Enqueue(() => Wait(7000));
                var job2 = builder.Enqueue(() => Wait(4000));
                var job3 = builder.ContinueWith(() => Wait(4000), job2);
                var job4 = builder.Schedule(() => Wait(3000), DateTimeOffset.Now.AddSeconds(5));
                var job5 = builder.ContinueWith(() => Wait(3000), job4);
            });

            client.ContinueJobWith(aggregate, () => Done(1000));
        }

        struct DummyStruct
        {
            public int A;
            public int B;
            public int C;
        }

        public static void Alloc()
        {
            var w1 = Stopwatch.StartNew();
            var rnd = new Random();
            while (true)
            {
                var alloc = rnd.Next(100000, 10000000);
                var x = new DummyStruct[alloc];
                x[0].A = 1;
                x[0].B = 1;
                x[0].C = 1;
                x[x.Length - 1].A = 2;
                if (w1.Elapsed > TimeSpan.FromSeconds(15))
                {
                    break;
                }
            }
        }

        public static void CpuKill(int cpuUsage)
        {
            Parallel.For(0, 1, i =>
            {
                var w1 = Stopwatch.StartNew();
                var w2 = Stopwatch.StartNew();

                while (true)
                {
                    if (w2.ElapsedMilliseconds > cpuUsage)
                    {
                        Thread.Sleep(100 - cpuUsage);
                        w2.Reset();
                        w2.Start();
                    }

                    if (w1.Elapsed > TimeSpan.FromSeconds(15))
                    {
                        break;
                    }
                }
            });
        }

        public static void Wait(int milliseconds) => Thread.Sleep(milliseconds);

        public static string Done(int milliseconds) => "DONE";
    }
}
