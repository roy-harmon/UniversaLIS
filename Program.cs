using UniversaLIS;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

using IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
         options.ServiceName = "UniversaLIS";
    })
    .ConfigureServices(services =>
    {
         LoggerProviderOptions.RegisterProviderOptions<
             EventLogSettings, EventLogLoggerProvider>(services);

         services.Add<UniversaLIService>();
         services.AddHostedService<WindowsBackgroundService>();
    })
    .ConfigureLogging((context, logging) =>
    {
         // See: https://github.com/dotnet/runtime/issues/47303
         logging.AddConfiguration(
             context.Configuration.GetSection("Logging"));
    })
    .Build();

await host.RunAsync();

/*
namespace UniversaLIS
{
     static class Program
     {
          /// <summary>
          /// The main entry point for the application.
          /// </summary>
          static void Main(string[] args)
          {
               if (System.Environment.UserInteractive)
               {    // Execute the program as a console app for debugging purposes.
                    UniversaLIService service1 = new UniversaLIService();
                    service1.DebuggingRoutine(args);
               }
               else
               {
                    // Run the service normally.  
                    ServiceBase[] ServicesToRun = new ServiceBase[]
                    {
                     new UniversaLIService()
                    };
                    ServiceBase.Run(ServicesToRun);
               }
          }
     }*/
}
