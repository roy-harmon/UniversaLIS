using System.ServiceProcess;

namespace IMMULIS
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
                    IMMULIService service1 = new IMMULIService();
                    service1.DebuggingRoutine(args);
               }
               else
               {
                    // Run the service normally.  
                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[]
                    {
                     new IMMULIService()
                    };
                    ServiceBase.Run(ServicesToRun);
               }
          }
     }
}
