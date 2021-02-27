# Hangfire.Heartbeat
[![NuGet](https://img.shields.io/nuget/v/Hangfire.Heartbeat.svg)](https://www.nuget.org/packages/Hangfire.Heartbeat/)
[![Tests](https://github.com/ahydrax/Hangfire.Heartbeat/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/ahydrax/Hangfire.Heartbeat/actions/workflows/build-and-test.yml)

A process monitoring plugin for Hangfire. Multiple processes are supported.

![dashboard](content/dashboard.png)

Read about hangfire here: https://github.com/HangfireIO/Hangfire#hangfire-
and here: http://hangfire.io/

## Instructions
Install a package from Nuget.

Then add this in your code:

for service side:
```csharp
app.UseHangfireServer(additionalProcesses: new[] { new ProcessMonitor(checkInterval: TimeSpan.FromSeconds(1)) });
```

for dashboard:
```csharp
services.AddHangfire(configuration => configuration.UseHeartbeatPage(checkInterval: TimeSpan.FromSeconds(1)));
```
It's not recommended to specify `checkInterval` less than 1 second as it can cause additional load on db server. Also I recommend to use the same interval as for server and dashboard. 

## Credits
 * Victoria Popova
 * Maria Gretskih
 * [@pieceofsummer](https://github.com/pieceofsummer)

## License
Authored by: Viktor Svyatokha (ahydrax)

This project is under MIT license. You can obtain the license copy [here](https://github.com/ahydrax/Hangfire.Heartbeat/blob/master/LICENSE).
