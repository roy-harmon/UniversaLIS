using UniversaLIS;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;

using IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
         options.ServiceName = "UniversaLIS";
    })
    .ConfigureServices(services =>
    {
         if (OperatingSystem.IsWindows())
         {
              LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(services);
         }

         services.AddHostedService<UniversaLIService>();
    })
    .ConfigureLogging((context, logging) =>
    {
         // See: https://github.com/dotnet/runtime/issues/47303
         logging.AddConfiguration(
             context.Configuration.GetSection("Logging"));
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
