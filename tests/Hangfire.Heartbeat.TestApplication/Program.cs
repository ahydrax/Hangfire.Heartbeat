﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Hangfire.Heartbeat.TestApplication
{
    public static class Program
    {
        public static void Main(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel()
                .Build()
                .Run();
    }
}
