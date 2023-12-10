using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace UniversaLIS
{
     public class Program
     {
          public static void Main(string[] args)
          {
               if (Environment.UserInteractive)
               {    
                    // Execute the program as a console app for debugging purposes.
                    IHost host = Host.CreateDefaultBuilder(args).Build();
                    UniversaLIService service1 = new UniversaLIService(host.Services.GetRequiredService<ILogger<UniversaLIService>>());
                    service1.DebuggingRoutine();
               }
               else
               {
                    // Run the service normally.  
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
                        logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                   })
                   .Build();
                    host.Run();
               }
          }
     }
}
