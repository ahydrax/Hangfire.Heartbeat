# Hangfire.Heartbeat
[![NuGet](https://img.shields.io/nuget/v/Hangfire.Heartbeat.svg)](https://www.nuget.org/packages/Hangfire.Heartbeat/)
[![Build status](https://ci.appveyor.com/api/projects/status/0w31nywe7fs5s2hl?svg=true)](https://ci.appveyor.com/project/ahydrax/hangfire-heartbeat)

![dashboard](content/dashboard.png)

A simple server monitoring plugin for Hangfire.
Read about hangfire here: https://github.com/HangfireIO/Hangfire#hangfire-
and here: http://hangfire.io/

# Instructions
Install a package from Nuget.

Then add this in your code:

for service side:
```csharp
app.UseHangfireServer(additionalProcesses: new[] { new SystemMonitor() });
```

for dashboard:
```csharp
services.AddHangfire(configuration => configuration.UseHeartbeatPage());
```

# License
Authored by: Viktor Svyatokha (ahydrax)
This project is under MIT license. You can obtain the license copy [here](https://github.com/ahydrax/Hangfire.Heartbeat/blob/master/LICENSE).