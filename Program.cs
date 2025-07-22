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
        var eventsDirGlobal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Events", "OnActionCenterNotification");
        var eventsDirUser = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Events", "OnActionCenterNotification");
        var csvLogPath = Path.Combine(Path.GetTempPath(), "ActionCenterEvents.csv");
        var csvHeader = "Timestamp,AppId,ToastTitle,ToastBody,Payload";
        
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
            var isBatch = filePath.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);
            var isShortcut = filePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);

            var startInfo = new ProcessStartInfo
            {
                FileName = isBatch ? "cmd.exe" : filePath,
                Arguments = isBatch ? $"/c \"{filePath}\"" : "",
                UseShellExecute = isShortcut ? true : false,
                CreateNoWindow = true, // Hide window for non-shortcuts
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            if (!isShortcut)
            {
                // Set environment variables with configurable prefix
                var prefix = config.EnvironmentVariablePrefix;
                startInfo.EnvironmentVariables[prefix + "APPID"] = appId;
                startInfo.EnvironmentVariables[prefix + "TITLE"] = toastTitle;
                startInfo.EnvironmentVariables[prefix + "BODY"] = toastBody;
                startInfo.EnvironmentVariables[prefix + "PAYLOAD"] = payload;
                startInfo.EnvironmentVariables[prefix + "TIMESTAMP"] = timestamp;
                startInfo.EnvironmentVariables[prefix + "DATETIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                Utils.Log($"Warning: Environment variables are not supported for shortcuts (.lnk files): {filePath}");
            }

            var process = Process.Start(startInfo);
            // Do not wait for exit, do not read output
            if (process != null)
            {
                process.EnableRaisingEvents = false;
            }
        }
        catch (Exception ex)
        {
            Utils.Log($"Failed to execute {filePath}: {ex.Message}");
        }
    }
}
