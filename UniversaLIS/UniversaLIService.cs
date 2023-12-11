using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace UniversaLIS
{
     public partial class UniversaLIService : BackgroundService
     {
          public readonly ILogger<UniversaLIService> EventLogger;
          private static readonly List<CommFacilitator> s_commFacilitators = new();
          private readonly Task _completedTask = Task.CompletedTask;
          private static readonly YamlSettings yamlSettings = GetSettings();
          private static readonly HttpClient client = new();

          public HttpResponseMessage SendRestLisRequest(HttpMethod method, string relativeUri, object body)
          {
               HttpRequestMessage message = new HttpRequestMessage(method, relativeUri);
               message.Content = JsonContent.Create(body);
               return client.Send(message);
          }

          public YamlSettings GetYamlSettings()
          {
               return yamlSettings;
          }

          public UniversaLIService(ILogger<UniversaLIService> logger)
          {
               EventLogger = logger;
          }

          private static YamlSettings GetSettings()
          {
               string configPath;
               if (Environment.UserInteractive)
               {
                    configPath = Path.Combine(Directory.GetCurrentDirectory(), "\\config.yml");
               }
               else
               {
                    configPath = Environment.ExpandEnvironmentVariables("%ProgramW6432%\\UniversaLIS\\config.yml");
               }
               using var reader = new StreamReader(configPath);
               var yamlText = reader.ReadToEnd();
               var deserializer = new DeserializerBuilder()
                    .Build();
               return deserializer.Deserialize<YamlSettings>(yamlText) ?? new YamlSettings();
          }

          public void HandleEx(Exception ex)
          {
               if (ex is null)
               {
                    return;
               }
               string? message = ex.Source + " - Error: " + ex.Message + "\n" + ex.TargetSite + "\n" + ex.StackTrace;
               EventLogger.LogError(message: message);
          }

          protected override Task ExecuteAsync(CancellationToken stoppingToken)
          {
               return Task.Delay(Timeout.Infinite, stoppingToken);
          }

          protected void OnStart()
          {
               try
               {
                    AppendToLog("Starting service; reading config.yml and opening ports.");
                    foreach (var serialPort in GetYamlSettings()?.Interfaces?.Serial ?? Enumerable.Empty<Serial>())
                    {
                         s_commFacilitators.Add(new CommFacilitator(serialPort, this));
                    }
                    foreach (var tcpPort in GetYamlSettings()?.Interfaces?.Tcp ?? Enumerable.Empty<Tcp>())
                    {
                         s_commFacilitators.Add(new CommFacilitator(tcpPort, this));
                    }
                    AppendToLog("All configured ports opened.");
               }
               catch (Exception ex)
               {
                    HandleEx(ex);
                    throw;
               }
               client.BaseAddress = new Uri(GetYamlSettings()?.RestLisAddress ?? "https://localhost:5001/");
          }
          protected void OnStop()
          {

               try
               {
                    AppendToLog("Service stopping.");
                    foreach (var comm in s_commFacilitators)
                    {
                         comm.Close();
                    }
               }
               catch (Exception ex)
               {
                    HandleEx(ex);
                    throw;
               }
          }

          public static void AppendToLog(string txt)
          {
               string? publicFolder = Environment.GetEnvironmentVariable("AllUsersProfile");
               var date = DateTime.Now;
               string txtFile = $"{publicFolder}\\UniversaLIS\\Service_Logs\\Log_{date.Year}-{date.Month}-{date.Day}.txt";
               if (!Directory.Exists($"{publicFolder}\\UniversaLIS\\Service_Logs\\"))
               {
                    Directory.CreateDirectory($"{publicFolder}\\UniversaLIS\\Service_Logs\\");
               }
               string txtWrite = $"{date.ToLocalTime()} \t{txt}\r\n";
               File.AppendAllText(txtFile, txtWrite);
          }

          // <summary>Method invoked when service is started from a debugging console.</summary>
          internal void DebuggingRoutine()
          {
               AppendToLog("Executing debugging routine...");
               OnStart();
               while (Console.ReadLine() is null)
               {
                    // Not sure why it doesn't work without looping anymore. 
                    Console.ReadLine();
               }
               OnStop();
          }

          public override Task StartAsync(CancellationToken cancellationToken)
          {
               AppendToLog("Initializing UniversaLIS background service...");
               OnStart();
               AppendToLog($"{s_commFacilitators.Count} port(s) active.");
               return _completedTask;
          }

          public override Task StopAsync(CancellationToken cancellationToken)
          {
               OnStop();
               return _completedTask;
          }

     }

}