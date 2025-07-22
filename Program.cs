using System;
using ActionCenterListener;
using System.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using System.IO;
using System.Diagnostics;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        
        Utils.SetConsoleTitle("ActionCenterEvents");
        var poller = new ActionCenterPoller();
        var shutdownRequested = false;
        
        // Define paths
        var exePath = AppContext.BaseDirectory ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? Environment.GetCommandLineArgs().FirstOrDefault() ?? ".";
        var eventsDirGlobal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Events", "OnActionCenterNotification");
        var eventsDirUser = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Events", "OnActionCenterNotification");
        var csvLogPath = Path.Combine(Path.GetTempPath(), "ActionCenterEvents.csv");
        var csvHeader = "Timestamp,AppId,ToastTitle,ToastBody,Payload";
        
        // Load configuration
        var config = Config.Load(args, exePath);
        
        // Create CSV file with header if it doesn't exist
        if (!File.Exists(csvLogPath))
        {
            File.WriteAllText(csvLogPath, csvHeader + Environment.NewLine, Encoding.UTF8);
        }
        
        try
        {
            if (config.Console)
            {
                Console.WriteLine($"Database path: {poller._dbPath}");
                Console.WriteLine($"CSV log path: {csvLogPath}");
                Console.WriteLine($"Global events path: {eventsDirGlobal}");
                Console.WriteLine($"User events path: {eventsDirUser}");
                Console.WriteLine($"Environment variable prefix: {config.EnvironmentVariablePrefix}");
            }
            
            poller.OnNotification += notif =>
            {
                try
                {
                    if (notif.Payload is null) return;

                    var timestamp = notif.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    var appId = notif.AppId ?? "";
                    var toastTitle = notif.Payload.ToastTitle ?? "";
                    var toastBody = notif.Payload.ToastBody ?? "";
                    var payload = notif.Payload.ToString() ?? "";
                    
                    // Print to console if enabled
                    if (config.Console)
                    {
                        Console.WriteLine($"[{timestamp}] {appId}: {toastTitle} - {toastBody}");
                    }
                    
                    // Log to CSV
                    var csvLine = string.Join(",", new[] { timestamp, appId, toastTitle, toastBody, payload }.Select(field => $"\"{field}\""));
                    try
                    {
                        File.AppendAllText(csvLogPath, csvLine + Environment.NewLine, Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        if (config.Console)
                        {
                            Console.WriteLine($"Failed to write to CSV: {ex.Message}");
                        }
                    }
                    
                                            // Execute files in specified directories
                        ExecuteNotificationFiles(config, eventsDirGlobal, eventsDirUser, appId, toastTitle, toastBody, payload, timestamp);
                }
                catch (Exception ex)
                {
                    if (config.Console)
                    {
                        Console.WriteLine($"Error processing notification: {ex.Message}");
                        Console.WriteLine($"Notification details - AppId: {notif?.AppId ?? "null"}, Timestamp: {notif?.Timestamp}");
                    }
                }
            };

            if (config.Console)
            {
                Console.WriteLine("Press CTRL+C to exit...");
            }
            
            // Set up CTRL+C handler
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Prevent the process from terminating immediately
                if (config.Console)
                {
                    Console.WriteLine("\nShutting down...");
                }
                shutdownRequested = true;
            };

            // Keep the application running
            while (!shutdownRequested)
            {
                Thread.Sleep(100);
            }
        }
        finally
        {
            poller.Dispose();
        }
    }
    
    static void ExecuteNotificationFiles(Config config, string eventsDirGlobal, string eventsDirUser, string appId, string toastTitle, string toastBody, string payload, string timestamp)
    {
        var directories = new[]
        {
            eventsDirGlobal,
            eventsDirUser
        };
        
        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    if (config.Console)
                    {
                        Console.WriteLine($"Failed to create directory {directory}: {ex.Message}");
                    }
                    continue;
                }
            }
            
            try
            {
                var files = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    ExecuteFile(config, file, appId, toastTitle, toastBody, payload, timestamp);
                }
            }
            catch (Exception ex)
            {
                if (config.Console)
                {
                    Console.WriteLine($"Failed to process directory {directory}: {ex.Message}");
                }
            }
        }
    }
    
    static void ExecuteFile(Config config, string filePath, string appId, string toastTitle, string toastBody, string payload, string timestamp)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            
            // Set environment variables with configurable prefix
            var prefix = config.EnvironmentVariablePrefix;
            startInfo.EnvironmentVariables[prefix + "APPID"] = appId;
            startInfo.EnvironmentVariables[prefix + "TITLE"] = toastTitle;
            startInfo.EnvironmentVariables[prefix + "BODY"] = toastBody;
            startInfo.EnvironmentVariables[prefix + "PAYLOAD"] = payload;
            startInfo.EnvironmentVariables[prefix + "TIMESTAMP"] = timestamp;
            startInfo.EnvironmentVariables[prefix + "DATETIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            var process = Process.Start(startInfo);
            if (process != null)
            {
                // Don't wait for the process to complete
                process.EnableRaisingEvents = false;
            }
        }
        catch (Exception ex)
        {
            if (config.Console)
            {
                Console.WriteLine($"Failed to execute {filePath}: {ex.Message}");
            }
        }
    }
}
