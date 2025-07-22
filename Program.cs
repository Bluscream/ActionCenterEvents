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
        // Define paths
        var exePath = AppContext.BaseDirectory ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? Environment.GetCommandLineArgs().FirstOrDefault() ?? ".";
        var eventsDirGlobal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Events", "OnActionCenterNotification");
        var eventsDirUser = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Events", "OnActionCenterNotification");
        var csvLogPath = Path.Combine(Path.GetTempPath(), "ActionCenterEvents.csv");
        var csvHeader = "Timestamp,AppId,ToastTitle,ToastBody,Payload";
        
        // Load configuration
        var config = Config.Load(args, exePath);

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
        if (config.csv && !File.Exists(csvLogPath))
        {
            File.WriteAllText(csvLogPath, csvHeader + Environment.NewLine, Encoding.UTF8);
        }
        
        try
        {
            Utils.Log($"Database path: {poller._dbPath}");
            Utils.Log($"CSV log path: {csvLogPath}");
            Utils.Log($"Global events path: {eventsDirGlobal}");
            Utils.Log($"User events path: {eventsDirUser}");
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
                    var payload = notif.Payload.ToString() ?? "";
                    
                    Utils.Log($"[{timestamp}] {appId}: {toastTitle} - {toastBody}");
                    
                    // Log to CSV
                    if (config.csv)
                    {
                        var csvLine = string.Join(",", new[] { timestamp, appId, toastTitle, toastBody, payload }.Select(field => $"\"{field}\""));
                        try
                        {
                            File.AppendAllText(csvLogPath, csvLine + Environment.NewLine, Encoding.UTF8);
                        }
                        catch (Exception ex)
                        {
                            Utils.Log($"Failed to write to CSV: {ex.Message}");
                        }
                    }
                    
                    // Execute files in specified directories
                    ExecuteNotificationFiles(config, eventsDirGlobal, eventsDirUser, appId, toastTitle, toastBody, payload, timestamp);
                }
                catch (Exception ex)
                {
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
                    Utils.Log($"Failed to create directory {directory}: {ex.Message}");
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
                Utils.Log($"Failed to process directory {directory}: {ex.Message}");
            }
        }
    }
    
    static void ExecuteFile(Config config, string filePath, string appId, string toastTitle, string toastBody, string payload, string timestamp)
    {
        try
        {
            string launchPath = filePath;
            string? resolvedTarget = null;
            if (filePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                resolvedTarget = Utils.RunInSTA(() => ShortcutResolver.ResolveShortcutTarget(filePath));
                if (!string.IsNullOrWhiteSpace(resolvedTarget))
                {
                    launchPath = resolvedTarget;
                }
                else
                {
                    Utils.Log($"Failed to resolve shortcut: {filePath}");
                    return;
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = launchPath,
                UseShellExecute = false,
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
            Utils.Log($"Failed to execute {filePath}: {ex.Message}");
        }
    }
}
