using System;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic; // For List<string>
using System.Linq; // For Select
using ActionCenterListener;

class Program
{
    static FileInfo csvLogFile = new FileInfo(Path.Combine(Path.GetTempPath(), "ActionCenterEvents.csv"));
    static string csvHeader = "Timestamp,AppId,ToastTitle,ImageCount,Image1,ToastBody,Payload";
    static EventDirectoryManager eventDirs = new EventDirectoryManager();

    static void Main(string[] args)
    {        
        // Load configuration
        var config = Config.Load(args);

        // Initialize console if requested
        if (config.Console)
        {
            Utils.CreateConsole();
            Utils.SetConsoleTitle("ActionCenterEvents");
        }
        Utils.SetConsoleEnabled(config.Console);
        
        if (config.Console)
        {
            Utils.Log("Initializing ActionCenterEvents...");
        }
        
        var poller = new ActionCenterPoller();
        var shutdownRequested = false;
        
        // Create CSV file with header if it doesn't exist and CSV logging is enabled
        if (config.csv && !File.Exists(csvLogFile.FullName))
        {
            File.WriteAllText(csvLogFile.FullName, csvHeader + Environment.NewLine, Encoding.UTF8);
        }
        
        try
        {
            Utils.Log($"Database path: {poller._dbPath}");
            Utils.Log($"CSV log path: {csvLogFile.FullName}");
            Utils.Log($"Environment variable prefix: {config.EnvironmentVariablePrefix}");
            
            poller.OnNotification += notif =>
            {
                try
                {
                    if (notif.Payload is null) return;

                    var timestamp = notif.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    var appId = notif.AppId ?? "";
                    var toastTitle = notif.Payload.ToastTitle ?? "";
                    var toastBody = notif.Payload.ToastBody ?? "";
                    var image1 = notif.Payload.Images.Count > 0 ? notif.Payload.Images[0] : "";
                    var payload = notif.Payload.RawXml;
                    
                    Utils.Log($"{timestamp};{appId};{toastTitle};{notif.Payload.Images.Count};{image1};{toastBody}");
                    
                    // Log to CSV
                    if (config.csv)
                    {
                        var payloadString = payload?.Replace("\r", "").Replace("\n", "").Replace("\"", "\"\"") ?? "";
                        var csvLine = string.Join(",", new[] { timestamp, appId, toastTitle, toastBody, notif.Payload.Images.Count.ToString(), image1, payloadString }.Select(field => $"\"{field}\""));
                        try
                        {
                            File.AppendAllText(csvLogFile.FullName, csvLine + Environment.NewLine, Encoding.UTF8);
                        }
                        catch (Exception ex)
                        {
                            Utils.Log($"Failed to write to CSV: {ex.Message}");
                        }
                    }
                    
                    // Execute files in specified directories
                    var envVars = new Dictionary<string, string>() {
                        { config.EnvironmentVariablePrefix + "APPID", appId ?? string.Empty },
                        { config.EnvironmentVariablePrefix + "TITLE", toastTitle ?? string.Empty },
                        { config.EnvironmentVariablePrefix + "BODY", toastBody ?? string.Empty },
                        { config.EnvironmentVariablePrefix + "PAYLOAD", payload ?? string.Empty },
                        { config.EnvironmentVariablePrefix + "TIMESTAMP", timestamp ?? string.Empty },
                        { config.EnvironmentVariablePrefix + "DATETIME", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                    };
                    for (int i = 0; i < notif.Payload.Images.Count; i++)
                    {
                        var varName = config.EnvironmentVariablePrefix + $"IMAGE{i + 1}";
                        envVars[varName] = notif.Payload.Images[i] ?? string.Empty;
                    }
                    eventDirs.ExecuteEvent("OnActionCenterNotification", envVars);
                } catch (Exception ex) {
                    Utils.Log($"Error processing notification: {ex.Message}");
                    Utils.Log($"Notification details - AppId: {notif?.AppId ?? "null"}, Timestamp: {notif?.Timestamp}");
                }
            };

            if (config.Console)
            {
                Utils.Log("Press CTRL+C to exit...");
                
                // Set up CTRL+C handler only if console is enabled
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true; // Prevent the process from terminating immediately
                    Utils.Log("\nShutting down...");
                    shutdownRequested = true;
                };
            }

            // Keep the application running
            Utils.Log("Starting main loop...");
            
            while (!shutdownRequested)
            {
                Thread.Sleep(100);
            }
            
            Utils.Log("Main loop ended.");
        }
        finally
        {
            poller.Dispose();
        }
    }
}
