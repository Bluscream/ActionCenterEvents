using System;
using ActionCenterListener;
using System.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic; // Added for List<string>
using System.Linq; // Added for Select

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
            Utils.Log($"Global events path: {eventDirs.GlobalEventDirectory}");
            Utils.Log($"User events path: {eventDirs.UserEventDirectory}");
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
                    eventDirs.ExecuteEventDirectories(config, "OnActionCenterNotification", appId, toastTitle, toastBody, payload, timestamp, notif.Payload.Images);
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
    
    static void ExecuteFile(Config config, string filePath, string appId, string toastTitle, string toastBody, string payload, string timestamp, List<string> images)
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
                foreach (var image in images)
                {
                    var varName = prefix + "IMAGE" + (images.IndexOf(image) + 1);
                    Utils.Log($"Setting environment variable {varName} to {image}");
                    startInfo.EnvironmentVariables[varName] = image;
                }
            }
            else
            {
                Utils.Log($"Warning: Environment variables are not supported for shortcuts (.lnk files)!");
            }

            Utils.Log($"Executing {filePath} with arguments: {startInfo.Arguments}");

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
